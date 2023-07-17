using crypto;
using Microsoft.Net.Http.Headers;
using System;
using System.Net.Http;
using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using static System.TimeSpan;

namespace Nop.Plugin.Shipping.UPS.Services
{
    public static class HttpClientExtensions
    {
        public static void PrepareRequest(this HttpClient httpClient,
            HttpRequestMessage request, UPSSettings upsSettings, string accessToken = null)
        {
            if (httpClient == null) 
                throw new ArgumentNullException(nameof(httpClient));
            
            if (request == null) 
                throw new ArgumentNullException(nameof(request));
            
            if (upsSettings == null) 
                throw new ArgumentNullException(nameof(upsSettings));

            httpClient.Timeout = FromSeconds(upsSettings.RequestTimeout ?? UPSDefaults.RequestTimeout);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, UPSDefaults.UserAgent);

            request.Headers.Add(HeaderNames.Authorization,
                string.IsNullOrEmpty(accessToken)
                    ? $"Basic {upsSettings.ClientId}:{upsSettings.ClientSecret}"
                    : $"Bearer {accessToken}");

            //save debug info
            if (!upsSettings.Tracing) 
                return;

            var logger = EngineContext.Current.Resolve<ILogger>();
            logger.Information($"UPS rates. Request: {request.Content?.ReadAsStringAsync().Result}");
        }

        public static void ProcessResponse(this HttpClient httpClient, HttpResponseMessage response, UPSSettings upsSettings)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (upsSettings == null)
                throw new ArgumentNullException(nameof(upsSettings));

            //save debug info
            if (!upsSettings.Tracing)
                return;

            var logger = EngineContext.Current.Resolve<ILogger>();
            logger.Information($"UPS rates. Response: {response.Content.ReadAsStringAsync().Result}");
        }
    }
}
