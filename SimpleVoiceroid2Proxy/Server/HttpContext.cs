using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace SimpleVoiceroid2Proxy.Server;

public sealed class HttpContext(HttpListenerContext Context) : IDisposable
{
    public HttpListenerRequest Request => Context.Request;
    public HttpListenerResponse Response => Context.Response;
    public NameValueCollection Query => HttpUtility.ParseQueryString(Request.Url.Query, Encoding.UTF8);

    public async Task RespondJson(HttpStatusCode code, Dictionary<string, object?> payload)
    {
        await JsonSerializer.SerializeAsync(Context.Response.OutputStream, payload);
        Response.StatusCode = (int)code;
    }

    public void Dispose()
    {
        Response.Close();
    }
}
