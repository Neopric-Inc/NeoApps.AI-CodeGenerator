using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using BitMiracle.LibTiff.Classic;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace nkv.MicroService.API.Ai
{
    /// <summary>
    /// Reads scanned PDFs, photos and images with Claude (vision / PDF input). Images are re-oriented from EXIF, converted
    /// (BMP, WebP, TIFF pages) and downscaled to stay within the API limits; long PDFs are sent in small page batches.
    /// </summary>
    public sealed class DocumentOcr
    {
        private const int PdfPagesPerCall = 8;
        private const int MaxEdge = 2000;
        private const string Instructions =
            "Transcribe ALL text in this content exactly as written, in reading order. Keep tables as one line per row with ' | ' between cells. " +
            "Keep numbers, dates, amounts and currency symbols exactly. Mark handwriting as [handwritten: ...] and unreadable parts as [illegible]. " +
            "Do not summarise or add commentary. If there is little or no text (a photo of a prototype, a test rig, a chart), write one line starting with " +
            "'[image]' that describes what is shown, then transcribe any visible labels or readings.";

        private readonly ClaudeClient _claude;
        private readonly string _model;
        private readonly CancellationToken _ct;

        private readonly bool _imagesOnly;

        /// <param name="imagesOnly">OpenAI-compatible endpoints can't take PDFs: send each page's scanned image instead.</param>
        public DocumentOcr(ClaudeClient claude, string model, CancellationToken ct, bool imagesOnly = false)
        {
            _claude = claude;
            _imagesOnly = imagesOnly;
            _model = model;
            _ct = ct;
        }

        public List<string> Read(byte[] content, string extension) =>
            extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? Pdf(content) : Image(content, extension);

        private List<string> Pdf(byte[] bytes)
        {
            if (_imagesOnly) return PdfAsImages(bytes);
            var pages = new List<string>();
            using var doc = PdfDocument.Open(bytes);
            var count = doc.NumberOfPages;
            for (var start = 1; start <= count; start += PdfPagesPerCall)
            {
                var end = Math.Min(count, start + PdfPagesPerCall - 1);
                var builder = new PdfDocumentBuilder();
                for (var p = start; p <= end; p++) builder.AddPage(doc, p);
                var chunk = builder.Build();
                var text = Ask(new JsonObject
                {
                    ["type"] = "document",
                    ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = "application/pdf", ["data"] = Convert.ToBase64String(chunk) },
                }, $"This is pages {start}-{end} of a scanned PDF. Start each page with a line '=== PAGE n ===' (n = page number within this excerpt, from 1).");
                var parts = Regex.Split(text, @"^=== PAGE \d+ ===\s*$", RegexOptions.Multiline).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
                if (parts.Count == 0) parts.Add(text);
                pages.AddRange(parts);
            }
            return pages;
        }

        /// <summary>Scanned PDFs hold one picture per page: extract it and read it as an image (no PDF rendering needed).</summary>
        private List<string> PdfAsImages(byte[] bytes)
        {
            var pages = new List<string>();
            using var doc = PdfDocument.Open(bytes);
            foreach (var page in doc.GetPages())
            {
                var text = new List<string>();
                foreach (var img in page.GetImages().OrderByDescending(i => i.WidthInSamples * i.HeightInSamples).Take(3))
                {
                    byte[] jpeg = null;
                    try { jpeg = img.TryGetPng(out var png) ? ToJpeg(png) : ToJpeg(img.RawBytes.ToArray()); }
                    catch (AiException) { }
                    if (jpeg == null) continue;
                    text.Add(Ask(new JsonObject
                    {
                        ["type"] = "image",
                        ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(jpeg) },
                    }, null));
                }
                pages.Add(text.Count > 0 ? string.Join("\n", text) : "[illegible]");
            }
            return pages;
        }

        private List<string> Image(byte[] bytes, string ext)
        {
            var images = ext.ToLowerInvariant() is ".tif" or ".tiff" ? TiffPages(bytes) : new List<byte[]> { ToJpeg(bytes) };
            var pages = new List<string>();
            foreach (var jpeg in images)
                pages.Add(Ask(new JsonObject
                {
                    ["type"] = "image",
                    ["source"] = new JsonObject { ["type"] = "base64", ["media_type"] = "image/jpeg", ["data"] = Convert.ToBase64String(jpeg) },
                }, null));
            return pages;
        }

        private string Ask(JsonObject block, string extra)
        {
            var res = _claude.CreateAsync(new JsonObject
            {
                ["model"] = _model,
                ["max_tokens"] = 16000,
                ["messages"] = new JsonArray(new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray(block, new JsonObject { ["type"] = "text", ["text"] = Instructions + (extra == null ? "" : "\n" + extra) }),
                }),
            }, _ct).GetAwaiter().GetResult();
            return string.Join("\n", res["content"]!.AsArray().Where(b => b?["type"]?.GetValue<string>() == "text").Select(b => b!["text"]!.GetValue<string>())).Trim();
        }

        // ------------------------------------------------------------------ image handling

        /// <summary>Decodes any image Skia supports, applies EXIF orientation, downsizes and re-encodes as JPEG.</summary>
        public static byte[] ToJpeg(byte[] bytes)
        {
            using var codec = SKCodec.Create(new MemoryStream(bytes))
                ?? throw new AiException("This image format can't be read here. Please upload it as JPG, PNG or PDF (iPhone HEIC photos: Settings > Camera > Formats > Most Compatible).");
            using var raw = SKBitmap.Decode(codec) ?? throw new AiException("The image couldn't be decoded.");
            using var oriented = Orient(raw, codec.EncodedOrigin);
            return Encode(oriented);
        }

        private static byte[] Encode(SKBitmap bmp)
        {
            var scale = Math.Min(1.0, (double)MaxEdge / Math.Max(bmp.Width, bmp.Height));
            using var resized = scale < 1
                ? bmp.Resize(new SKImageInfo(Math.Max(1, (int)(bmp.Width * scale)), Math.Max(1, (int)(bmp.Height * scale))), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
                : null;
            using var img = SKImage.FromBitmap(resized ?? bmp);
            using var data = img.Encode(SKEncodedImageFormat.Jpeg, 85);
            return data.ToArray();
        }

        private static SKBitmap Orient(SKBitmap src, SKEncodedOrigin origin)
        {
            int degrees = origin switch { SKEncodedOrigin.BottomRight => 180, SKEncodedOrigin.RightTop => 90, SKEncodedOrigin.LeftBottom => 270, _ => 0 };
            if (degrees == 0) return src.Copy();
            var swap = degrees != 180;
            var dst = new SKBitmap(swap ? src.Height : src.Width, swap ? src.Width : src.Height);
            using var canvas = new SKCanvas(dst);
            canvas.Translate(dst.Width / 2f, dst.Height / 2f);
            canvas.RotateDegrees(degrees);
            canvas.Translate(-src.Width / 2f, -src.Height / 2f);
            canvas.DrawBitmap(src, 0, 0);
            return dst;
        }

        /// <summary>Every page of a (multi-page) TIFF scan as JPEG.</summary>
        private static List<byte[]> TiffPages(byte[] bytes)
        {
            var result = new List<byte[]>();
            using var tif = Tiff.ClientOpen("upload", "r", new MemoryStream(bytes), new TiffStream()) ?? throw new AiException("Not a valid TIFF file.");
            do
            {
                var w = tif.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                var h = tif.GetField(TiffTag.IMAGELENGTH)[0].ToInt();
                if ((long)w * h > 80_000_000) throw new AiException("TIFF page too large to read.");
                var raster = new int[w * h];
                if (!tif.ReadRGBAImageOriented(w, h, raster, Orientation.TOPLEFT)) continue;
                using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Unpremul));
                Marshal.Copy(raster, 0, bmp.GetPixels(), raster.Length);
                result.Add(Encode(bmp));
            } while (result.Count < 60 && tif.ReadDirectory());
            return result;
        }
    }
}
