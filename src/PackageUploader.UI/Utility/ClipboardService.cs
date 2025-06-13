using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.UI.Utility
{

    public interface IClipboardService
    {
        void SetData(string dataFormat, object data);
    }

    public class ClipboardService : IClipboardService
    {
        public void SetData(string dataFormat, object data)
        {
            System.Windows.Clipboard.SetData(dataFormat, data);
        }
    }
}
