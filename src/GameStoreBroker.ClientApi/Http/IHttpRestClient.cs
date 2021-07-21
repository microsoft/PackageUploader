using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi.Http
{
    internal interface IHttpRestClient
    {
        Task<T> GetAsync<T>(string subUrl);
    }
}
