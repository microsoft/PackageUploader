using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Chat;

namespace PackageUploader.UI.Utility
{
    class OneTimeHolder<T> where T : class
    {
        private T? _value;
        public T? Value
        {
            get
            {
                // copilot figured this out, scary
                var value = _value; 
                _value = null;
                return value;
            }
        }
        public OneTimeHolder(T value) 
        {
            _value = value;
        }

    }
}
