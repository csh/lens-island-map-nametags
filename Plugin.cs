using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using FlowStudio.Map;
using HarmonyLib;
using MapNametags.Patches;
using UnityEngine.SceneManagement;

namespace MapNametags;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MapNametagsPlugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;

    private static readonly HashSet<string> NonGameScenes =
    [
        "Boot",
        "Intro",
        "MainMenu",
        "LoadingScreen"
    ];

    private bool _inGameScene;
    private bool _wasMapOpen;
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
        
        SceneManager.sceneLoaded += OnSceneLoaded;
        _inGameScene = !NonGameScenes.Contains(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_inGameScene && scene.name == "MainMenu")
        {
            NametagManager.RemoveAllLabels();
        }
        _inGameScene = !NonGameScenes.Contains(scene.name);
    }

    private void Update()
    {
        if (!_inGameScene || MapUIManager.Instance is null) return;
        
        var isOpen = MapUIManager.IsOpen;
        if (isOpen == _wasMapOpen) return;
        NametagManager.SetAllLabelsVisible(isOpen);
        _wasMapOpen = isOpen;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _harmony?.UnpatchSelf();
        NametagManager.RemoveAllLabels();
    }
}