using AquaMai.Config.Attributes;
using HarmonyLib;
using MAI2.Util;
using MAI2System;
using MelonLoader;
using Net.VO.Mai2;
using Process.Entry.State;

namespace AquaMai.Mods.Tweaks
{
    [ConfigSection("跳过版本检查",
                      "Allow login with higher data version.",
                      """
                      原先如果你的账号版本比当前游戏设定的版本高的话，就会不能登录
                      开了这个选项之后就可以登录了，不过你的账号版本还是会被设定为当前游戏的版本
                      """,
                      defaultOn: true)]
    public class SkipUserVersionCheck
    {
        [HarmonyPrefix, HarmonyPatch(typeof(ConfirmPlay), "IsValidVersion")]
        public static bool IsValidVersion(UserPreviewResponseVO vo, ref bool __result)
        {
            var runtimeRomVersion = Singleton<SystemConfig>.Instance.config.romVersionInfo.versionNo; // 1.55.00
            var runtimeDataVersion = Singleton<SystemConfig>.Instance.config.dataVersionInfo.versionNo; // 1.55.01
            MelonLogger.Msg(runtimeRomVersion.versionString);
            MelonLogger.Msg(runtimeDataVersion.versionString);
            
            var userLastRomVersion = default(VersionNo);
            var userLastDataVersion = default(VersionNo);
            userLastRomVersion.tryParse(vo.lastRomVersion, true);
            userLastDataVersion.tryParse(vo.lastDataVersion, true);
            var romValid = userLastRomVersion.versionCode <= runtimeRomVersion.versionCode;
            var dataValid = userLastDataVersion.versionCode <= runtimeDataVersion.versionCode;
            __result = true;
            return false;
        }
    }
}