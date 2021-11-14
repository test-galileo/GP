using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GroupProcessor
{
    static class Extensions
    {

        public static string Left(this string str, int len)
        {
            if (len <= 0 || str.Length == 0)
                return string.Empty;
            if (str.Length <= len)
                return str;
            return str.Substring(0, len);
        }

        public static string Right(this string str, int len)
        {
            if (len <= 0 || str.Length == 0)
                return string.Empty;
            if (str.Length <= len)
                return str;
            return str.Substring(str.Length - len, len);
        }

    }

}
