using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace USCAssociation.RobotClient.Tools.Extensions
{
    public static class StringExtensions
    {
        public static List<string> FindStringListInText(this string text, List<string> strings)
        {
            var list = new List<string>();
            foreach (var str in strings)
            {
                if (text.Contains(str))
                {
                    list.Add(str);
                }
            }

            return list;
        }

        public static string Abbreviate(this string str, int length)
        {
            if (str == null)
            {
                return "";
            }
            if (str.Length < length * 2.5)
            {
                return str;
            }
            else
            {
                return string.Concat(str.AsSpan(0, (int)(length * 2.5)), "...");
            }
        }

        public static string MidStrEx(this string sourse, string startstr, string endstr)
        {
            var result = string.Empty;
            int startindex, endindex;
            try
            {
                startindex = sourse.IndexOf(startstr);
                if (startindex == -1)
                {
                    return result;
                }

                var tmpstr = sourse[(startindex + startstr.Length)..];
                endindex = tmpstr.IndexOf(endstr);
                if (endindex == -1)
                {
                    return result;
                }

                result = tmpstr.Remove(endindex);
            }
            catch
            {
                //Log.WriteLog("MidStrEx Err:" + ex.Message);
            }
            return result;
        }

        public static string DeleteHtmlLinks(this string value)
        {
            value = value.Replace("</a>", "");

            while (true)
            {
                var temp = value.MidStrEx("<a ", ">");

                if (string.IsNullOrWhiteSpace(temp))
                {
                    break;
                }

                value = value.Replace("<a " + temp + ">", "");

            }

            return value;
        }

        public static List<string> GetLinks(this string value)
        {
            return Regex.Matches(value, "http[s]?://(?:(?!http[s]?://)[a-zA-Z]|[0-9]|[$\\-_@.&+/]|[!*\\(\\),]|(?:%[0-9a-fA-F][0-9a-fA-F]))+").Select(s => s.ToString().Trim()).ToList();
        }
    }
}
