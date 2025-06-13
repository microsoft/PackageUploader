using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.UI.Utility
{
    public interface IProcessStarterService
    {
        Process Start(string fileName, string arguments);
        Process Start(ProcessStartInfo processStartInfo);
    }
    public class ProcessStarterService : IProcessStarterService
    {
        public Process Start(string fileName, string arguments)
        {
            return Process.Start(fileName, arguments);
        }

        public Process Start(ProcessStartInfo processStartInfo)
        {
            return Process.Start(processStartInfo);
        }
    }
}
