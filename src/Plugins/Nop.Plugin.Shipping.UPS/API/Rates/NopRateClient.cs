using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Nop.Plugin.Shipping.UPS.Services;

namespace Nop.Plugin.Shipping.UPS.API.Rates
{
    public partial class RateClient
    {

        private UPSSettings _upsSettings;
        private string _accessToken;

        public RateClient(HttpClient httpClient, UPSSettings upsSettings, string accessToken) : this(httpClient)
        {
            _upsSettings = upsSettings;
            _accessToken = accessToken;

            if (!_upsSettings.UseSandbox)
                BaseUrl = UPSDefaults.ApiUrl;
        }

        partial void PrepareRequest(HttpClient client, HttpRequestMessage request,
            string url)
        {
            client.PrepareRequest(request, _upsSettings, _accessToken);
        }

        public async Task<RateResponse> ProcessRateAsync(RateRequest request)
        {
            var response = await RatingAsync("v1", "Rate", new RATERequestWrapper
            {
                RateRequest = request
            });

            return response.RateResponse;
        }
    }
}
