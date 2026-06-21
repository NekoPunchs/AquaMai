using System.Collections;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using HarmonyLib;
using Manager;
using MelonLoader;
using MelonLoader.TinyJSON;
using Net;
using Net.Packet;

namespace AquaMai.Core.Helpers
{
    public class NetPacketHook
    {
        private static void PrintAllCookies(CookieContainer container)
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

        [HarmonyPrefix, HarmonyPatch(typeof(NetHttpClient), "CheckServerHash")]
        public static bool PreCheckServerHash(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, ref bool __result)
        {
            MelonLogger.Msg("PreCheckServerHash hook."); 
            __result = true;
            return false;
        }
        
        [HarmonyPrefix, HarmonyPatch(typeof(Packet), "ProcImpl")]
        public static void PreProcImpl(Packet __instance)
        {
            if (
                __instance.State == PacketState.Process &&
                Traverse.Create(__instance).Field("Client").GetValue() is NetHttpClient client &&
                client.State == NetHttpClient.StateDone)
            {
                var netQuery = __instance.Query;
                var api = Shim.RemoveApiSuffix(netQuery.Api);
                var responseBytes = client.GetResponse().ToArray();
                var decryptedResponse = Shim.NetHttpClientDecryptsResponse ? responseBytes : Shim.DecryptNetPacketBody(responseBytes);
                var decodedResponse = Encoding.UTF8.GetString(decryptedResponse);
                var responseJson = JSON.Load(decodedResponse);
                var requestJson = JSON.Load(netQuery.GetRequest());

                var req = Traverse.Create(client).Field<HttpWebRequest>("_request").Value;
                MelonLogger.Msg(api + " Req Cookie:");
                PrintAllCookies(req.CookieContainer);

                MelonLogger.Msg(api + " Resp Cookie:");
                foreach (var obj in client.GetCookie())
                {
                    MelonLogger.Msg(obj.ToString());
                }

                MelonLogger.Msg(api + " Header:");
                foreach (string headerName in req.Headers)
                {
                    MelonLogger.Msg($"{headerName}: {req.Headers[headerName]}");
                }
            }
        }
    }
}