using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace nkv.MicroService.API.Ai
{
    /// <summary>
    /// Reads uploaded files that need OCR (scanned PDFs, photos, images, and e-mails/ZIPs containing them) with Claude in the
    /// background, then indexes their text. Files wait in status "needs_ocr" until ANTHROPIC_API_KEY is set.
    /// Several API instances can run it: each file is claimed with a conditional update.
    /// </summary>
    public sealed class AiFileWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly AiSettings _settings;
        private readonly AiData _data;
        private readonly ILogger<AiFileWorker> _log;

        public AiFileWorker(IServiceProvider services, AiSettings settings, AiData data, ILogger<AiFileWorker> log)
        {
            _services = services;
            _settings = settings;
            _data = data;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stop);
            try
            {
                // files left half-read by a crash or restart go back in the queue
                using var c = _data.Open();
                AiData.Cmd(c, "UPDATE ai_files SET status='needs_ocr' WHERE status='reading'").ExecuteNonQuery();
            }
            catch (Exception ex) { _log.LogWarning(ex, "AI file worker could not reset interrupted files"); }

            while (!stop.IsCancellationRequested)
            {
                var worked = false;
                try { if (_settings.HasKey) worked = await ProcessNext(stop); }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { break; }
                catch (Exception ex) { _log.LogError(ex, "AI file worker failed"); }
                await Task.Delay(worked ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(_settings.HasKey ? 10 : 60), stop);
            }
        }

        private async Task<bool> ProcessNext(CancellationToken stop)
        {
            using var c = _data.Open();
            int id; string owner, path, name;
            using (var r = AiData.Cmd(c, "SELECT file_id, owner_name, storage_path, file_name FROM ai_files WHERE status='needs_ocr' ORDER BY file_id LIMIT 1").ExecuteReader())
            {
                if (!r.Read()) return false;
                (id, owner, path, name) = (r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3));
            }
            if (AiData.Cmd(c, "UPDATE ai_files SET status='reading', error=NULL WHERE file_id=@id AND status='needs_ocr'", ("@id", id)).ExecuteNonQuery() != 1) return true;

            try
            {
                using var scope = _services.CreateScope();
                var claude = scope.ServiceProvider.GetRequiredService<ClaudeClient>();
                var ocr = new DocumentOcr(claude, _settings.VisionModel(_settings.Config(owner)), stop, _settings.IsOpenAi);
                var result = await Task.Run(() => DocumentText.Read(path, name, (bytes, ext) => ocr.Read(bytes, ext)), stop);
                var tenant = AiData.Cmd(c, "SELECT tenant_id FROM ai_files WHERE file_id=@id", ("@id", id)).ExecuteScalar();
                using var tx = c.BeginTransaction();
                void Exec(string sql, params (string, object)[] ps) { var cmd = AiData.Cmd(c, sql, ps); cmd.Transaction = tx; cmd.ExecuteNonQuery(); }
                Exec("DELETE FROM ai_chunks WHERE file_id=@id", ("@id", id));
                foreach (var (text, i) in result.Pages.Select((t, i) => (t, i)).Where(p => !string.IsNullOrWhiteSpace(p.t)))
                    Exec("INSERT INTO ai_chunks (file_id, owner_name, page, text, tenant_id) VALUES (@f, @o, @p, @t, @tn)",
                        ("@f", id), ("@o", owner), ("@p", i + 1), ("@t", text), ("@tn", tenant ?? DBNull.Value));
                Exec("UPDATE ai_files SET status=@s, pages=@p, error=@e WHERE file_id=@id",
                    ("@s", result.Status == "needs_ocr" ? "indexed" : result.Status), ("@p", result.Pages.Count), ("@e", result.Note), ("@id", id));
                tx.Commit();
                _log.LogInformation("Read {File} ({Pages} pages) with OCR", name, result.Pages.Count);
            }
            catch (Exception ex) when (!stop.IsCancellationRequested)
            {
                var message = ex is AiException ? ex.Message : "Couldn't read this file: " + ex.Message;
                AiData.Cmd(c, "UPDATE ai_files SET status='failed', error=@e WHERE file_id=@id", ("@e", message), ("@id", id)).ExecuteNonQuery();
                _log.LogWarning(ex, "OCR failed for {File}", name);
            }
            return true;
        }
    }
}
