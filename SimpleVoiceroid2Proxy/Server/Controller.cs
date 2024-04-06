using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace SimpleVoiceroid2Proxy.Server;

public sealed class Controller
{
    public async Task HandleAsync(HttpContext context)
    {
        try
        {
            using (context)
            {
                switch (context.Request.Url.AbsolutePath)
                {
                    case "/talk" when context.Request.HttpMethod == "GET":
                        await HandleGetTalkAsync(context);
                        return;
                    case "/talk" when context.Request.HttpMethod == "POST":
                        await HandlePostTalkAsync(context);
                        return;
                    case "/talk" when context.Request.HttpMethod == "OPTIONS":
                        HandleOptionsTalk(context);
                        return;
                    default:
                        await context.RespondJson(HttpStatusCode.NotFound, new Dictionary<string, object?>{
                            {"success", false},
                            {"message", "not found."},
                        });
                        return;
                }
            }
        }
        catch (Exception exception)
        {
            ConsoleLogger.Instance.Error(exception, "internal server error occurred");

            await context.RespondJson(HttpStatusCode.InternalServerError, new Dictionary<string, object?>{
                {"success", false},
                {"message", "internal server error occurred."},
            });
        }
    }

    private async Task HandleGetTalkAsync(HttpContext context)
    {
        await HandleTalkAsync(context, context.Query["text"]);
    }

    private async Task HandlePostTalkAsync(HttpContext context)
    {
        var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, object?>>(context.Request.InputStream);
        await HandleTalkAsync(context, (string?)payload!["text"]);
    }

    private async Task HandleTalkAsync(HttpContext context, string? text)
    {
        context.Response.AddHeader("Content-Type", "application/json");
        context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        context.Response.AddHeader("Access-Control-Allow-Method", "GET,POST");
        context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

        if (string.IsNullOrWhiteSpace(text))
        {
            await context.RespondJson(HttpStatusCode.BadRequest, new Dictionary<string, object?>
            {
                {"success", false},
                {"message", "`text` parameter is null or empty."},
            });
            return;
        }

        await Program.VoiceroidEngine.TalkAsync(text!);
        await context.RespondJson(HttpStatusCode.OK, new Dictionary<string, object?>
        {
            {"success", true},
            {"message", $"Talked `{text}`."},
            {"text", text},
        });
    }

    private void HandleOptionsTalk(HttpContext context)
    {
        context.Response.AddHeader("Access-Control-Allow-Origin", "*");
        context.Response.AddHeader("Access-Control-Allow-Method", "GET, POST, OPTIONS");
        context.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
        context.Response.AddHeader("Access-Control-Max-Age", "7200");
        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
    }
}
