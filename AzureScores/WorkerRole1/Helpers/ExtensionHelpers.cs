using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WorkerRole1.Helpers
{
    public static class ExtensionHelpers
    {
        public static string CleanHtml(this string str)
        {
            str = Regex.Replace(str, @"(&nbsp;)", "");
            return str;
        }
    }
}
