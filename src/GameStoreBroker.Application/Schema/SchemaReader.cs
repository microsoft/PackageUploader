using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GameStoreBroker.Application.Schema
{
    public class SchemaReader<T> where T : BaseOperationSchema
    {
        public async Task<T> DeserializeFile(string path)
        {
            var rawJson = await File.ReadAllTextAsync(path);

            var schema = JsonConvert.DeserializeObject<T>(rawJson);
            return schema;
        }
    }
}
