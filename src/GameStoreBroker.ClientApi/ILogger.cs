using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStoreBroker.ClientApi
{
    public interface ILogger
    {
        Task LogLineAsync(string message);
        Task LogVerboseAsync(string message);
        Task LogWarningAsync(string message);
        Task LogErrorAsync(string message);
    }
}
