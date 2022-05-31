using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace SimpleVoiceroid2Proxy
{
    internal class HttpRequest
    {
        private readonly HttpListenerRequest request;
        private readonly HttpListenerResponse response;
        private readonly StreamWriter writer;

        public HttpRequest(HttpListenerContext context)
        {
            request = context.Request;
            response = context.Response;
            writer = new StreamWriter(response.OutputStream);
        }

        private NameValueCollection Query => HttpUtility.ParseQueryString(request.Url.Query, Encoding.UTF8);

        public async Task HandleAsync()
        {
            using (response)
            {
                response.AddHeader("Content-Type", "application/json");
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Method", "GET");

                using (writer)
                {
                    try
                    {
                        switch (request.Url.AbsolutePath)
                        {
                            case "/talk":
                                await TalkAsync();
                                return;
                            default:
                                await Respond(HttpStatusCode.NotFound, "Request path not found.");
                                return;
                        }
                    }
                    catch (Exception exception)
                    {
                        await Respond(HttpStatusCode.InternalServerError, "Internal error occurred.");

                        Program.Logger.Error(exception);
                    }
                }
            }
        }

        private async Task TalkAsync()
        {
            string? text = null;
            switch (request.HttpMethod)
            {
                case "GET":
                    text = Query["text"];
                    break;
                case "POST":
                {
                    using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                    var content = await reader.ReadToEndAsync();
                    dynamic? json = JsonConvert.DeserializeObject(content);

                    text = json?.text;
                    break;
                }
                case "OPTIONS":
                {
                    response.AddHeader("Access-Control-Allow-Method", "GET, POST, OPTIONS");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                    response.AddHeader("Access-Control-Max-Age", "7200");
                    response.StatusCode = (int) HttpStatusCode.NoContent;
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                await Respond(HttpStatusCode.BadRequest, "`text` parameter is null or empty.");
                return;
            }

            await Program.VoiceroidEngine.TalkAsync(text!);

            await Respond(HttpStatusCode.OK, $"Talked `{text}`.");
        }

        private async Task Respond(HttpStatusCode code, string message)
        {
            var payload = new Dictionary<string, object>
            {
                {"success", code == HttpStatusCode.OK},
                {"message", message},
            };
            var content = JsonConvert.SerializeObject(payload, Formatting.Indented);

            await writer.WriteLineAsync(content);

            response.StatusCode = (int) code;
        }
    }
}
