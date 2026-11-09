using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Attributes
{
    /// <summary>
    /// Per-role hidden fields (ui.config.json "hiddenFields") on the table endpoints /v1/api/{table}...: removed from every
    /// response, and refused as filter or sort columns (so values can't be probed). Writes are handled by DataGuard.
    /// </summary>
    public sealed class FieldAccessFilter : IAsyncActionFilter, IAsyncResultFilter
    {
        readonly JsonSerializerOptions _json;
        public FieldAccessFilter(IOptions<JsonOptions> json) => _json = json.Value.JsonSerializerOptions;

        static string TableOf(Microsoft.AspNetCore.Http.HttpRequest r)
        {
            var parts = (r.Path.Value ?? "").Trim('/').Split('/');
            // v1/api/{table}[/...]
            return parts.Length >= 3 && parts[1].Equals(APIEndpoint.APIRoute, StringComparison.OrdinalIgnoreCase) && DataGuard.AppTables.Contains(parts[2])
                ? parts[2].ToLowerInvariant() : null;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
        {
            var table = TableOf(ctx.HttpContext.Request);
            var hidden = table == null ? null : DataGuard.HiddenFields(table);
            if (hidden?.Count > 0)
            {
                var used = ctx.ActionArguments.Values.OfType<IEnumerable<FilterModel>>().SelectMany(f => f).Select(f => f?.ColumnName)
                    .Concat(((string)ctx.HttpContext.Request.Query["orderBy"] ?? "").Split(',').Select(o => o.Trim().Split(' ')[0]))
                    .Where(c => !string.IsNullOrEmpty(c) && hidden.Contains(c)).ToList();
                if (used.Count > 0)
                {
                    ctx.Result = new ObjectResult(new APIResponse(ResponseCode.ERROR, $"You can't filter or sort by {string.Join(", ", used)}.")) { StatusCode = 403 };
                    return;
                }
            }
            await next();
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext ctx, ResultExecutionDelegate next)
        {
            var table = TableOf(ctx.HttpContext.Request);
            if (table != null && ctx.Result is ObjectResult r && r.Value != null && DataGuard.HiddenFields(table).Count > 0)
                r.Value = DataGuard.StripHidden(table, JsonSerializer.SerializeToNode(r.Value, r.Value.GetType(), _json));
            await next();
        }
    }
}
