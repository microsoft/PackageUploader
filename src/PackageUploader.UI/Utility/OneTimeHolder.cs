using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PackageUploader.UI.Utility
{
    public class OneTimeHolder<T> where T : class
    {
        private T? _value;
        public T? Value
        {
            get
            {
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
