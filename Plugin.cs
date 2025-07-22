using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using FlowStudio.Map;
using HarmonyLib;
using MapNametags.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MapNametags;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class MapNametagsPlugin : BaseUnityPlugin
{
    internal static ConfigEntry<bool> DisplayLocalPlayerNametag;
    
    internal new static ManualLogSource Logger;

    private static readonly HashSet<string> NonGameScenes =
    [
        "Boot",
        "Intro",
        "MainMenu",
        "LoadingScreen"
    ];

    private Coroutine _mapCheckRoutine;
    private bool _inGameScene;
    private bool _wasMapOpen;
    private Harmony _harmony;

    private void Awake()
    {
        Logger = base.Logger;
        DontDestroyOnLoad(gameObject);
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");

        DisplayLocalPlayerNametag = Config.Bind("Nametags", "Display Local Player Nametag", true);
        
        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll(typeof(NamedPlayerMarkerPatches));

        foreach (var patchedMethod in _harmony.GetPatchedMethods())
        {
            Logger.LogDebug($"Patched {patchedMethod.DeclaringType?.Name}.{patchedMethod.Name}");
        }
        
        _inGameScene = !NonGameScenes.Contains(SceneManager.GetActiveScene().name);
    }

    private void Start()
    {
        _mapCheckRoutine = StartCoroutine(CheckMapState());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (DisplayLocalPlayerNametag is not null)
        {
            DisplayLocalPlayerNametag.SettingChanged += ToggleLocalPlayerNametagUsage;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (DisplayLocalPlayerNametag is not null)
        {
            DisplayLocalPlayerNametag.SettingChanged -= ToggleLocalPlayerNametagUsage;
        }
    }

    private void ToggleLocalPlayerNametagUsage(object sender, EventArgs e)
    {
        if (_inGameScene == false) return;
        
        var newValue = DisplayLocalPlayerNametag.Value;
        if (newValue)
        {
            NametagManager.CreateLocalPlayerNametag();
        }
        else
        {
            NametagManager.RemoveLabel("LocalPlayerNametag");
        } 
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_inGameScene && scene.name == "MainMenu")
        {
            NametagManager.RemoveAllLabels();
        }
        _inGameScene = !NonGameScenes.Contains(scene.name);
    }

    private IEnumerator CheckMapState()
    {
        var wait = new WaitForSeconds(0.1f);
        while (enabled)
        {
            if (_inGameScene && MapUIManager.Instance is not null)
            {
                var isOpen = MapUIManager.IsOpen;
                if (isOpen != _wasMapOpen)
                {
                    NametagManager.SetAllLabelsVisible(isOpen);
                    _wasMapOpen = isOpen;
                }
            }

            yield return wait;
        }
    }

    private void OnDestroy()
    {
        if (_mapCheckRoutine != null)
        {
            StopCoroutine(_mapCheckRoutine);
        }
        _harmony?.UnpatchSelf();
        NametagManager.RemoveAllLabels();
    }
}