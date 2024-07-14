using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

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
        await HandleTalkAsync(context, context.Query.GetValues("text")?.FirstOrDefault());
    }

    private async Task HandlePostTalkAsync(HttpContext context)
    {
        string? text = null;
        if (context.RequestMediaType == "application/x-www-form-urlencoded")
        {
            using var reader = new StreamReader(context.Request.InputStream);
            var body = reader.ReadToEnd();
            var form = HttpUtility.ParseQueryString(body);
            text = form.GetValues("text")?.FirstOrDefault();
        }
        else
        {
            var payload = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(context.Request.InputStream);
            text = payload!["text"].GetString();
        }

        await HandleTalkAsync(context, text);
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
