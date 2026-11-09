using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ExcelDataReader;
using UglyToad.PdfPig;

namespace nkv.MicroService.API.Ai
{
    /// <summary>
    /// Turns an uploaded file into text pages for search and AI. Reads PDF, Word, Excel/CSV, PowerPoint, OpenDocument,
    /// RTF, HTML/XML, e-mail (.eml/.msg) with attachments, ZIP archives and any plain-text or source file.
    /// Scanned PDFs, photos and images need OCR: without an <c>ocr</c> callback they are reported as <see cref="Result.NeedsOcr"/>
    /// (the AI file worker reads them later with Claude); with one, they are transcribed inline, including inside e-mails and ZIPs.
    /// Formats with no readable text (CAD, video…) are kept as "stored".
    /// </summary>
    public static class DocumentText
    {
        /// <summary>Executables and installers are refused; everything else is accepted.</summary>
        public static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
            { ".exe", ".dll", ".msi", ".scr", ".com", ".bat", ".cmd", ".ps1", ".vbs", ".jar", ".apk", ".app", ".dmg", ".iso", ".pif", ".cpl", ".msp", ".hta" };
        public static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
            { ".png", ".jpg", ".jpeg", ".jfif", ".gif", ".webp", ".bmp", ".tif", ".tiff", ".heic", ".heif" };
        public static readonly HashSet<string> Spreadsheets = new(StringComparer.OrdinalIgnoreCase)
            { ".xlsx", ".xlsm", ".xls", ".xlsb", ".csv", ".tsv", ".ods" };
        /// <summary>Kept for compatibility: the formats read without OCR.</summary>
        public static readonly string[] Supported = { ".pdf", ".docx", ".txt", ".md", ".csv", ".json", ".xlsx", ".xls", ".pptx", ".eml", ".msg", ".zip", ".html", ".rtf", ".odt", ".ods", ".odp" };

        private const int PseudoPage = 3000;
        private const int MaxArchiveEntries = 300;
        private const long MaxArchiveBytes = 400_000_000;

        public sealed class Result
        {
            public List<string> Pages = new();
            /// <summary>indexed | needs_ocr | stored</summary>
            public string Status = "indexed";
            public string Note;
            public bool NeedsOcr;
        }

        /// <summary>One sheet of a spreadsheet/CSV as rows of cell strings (dates as yyyy-MM-dd, numbers invariant).</summary>
        public sealed record Table(string Name, List<string[]> Rows);

        /// <summary>OCR callback: (file bytes, extension) -> pages of text.</summary>
        public delegate List<string> Ocr(byte[] content, string extension);

        static DocumentText() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        public static List<string> Extract(string path, string fileName) => Read(path, fileName).Pages;

        public static Result Read(string path, string fileName, Ocr ocr = null)
        {
            var r = new Result();
            try { r.Pages = ReadAny(File.ReadAllBytes(path), fileName, ocr, r, 0); }
            catch (Exception ex) when (ex is not AiException) { throw new AiException($"Couldn't read {fileName}: {ex.Message}"); }
            r.Pages = r.Pages.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            if (r.NeedsOcr && ocr == null) r.Status = "needs_ocr";
            else if (r.Pages.Sum(p => p.Length) < 20) { r.Status = "stored"; r.Note ??= "No readable text in this file type; it is stored and listed as evidence."; }
            if (r.Pages.Count == 0) r.Pages.Add("");
            return r;
        }

        private static List<string> ReadAny(byte[] bytes, string fileName, Ocr ocr, Result r, int depth)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            switch (ext)
            {
                case ".pdf": return Pdf(bytes, ocr, r);
                case ".docx": case ".docm": case ".dotx": return Split(Docx(bytes));
                case ".pptx": case ".ppsx": case ".pptm": return Pptx(bytes);
                case ".xlsx": case ".xlsm": case ".xls": case ".xlsb": case ".csv": case ".tsv": case ".ods":
                    return TablesToPages(ReadTables(bytes, fileName));
                case ".odt": case ".odp": return Split(OpenDocumentText(bytes));
                case ".rtf": return Split(Rtf(Decode(bytes)));
                case ".html": case ".htm": case ".xhtml": return Split(Html(Decode(bytes)));
                case ".eml": return Eml(bytes, ocr, r, depth);
                case ".msg": return Msg(bytes, ocr, r, depth);
                case ".zip": return Zip(bytes, ocr, r, depth);
                case ".doc": case ".ppt": case ".wps": return Split(LegacyOfficeText(bytes));
            }
            if (Images.Contains(ext))
            {
                if (ocr == null) { r.NeedsOcr = true; return new List<string> { $"[image {fileName}: text not read yet]" }; }
                return ocr(bytes, ext);
            }
            if (LooksLikeText(bytes)) return Split(Decode(bytes));
            r.Note = $"{ext} files have no readable text; the file is stored.";
            return new List<string>();
        }

        // ------------------------------------------------------------------ PDF

        private static List<string> Pdf(byte[] bytes, Ocr ocr, Result r)
        {
            List<string> pages;
            using (var doc = PdfDocument.Open(bytes)) pages = doc.GetPages().Select(p => Layout(p.GetWords().ToList())).ToList();
            // A scanned PDF (or one with many scanned pages) has little or no text layer.
            var empty = pages.Count(p => p.Count(c => !char.IsWhiteSpace(c)) < 40);
            if (pages.Count > 0 && empty >= Math.Max(1, pages.Count * 0.3))
            {
                if (ocr == null) { r.NeedsOcr = true; return pages; }
                return ocr(bytes, ".pdf");
            }
            return pages;
        }

        /// <summary>
        /// Rebuilds a page's lines from word positions, keeping horizontal placement so tables stay aligned under their
        /// column headers instead of collapsing into one run of words.
        /// </summary>
        private static string Layout(List<UglyToad.PdfPig.Content.Word> words)
        {
            words = words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).ToList();
            if (words.Count == 0) return "";
            var charWidth = Math.Max(1.0, Median(words.Select(w => w.BoundingBox.Width / Math.Max(1, w.Text.Length))));
            var height = Math.Max(1.0, Median(words.Select(w => w.BoundingBox.Height)));
            var left = words.Min(w => w.BoundingBox.Left);
            var lines = new List<List<UglyToad.PdfPig.Content.Word>>();
            foreach (var w in words.OrderByDescending(w => w.BoundingBox.Bottom))
            {
                var line = lines.LastOrDefault();
                if (line != null && Math.Abs(line[0].BoundingBox.Bottom - w.BoundingBox.Bottom) < height * 0.5) line.Add(w);
                else lines.Add(new List<UglyToad.PdfPig.Content.Word> { w });
            }
            var sb = new StringBuilder();
            double? lastBottom = null;
            foreach (var line in lines)
            {
                var bottom = line[0].BoundingBox.Bottom;
                if (lastBottom is double lb && lb - bottom > height * 2.2) sb.AppendLine();
                lastBottom = bottom;
                var row = new StringBuilder();
                foreach (var w in line.OrderBy(w => w.BoundingBox.Left))
                {
                    var col = (int)Math.Round((w.BoundingBox.Left - left) / charWidth);
                    var min = row.Length == 0 ? 0 : row.Length + 1;
                    row.Append(' ', Math.Max(min, Math.Min(col, row.Length + 60)) - row.Length);
                    row.Append(w.Text);
                }
                sb.AppendLine(row.ToString().TrimEnd());
            }
            return sb.ToString().Trim('\n', '\r');
        }

        private static double Median(IEnumerable<double> values)
        {
            var v = values.OrderBy(x => x).ToList();
            return v.Count == 0 ? 0 : v[v.Count / 2];
        }

        // ------------------------------------------------------------------ Office Open XML

        private static XDocument ZipXml(ZipArchive zip, string name)
        {
            var e = zip.GetEntry(name);
            if (e == null) return null;
            using var s = e.Open();
            return XDocument.Load(s);
        }

        private static string Docx(byte[] bytes)
        {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            var xml = ZipXml(zip, "word/document.xml") ?? throw new AiException("Not a valid Word file.");
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var sb = new StringBuilder();
            string Text(XElement e) => string.Concat(e.Descendants().Where(x => x.Name == w + "t" || x.Name == w + "tab").Select(x => x.Name == w + "tab" ? "\t" : x.Value));
            foreach (var el in xml.Root?.Element(w + "body")?.Elements() ?? Enumerable.Empty<XElement>())
            {
                if (el.Name == w + "tbl")
                    foreach (var tr in el.Descendants(w + "tr"))
                        sb.AppendLine("| " + string.Join(" | ", tr.Elements(w + "tc").Select(tc => string.Join(" ", tc.Elements(w + "p").Select(Text)).Trim())) + " |");
                else if (el.Name == w + "p") sb.AppendLine(Text(el));
            }
            // comments and footnotes often hold reviewer reasoning
            foreach (var part in new[] { "word/comments.xml", "word/footnotes.xml" })
                if (ZipXml(zip, part) is { } extra)
                {
                    var t = string.Join("\n", extra.Descendants(w + "p").Select(Text).Where(s => !string.IsNullOrWhiteSpace(s)));
                    if (t.Length > 0) sb.AppendLine($"\n[{Path.GetFileNameWithoutExtension(part)}]\n{t}");
                }
            return sb.ToString();
        }

        private static List<string> Pptx(byte[] bytes)
        {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
            var slides = zip.Entries.Where(e => Regex.IsMatch(e.FullName, @"^ppt/slides/slide\d+\.xml$"))
                .OrderBy(e => int.Parse(Regex.Match(e.FullName, @"\d+").Value)).ToList();
            var pages = new List<string>();
            foreach (var (s, i) in slides.Select((s, i) => (s, i)))
            {
                var xml = ZipXml(zip, s.FullName);
                var text = string.Join("\n", xml.Descendants(a + "p").Select(p => string.Concat(p.Descendants(a + "t").Select(t => t.Value))).Where(t => t.Trim().Length > 0));
                var notes = ZipXml(zip, $"ppt/notesSlides/notesSlide{i + 1}.xml");
                var noteText = notes == null ? "" : string.Join(" ", notes.Descendants(a + "t").Select(t => t.Value));
                pages.Add($"Slide {i + 1}\n{text}" + (noteText.Trim().Length > 0 ? $"\n[notes] {noteText}" : ""));
            }
            return pages;
        }

        private static string OpenDocumentText(byte[] bytes)
        {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            var xml = ZipXml(zip, "content.xml") ?? throw new AiException("Not a valid OpenDocument file.");
            XNamespace t = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
            return string.Join("\n", xml.Descendants().Where(e => e.Name == t + "p" || e.Name == t + "h").Select(e => e.Value));
        }

        // ------------------------------------------------------------------ spreadsheets

        public static List<Table> ReadTables(string path, string fileName) => ReadTables(File.ReadAllBytes(path), fileName);

        public static List<Table> ReadTables(byte[] bytes, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".ods") return Ods(bytes);
            var tables = new List<Table>();
            using var ms = new MemoryStream(bytes);
            using var reader = ext is ".csv" or ".tsv"
                ? ExcelReaderFactory.CreateCsvReader(ms, new ExcelReaderConfiguration { AutodetectSeparators = ext == ".tsv" ? new[] { '\t' } : new[] { ',', ';', '\t', '|' } })
                : ExcelReaderFactory.CreateReader(ms);
            do
            {
                var rows = new List<string[]>();
                while (reader.Read())
                {
                    var cells = new string[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++) cells[i] = Cell(reader.GetValue(i));
                    if (cells.Any(c => c.Length > 0)) rows.Add(cells);
                }
                if (rows.Count > 0) tables.Add(new Table(string.IsNullOrEmpty(reader.Name) ? Path.GetFileNameWithoutExtension(fileName) : reader.Name, rows));
            } while (reader.NextResult());
            return tables;
        }

        private static string Cell(object v) => v switch
        {
            null => "",
            DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("yyyy-MM-dd") : d.ToString("yyyy-MM-dd HH:mm"),
            double x => x.ToString("0.##########", CultureInfo.InvariantCulture),
            float x => x.ToString("0.######", CultureInfo.InvariantCulture),
            decimal x => x.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "TRUE" : "FALSE",
            _ => v.ToString()!.Trim(),
        };

        private static List<Table> Ods(byte[] bytes)
        {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            var xml = ZipXml(zip, "content.xml") ?? throw new AiException("Not a valid OpenDocument spreadsheet.");
            XNamespace table = "urn:oasis:names:tc:opendocument:xmlns:table:1.0", office = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
            var result = new List<Table>();
            foreach (var t in xml.Descendants(table + "table"))
            {
                var rows = new List<string[]>();
                foreach (var tr in t.Descendants(table + "table-row").Take(200000))
                {
                    var cells = new List<string>();
                    foreach (var tc in tr.Elements(table + "table-cell"))
                    {
                        var n = Math.Min(64, int.TryParse((string)tc.Attribute(table + "number-columns-repeated"), out var k) ? k : 1);
                        var val = (string)tc.Attribute(office + "date-value") ?? (string)tc.Attribute(office + "value") ?? string.Join(" ", tc.Elements().Select(e => e.Value));
                        for (var i = 0; i < n; i++) cells.Add(val?.Trim() ?? "");
                    }
                    while (cells.Count > 0 && cells[^1].Length == 0) cells.RemoveAt(cells.Count - 1);
                    if (cells.Count > 0) rows.Add(cells.ToArray());
                }
                if (rows.Count > 0) result.Add(new Table((string)t.Attribute(table + "name") ?? "Sheet", rows));
            }
            return result;
        }

        /// <summary>Sheets as text: "| a | b |" rows, split into pages that each repeat the sheet's header row.</summary>
        private static List<string> TablesToPages(List<Table> tables)
        {
            var pages = new List<string>();
            foreach (var t in tables)
            {
                var header = "| " + string.Join(" | ", t.Rows[0]) + " |";
                var sb = new StringBuilder($"Sheet: {t.Name} ({t.Rows.Count} rows)\n{header}\n");
                foreach (var row in t.Rows.Skip(1))
                {
                    var line = "| " + string.Join(" | ", row) + " |";
                    if (sb.Length + line.Length > PseudoPage) { pages.Add(sb.ToString()); sb.Clear().Append($"Sheet: {t.Name} (continued)\n{header}\n"); }
                    sb.AppendLine(line);
                }
                pages.Add(sb.ToString());
            }
            return pages;
        }

        // ------------------------------------------------------------------ e-mail and archives

        private static List<string> Eml(byte[] bytes, Ocr ocr, Result r, int depth)
        {
            var msg = MimeKit.MimeMessage.Load(new MemoryStream(bytes));
            var body = msg.TextBody ?? (msg.HtmlBody != null ? Html(msg.HtmlBody) : "");
            var pages = Split($"From: {msg.From}\nTo: {msg.To}\nCc: {msg.Cc}\nDate: {msg.Date:yyyy-MM-dd HH:mm}\nSubject: {msg.Subject}\n\n{body}");
            foreach (var part in msg.Attachments.OfType<MimeKit.MimePart>())
            {
                using var ms = new MemoryStream();
                part.Content?.DecodeTo(ms);
                pages.AddRange(Inner(ms.ToArray(), part.FileName ?? "attachment", ocr, r, depth));
            }
            return pages;
        }

        private static List<string> Msg(byte[] bytes, Ocr ocr, Result r, int depth)
        {
            using var msg = new MsgReader.Outlook.Storage.Message(new MemoryStream(bytes));
            var body = msg.BodyText ?? (msg.BodyHtml != null ? Html(msg.BodyHtml) : "");
            var to = string.Join(", ", msg.Recipients.Select(x => x.Email ?? x.DisplayName));
            var pages = Split($"From: {msg.Sender?.Email ?? msg.Sender?.DisplayName}\nTo: {to}\nDate: {msg.SentOn:yyyy-MM-dd HH:mm}\nSubject: {msg.Subject}\n\n{body}");
            foreach (var a in msg.Attachments.OfType<MsgReader.Outlook.Storage.Attachment>())
                if (a.Data != null) pages.AddRange(Inner(a.Data, a.FileName ?? "attachment", ocr, r, depth));
            return pages;
        }

        private static List<string> Zip(byte[] bytes, Ocr ocr, Result r, int depth)
        {
            using var zip = new ZipArchive(new MemoryStream(bytes));
            var pages = new List<string>();
            long total = 0;
            var files = zip.Entries.Where(e => e.Length > 0 && !e.FullName.EndsWith("/") && !e.FullName.StartsWith("__MACOSX") && !Path.GetFileName(e.FullName).StartsWith(".")).ToList();
            pages.Add("Archive contents:\n" + string.Join("\n", files.Take(1000).Select(e => $"- {e.FullName} ({e.Length / 1024} KB)")));
            foreach (var e in files.Take(MaxArchiveEntries))
            {
                total += e.Length;
                if (total > MaxArchiveBytes) { pages.Add("[archive truncated: too large to read completely]"); break; }
                using var s = e.Open();
                using var ms = new MemoryStream();
                s.CopyTo(ms);
                pages.AddRange(Inner(ms.ToArray(), e.FullName, ocr, r, depth));
            }
            return pages;
        }

        private static List<string> Inner(byte[] bytes, string name, Ocr ocr, Result r, int depth)
        {
            if (depth >= 3 || Blocked.Contains(Path.GetExtension(name))) return new List<string> { $"[{name}: skipped]" };
            try
            {
                var inner = ReadAny(bytes, name, ocr, r, depth + 1).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                if (inner.Count == 0) return new List<string> { $"=== {name} (no readable text) ===" };
                inner[0] = $"=== {name} ===\n{inner[0]}";
                return inner;
            }
            catch (Exception ex) { return new List<string> { $"=== {name}: couldn't read ({ex.Message}) ===" }; }
        }

        // ------------------------------------------------------------------ text formats

        private static string Rtf(string rtf)
        {
            var s = Regex.Replace(rtf, @"\{\\\*[^{}]*\}", "");
            s = Regex.Replace(s, @"\\'([0-9a-fA-F]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
            s = Regex.Replace(s, @"\\(par|line)\b ?", "\n");
            s = Regex.Replace(s, @"\\[a-zA-Z]+-?\d* ?", "");
            return s.Replace("{", "").Replace("}", "");
        }

        private static string Html(string html)
        {
            var s = Regex.Replace(html, @"<(script|style)[^>]*>.*?</\1>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<(br|/p|/div|/tr|/h\d|/li)[^>]*>", "\n", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"</t[dh]>", " | ", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<[^>]+>", "");
            return System.Net.WebUtility.HtmlDecode(s);
        }

        /// <summary>Best-effort text from legacy binary Word/PowerPoint: UTF-16 and 8-bit runs of readable characters.</summary>
        private static string LegacyOfficeText(byte[] bytes)
        {
            var sb = new StringBuilder();
            var utf16 = Encoding.Unicode.GetString(bytes);
            foreach (Match m in Regex.Matches(utf16, @"[\p{L}\p{N}\p{P}\p{Zs}\r\n\t]{12,}")) sb.AppendLine(m.Value);
            if (sb.Length < 200)
                foreach (Match m in Regex.Matches(Encoding.Latin1.GetString(bytes), @"[A-Za-z0-9 ,.;:()'""%$&/\-\r\n\t]{20,}")) sb.AppendLine(m.Value);
            return sb.ToString();
        }

        private static bool LooksLikeText(byte[] bytes)
        {
            var n = Math.Min(bytes.Length, 8192);
            if (n == 0) return false;
            var nul = 0;
            for (var i = 0; i < n; i++) if (bytes[i] == 0) nul++;
            return nul == 0 || (bytes.Length > 1 && (bytes[0] == 0xFF && bytes[1] == 0xFE || bytes[0] == 0xFE && bytes[1] == 0xFF));
        }

        private static string Decode(byte[] bytes)
        {
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
            try { return new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
            catch (DecoderFallbackException) { return Encoding.GetEncoding(1252).GetString(bytes); }
        }

        public static List<string> Split(string text)
        {
            text = (text ?? "").Replace("\r\n", "\n");
            var pages = new List<string>();
            var sb = new StringBuilder();
            foreach (var para in text.Split('\n'))
            {
                if (sb.Length + para.Length > PseudoPage && sb.Length > 0) { pages.Add(Clean(sb.ToString())); sb.Clear(); }
                sb.AppendLine(para.Length > PseudoPage * 2 ? para.Substring(0, PseudoPage * 2) : para);
            }
            if (sb.Length > 0) pages.Add(Clean(sb.ToString()));
            return pages.Count == 0 ? new List<string> { "" } : pages;
        }

        private static string Clean(string s) => Regex.Replace(s ?? "", @"[ \t]{2,}", " ").Trim();
    }
}
