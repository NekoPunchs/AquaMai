// ***********************************************************************
//  文件名：         CookiesHelper.cs
//  创建日期：       2026/06/22
//  作者：           NekoPunch!
//  ***********************************************************************

using System.Collections;
using System.Net;
using System.Reflection;
using MelonLoader;

namespace AquaMai.Core.Helpers
{
    public static class CookiesHelper
    {
        public static void PrintAllCookies(CookieContainer container)
        {
            var table = (Hashtable)typeof(CookieContainer).GetField("m_domainTable", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(container);

            foreach (DictionaryEntry entry in table)
            {
                var domain = entry.Key as string;
                var pathList = entry.Value.GetType().GetField("m_list", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(entry.Value) as SortedList;

                foreach (DictionaryEntry pathEntry in pathList)
                {
                    var cookieCollection = pathEntry.Value as CookieCollection;
                    if (cookieCollection == null)
                    {
                        continue;
                    }

                    foreach (Cookie cookie in cookieCollection)
                    {
                        MelonLogger.Msg($"Name={cookie.Name}, Value={cookie.Value}, Path={cookie.Path}, Domain={cookie.Domain}");
                    }
                }
            }
        }
    }
}