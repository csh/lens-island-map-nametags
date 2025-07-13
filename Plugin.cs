using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MapNametags.Behaviours;
using MapNametags.Patches;

namespace MapNametags;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MapNametagsPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    private Harmony _harmony;

    private void Awake()
    {
        Logger = base.Logger;
        DontDestroyOnLoad(gameObject);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(NamedPlayerMarkerPatches));

        foreach (var patchedMethod in _harmony.GetPatchedMethods())
        {
            Logger.LogDebug($"Patched {patchedMethod.DeclaringType?.Name}.{patchedMethod.Name}");
        }
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        
        foreach (var label in FindObjectsOfType<AbsoluteMapLabelBehaviour>())
        {
            Destroy(label.gameObject);
        }
    }
}