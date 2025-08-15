using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;  
using static UnityEngine.UI.Image;

namespace CustomDash;

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class Plugin : BaseUnityPlugin
{
    public static ConfigEntry<KeyCode> KeyConfig { get; private set; }
    public static KeyCode Key;
    internal static new ManualLogSource Logger;

    public void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        Harmony harmony = new(PluginInfo.GUID);
        harmony.Patch(
            original: AccessTools.Method(typeof(PlayerController), "Update"),
            postfix: new HarmonyMethod(typeof(Dash_Patch), nameof(Dash_Patch.Dash))
            );
        KeyConfig = Config.Bind("General", "Dash Key", KeyCode.LeftControl, "Key to do a grounded dash");
        Key = KeyConfig.Value;
    }

}
public class Dash_Patch
{

    public static void Dash(PlayerController __instance)
    {
        // Prevent game from updating scores to leaderboards
        Game.mod = true;
        Plugin.Logger.LogInfo($"Mod active = {Game.mod}");
        float dashSpeed = 0f;
        if (__instance.grounder.grounded) dashSpeed = 70f;
        if (Input.GetKeyDown(Plugin.Key))
        {
            float x = 0f;
            float z = 0f;

            if (Input.GetKey(KeyCode.W)) z += 1f;
            if (Input.GetKey(KeyCode.S)) z -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;

            // Get local movement direction and keep original vertical velocity
            Vector3 currentVelocity = __instance.rb.velocity;
            Vector3 inputDir = (__instance.tHead.forward * z) + (__instance.tHead.right * x);
            inputDir.y = 0f;

            // Only dash on input, grounded and not holding kick (holding kick has weird traction)
            if (inputDir != Vector3.zero && __instance.grounder.grounded && !Input.GetKey(__instance.kickKey))
            {
                inputDir.Normalize();
                Vector3 newVelocity = inputDir * dashSpeed;
                newVelocity.y = currentVelocity.y;
                __instance.rb.velocity = newVelocity;
                QuickEffectsPool.Get("Recover Jump FX", __instance.headPos + newVelocity.normalized * 3f, Quaternion.LookRotation(newVelocity)).Play(-1f, 0);
            }
        }
    }
}