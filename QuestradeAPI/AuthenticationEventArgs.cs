using System;
using System.Collections.Generic;
using System.Text;

namespace QuestradeAPI
{
    public class SuccessAuthEventArgs : EventArgs
    {
        public DateTime TokenExpiry;
    }

    public class UnsuccessfulAuthArg : EventArgs
    {
        public System.Net.Http.HttpResponseMessage resp;
    }
}
