using System;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using nkv.MicroService.API.Ai;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>AI assistant: document files, Q&amp;A with citations over documents and records, and workflows (configured in ui.config.json "ai").</summary>
    [Authorize]
    [ApiController]
    public class AiController : ControllerBase
    {
        private const string Route = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/Ai";
        private readonly AiService _ai;
        private readonly ILogger<AiController> _log;

        public AiController(AiService ai, ILogger<AiController> log)
        {
            _ai = ai;
            _log = log;
        }

        private AiUser Me()
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            return new AiUser(
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0,
                name,
                int.TryParse(User.FindFirst(ClaimTypes.Role)?.Value, out var r) ? r : 0,
                User.FindFirst(ClaimTypes.Actor)?.Value ?? name);
        }

        private async Task<ActionResult> Run(Func<Task<object>> action)
        {
            try { return Ok(await action()); }
            catch (AiException ex) { return BadRequest(new { message = ex.Message }); }
            catch (OperationCanceledException) { return StatusCode(499); }
            catch (Exception ex)
            {
                _log.LogError(ex, "AI request failed");
                return StatusCode(500, new { message = "The assistant ran into an error: " + ex.Message });
            }
        }

        private Task<ActionResult> Run(Func<object> action) => Run(() => Task.FromResult(action()));

        [HttpGet, Route(Route + "/status")]
        public Task<ActionResult> Status() => Run(() => _ai.Status(Me()));

        // ------------------------------------------------------------------ files

        /// <summary>Multipart body for file uploads (a model class keeps Swagger generation working with IFormFile).</summary>
        public class UploadForm { public IFormFile file { get; set; } public string table { get; set; } public string recordId { get; set; } }

        [HttpPost, Route(Route + "/files"), RequestSizeLimit(50_000_000)]
        public Task<ActionResult> UploadFile([FromForm] UploadForm form) => Run(() =>
        {
            var file = form.file; var table = form.table; var recordId = form.recordId;
            if (file == null || file.Length == 0) throw new AiException("Choose a file to upload.");
            using var s = file.OpenReadStream();
            return _ai.Upload(Me(), table?.ToLowerInvariant(), recordId, file.FileName, file.ContentType, s);
        });

        [HttpGet, Route(Route + "/files")]
        public Task<ActionResult> Files([FromQuery] string table, [FromQuery] string recordId) =>
            Run(() => _ai.ListFiles(Me(), table?.ToLowerInvariant(), recordId));

        [HttpGet, Route(Route + "/files/{id:int}/content")]
        public ActionResult Download(int id)
        {
            try
            {
                var (path, name, type) = _ai.FileContent(Me(), id);
                return PhysicalFile(path, type, name);
            }
            catch (AiException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete, Route(Route + "/files/{id:int}")]
        public Task<ActionResult> DeleteFile(int id) => Run(() => { _ai.DeleteFile(Me(), id); return new { ok = true }; });

        // ------------------------------------------------------------------ assistant

        public sealed class AskRequest
        {
            public int? ConversationId { get; set; }
            public string Question { get; set; }
            public string Table { get; set; }
            public string RecordId { get; set; }
        }

        [HttpPost, Route(Route + "/ask")]
        public Task<ActionResult> Ask([FromBody] AskRequest req, CancellationToken ct) =>
            Run(async () => (object)await _ai.Ask(Me(), req.ConversationId, req.Question, req.Table?.ToLowerInvariant(), req.RecordId, ct));

        [HttpGet, Route(Route + "/conversations")]
        public Task<ActionResult> Conversations() => Run(() => _ai.Conversations(Me()));

        [HttpGet, Route(Route + "/conversations/{id:int}")]
        public Task<ActionResult> Messages(int id) => Run(() => _ai.Messages(Me(), id));

        [HttpDelete, Route(Route + "/conversations/{id:int}")]
        public Task<ActionResult> DeleteConversation(int id) => Run(() => { _ai.DeleteConversation(Me(), id); return new { ok = true }; });

        // ------------------------------------------------------------------ workflows

        public sealed class WorkflowRequest
        {
            public string RecordId { get; set; }
            public string Note { get; set; }
            public JsonArray Records { get; set; }
        }

        [HttpPost, Route(Route + "/workflows/{id}/preview")]
        public Task<ActionResult> Preview(string id, [FromBody] WorkflowRequest req, CancellationToken ct) =>
            Run(async () => (object)await _ai.PreviewWorkflow(Me(), id, req?.RecordId, req?.Note, ct));

        [HttpPost, Route(Route + "/workflows/{id}/apply")]
        public Task<ActionResult> Apply(string id, [FromBody] WorkflowRequest req) =>
            Run(() => _ai.ApplyWorkflow(Me(), id, req?.RecordId, req?.Records));
    }
}
