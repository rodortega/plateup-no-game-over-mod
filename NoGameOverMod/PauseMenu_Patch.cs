using HarmonyLib;
using Kitchen;
using System;
using System.Reflection;

namespace NoGameOver
{
    [HarmonyPatch(typeof(MainMenu), "Setup")]
    class MainMenu_Patch
    {
        public static bool Prefix(MainMenu __instance)
        {
            if (GameInfo.CurrentScene == SceneType.Kitchen && !GameInfo.IsPreparationTime)
            {
                MethodInfo requestAction = __instance.GetType().GetMethod("RequestAction", BindingFlags.NonPublic | BindingFlags.Instance);
                MethodInfo addButton = __instance.GetType().GetMethod("AddButton", BindingFlags.NonPublic | BindingFlags.Instance);

                Action<int> action = (_) => {
                    PatchController.CustomOfferRestart(out bool shouldRunOriginal);
                    requestAction.Invoke(__instance, new object[] { new MenuAction(PauseMenuAction.CloseMenu) });
                };

                addButton.Invoke(__instance, new object[] { "Restart Day", action, 0, 1f, 0.2f });
            }

            return true;
        }
    }
}