using System.Net;

namespace OIO.Api.Extensions;

public static class HttpExtension
{
    extension(HttpContext httpContext)
    {
        public IPAddress GetIpAddress()
        {
            return httpContext.Connection.RemoteIpAddress ?? IPAddress.None;
        }

        
    }

    extension(HttpRequest request)
    {
        public string GetUserAgent()
        {
            return request.Headers.UserAgent.ToString();
        }
    }
}