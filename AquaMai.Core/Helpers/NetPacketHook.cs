using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using HarmonyLib;
using Manager;
using MelonLoader;
using MelonLoader.TinyJSON;
using Net;
using Net.Packet;
using Net.Packet.Helper;
using Net.Packet.Mai2;
using Net.VO.Mai2;

namespace AquaMai.Core.Helpers
{
    public class NetPacketHook
    {
        [HarmonyPrefix, HarmonyPatch(typeof(NetHttpClient), "CheckServerHash")]
        public static bool PreCheckServerHash(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, ref bool __result)
        {
            MelonLogger.Msg("PreCheckServerHash hook.");
            __result = true;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(TicketManager), "RequestUserCharge")]
        public static bool PreRequestUserCharge(TicketManager __instance, int playerId)
        {
            MelonLogger.Msg("RequestUserCharge Hook: " + playerId);
            var userData = MAI2.Util.Singleton<UserDataManager>.Instance.GetUserData(playerId);
            var userID = userData.Detail.UserID;
            var connectTicketId = Traverse.Create(__instance).Field<TicketConnectData[]>("_connectTicketData").Value[playerId].connectTicketId;
            PacketHelper.StartPacket(new PacketUploadUserChargelog(playerId,
                                                                   userID,
                                                                   userData.ExportEnableUserCharge(connectTicketId),
                                                                   userData.
                                                                       ExportUserChargelog(connectTicketId),
                                                                   x => OnDone(x, playerId),
                                                                   x => OnError(x, playerId)));
            return false;
        }

        private static void OnDone(int code, int playerId)
        {
            MelonLogger.Msg("TicketManager OnDone code: " + code);
            if (code != 1)
            {
                Thread.Sleep(1000);
                PreRequestUserCharge(TicketManager.Instance, playerId);
                return;
            }

            typeof(TicketManager).GetMethod("RequestSuccess", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(TicketManager.Instance, [ playerId ]);
        }

        private static void OnError(PacketStatus error, int playerId) =>
            typeof(TicketManager).GetMethod("RequestFail", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(TicketManager.Instance, [ playerId ]);

        [HarmonyPrefix, HarmonyPatch(typeof(Packet), "ProcImpl")]
        public static void PreProcImpl(Packet __instance)
        {
            if (__instance.State == PacketState.Process && Traverse.Create(__instance).Field("Client").GetValue() is NetHttpClient client && client.State == NetHttpClient.StateDone)
            {
                var netQuery = __instance.Query;
                var api = Shim.RemoveApiSuffix(netQuery.Api);

                var req = Traverse.Create(client).Field<HttpWebRequest>("_request").Value;
                MelonLogger.Msg(api + " Req Cookie:");
                CookiesHelper.PrintAllCookies(req.CookieContainer);

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