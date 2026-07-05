using System.Diagnostics;
using System.Runtime.InteropServices;
using AquaMai.Config.Attributes;
using HarmonyLib;
using Main;
using MelonLoader;
using UnityEngine;

namespace AquaMai.Mods.Utils
{
    [ConfigSection("狂暴引擎",
                      "Some tricks to prevent the system from lagging",
                      "可能缓解掉帧，但也可能把狂暴转移到用户身上")]
    public class AntiLag : MonoBehaviour
    {
        [ConfigEntry("后台运行", zh: "游戏未取得焦点时也运行")]
        private static readonly bool activateWhileBackground = false;

        [ConfigEntry("高优先级", zh: "将游戏设为高优先级")] private static readonly bool setHighPriority = true;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMainObject), "Awake")]
        public static void OnGameMainObjectAwake()
        {
            var go = new GameObject("妙妙防掉帧");
            go.AddComponent<AntiLag>();
            if (setHighPriority)
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            }
        }

        private void Awake() => InvokeRepeating(nameof(OnTimer), 10f, 10f);

        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void keybd_event(uint bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const int VK_CONTROL = 17;

        private void OnTimer()
        {
#if DEBUG
            MelonLogger.Msg("[AntiLag] Trigger");
#endif
            if (Application.isFocused || activateWhileBackground)
            {
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYDOWN, 0);
                keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
            }
        }
    }
}