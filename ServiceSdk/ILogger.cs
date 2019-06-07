using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceSdk
{
    public interface ILogger
    {
        void Info(string msg);

        void Warn(string msg);

        void Error(string msg, Exception e = null);

        void Fatial(string msg, Exception e = null);
    }
}
