using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace LeaderSurvey.Extensions
{
    public static class HttpRequestExtensions
    {
        public static bool IsAjaxRequest(this HttpRequest request)
        {
            if (request == null)
            {
                return false;
            }

            return request.Headers["X-Requested-With"].ToString().Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }
    }
}
