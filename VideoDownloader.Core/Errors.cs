using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.Core
{
    public static class Errors
    {
        public static string ParseErrorMessage(Exception ex)
        {
            if (ex.Message.ToUpperInvariant().Contains("HTTP 403") || ex.Message.ToUpperInvariant().Contains("PAID"))
            {
                return Localization.T("Restricted access! Payable content?");
            }

            return ex.Message;
        }
    }
}
