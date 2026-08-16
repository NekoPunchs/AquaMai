// ***********************************************************************
//  文件名：         BoundsFirstDay.cs
//  创建日期：       2026/08/16
//  作者：           NekoPunch!
//  ***********************************************************************

using AquaMai.Config.Attributes;
using HarmonyLib;
using Manager.UserDatas;

namespace AquaMai.Mods.GameSystem
{
    [ConfigSection("始终视为首玩",
                      defaultOn: true,
                      en: "Always treat every play as the first play of the day (always trigger daily login bonus, navi greeting, etc.).",
                      zh: "始终将每次游玩视为当天的首次游玩（每次都触发每日登录盖章、导航角色问候等首次游玩内容）")]
    public static class BoundsFirstDay
    {
        [HarmonyPrefix, HarmonyPatch(typeof(UserDetail), nameof(UserDetail.FirstPlayOnDay), MethodType.Getter)]
        public static bool Prefix(ref bool __result, UserDetail __instance)
        {
            __result = true;
            return false;
        }
    }
}