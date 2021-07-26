using GameStoreBroker.ClientApi.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GameStoreBroker.ClientApi.Xfus
{
    public sealed class XfusHttpClient : HttpRestClient
    {
        public XfusHttpClient(ILogger<XfusHttpClient> logger, HttpClient httpClient) : base(logger, httpClient)
        {
            // will be config driven when features implemented

            //httpClient.BaseAddress = new Uri(xfusUploadInfo.UploadDomain + "/api/v2/assets/");
            //httpClient.Timeout = TimeSpan.FromMilliseconds(this.uploadConfig.HttpUploadTimeoutMs);
            //httpClient.DefaultRequestHeaders.Add("Tenant", config.Tenant);
            //var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(xfusUploadInfo.Token));

            //httpClient.BaseAddress = new Uri("/api/v2/assets/");
            //httpClient.Timeout = TimeSpan.FromSeconds(10);

            //var authToken = Convert.ToBase64String(Encoding.UTF8.GetBytes("mytoken"));

            //httpClient.DefaultRequestHeaders.Add("Authorization", authToken);
            //httpClient.DefaultRequestHeaders.Add("Tenant", "Xfus");
        }
    }
}
