using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UglyToad.PdfPig;

namespace nkv.MicroService.API.Ai
{
    /// <summary>Extracts text per page from PDF, DOCX and plain-text files (DOCX/TXT are split into ~3,000-character pages).</summary>
    public static class DocumentText
    {
        public static readonly string[] Supported = { ".pdf", ".docx", ".txt", ".md", ".csv", ".json" };
        private const int PseudoPage = 3000;

        public static List<string> Extract(string path, string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => Pdf(path),
                ".docx" => Split(Docx(path)),
                ".txt" or ".md" or ".csv" or ".json" => Split(File.ReadAllText(path)),
                _ => throw new AiException($"Unsupported file type {ext}. Upload PDF, DOCX, TXT, MD, CSV or JSON."),
            };
        }

        private static List<string> Pdf(string path)
        {
            using var doc = PdfDocument.Open(path);
            return doc.GetPages().Select(p => Layout(p.GetWords().ToList())).ToList();
        }

        /// <summary>
        /// Rebuilds a page's lines from word positions, keeping horizontal placement so tables (e.g. a Schedule of
        /// Assessments grid of X marks) stay aligned under their column headers instead of collapsing into one run of words.
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
                if (lastBottom is double lb && lb - bottom > height * 2.2) sb.AppendLine();   // paragraph gap
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

        private static string Docx(string path)
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("word/document.xml") ?? throw new AiException("Not a valid DOCX file.");
            using var s = entry.Open();
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            var xml = XDocument.Load(s);
            var sb = new StringBuilder();
            string Text(XElement e) => string.Concat(e.Descendants().Where(x => x.Name == w + "t" || x.Name == w + "tab").Select(x => x.Name == w + "tab" ? "\t" : x.Value));
            var body = xml.Root?.Element(w + "body");
            foreach (var el in body?.Elements() ?? Enumerable.Empty<XElement>())
            {
                if (el.Name == w + "tbl")
                    // Tables keep their grid: one line per row, cells separated by " | " (empty cells stay visible).
                    foreach (var tr in el.Descendants(w + "tr"))
                        sb.AppendLine("| " + string.Join(" | ", tr.Elements(w + "tc").Select(tc => string.Join(" ", tc.Elements(w + "p").Select(Text)).Trim())) + " |");
                else if (el.Name == w + "p") sb.AppendLine(Text(el));
            }
            return sb.ToString();
        }

        private static List<string> Split(string text)
        {
            text = text.Replace("\r\n", "\n");
            var pages = new List<string>();
            var sb = new StringBuilder();
            foreach (var para in text.Split('\n'))
            {
                if (sb.Length + para.Length > PseudoPage && sb.Length > 0) { pages.Add(Clean(sb.ToString())); sb.Clear(); }
                sb.AppendLine(para);
            }
            if (sb.Length > 0) pages.Add(Clean(sb.ToString()));
            return pages.Count == 0 ? new List<string> { "" } : pages;
        }

        private static string Clean(string s) => Regex.Replace(s ?? "", @"[ \t]{2,}", " ").Trim();
    }
}
