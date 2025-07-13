using System.Diagnostics.CodeAnalysis;
using FlowStudio.Map;
using HarmonyLib;
using MapNametags.Behaviours;
using UnityEngine;

namespace MapNametags.Patches;

public static class NamedPlayerMarkerPatches
{
    private const string LocalPlayerLabelName = "LocalPlayerAbsoluteLabel";
    
    private static RectTransform CanvasRectTransform => MapUIManager.Instance.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
    
    private static string GetRemotePlayerLabelName(string key)
    {
        var playerId = key.StartsWith("Client_")
            ? key[7..]
            : key;
        
        return $"RemotePlayerAbsoluteLabel_{playerId}";
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.Init))]
    public static void InitPostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
        MapUIManager __instance)
    {
        AddLocalPlayerLabelBehaviour();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.Open))]
    public static void MapOpenPostfix() => ToggleNametags(true);

    [HarmonyPostfix, HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.Close))]
    public static void MapClosePostfix() => ToggleNametags(false);

    private static void ToggleNametags(bool visible)
    {
        MapNametagsPlugin.Logger.LogDebug($"ToggleNametags called; setting to {visible}");
        var canvasRT = CanvasRectTransform;
        if (canvasRT == null)
        {
            MapNametagsPlugin.Logger.LogError("CanvasRT not found");
            return;
        }
        
        MapNametagsPlugin.Logger.LogDebug("Attempting to set nametag visibility");
        foreach (var label in canvasRT.GetComponentsInChildren<AbsoluteMapLabelBehaviour>(true))
        {
            MapNametagsPlugin.Logger.LogDebug($"[ToggleNametags] Found label: {label.name}, activeSelf={label.gameObject.activeSelf}, parentActive={label.transform.parent.gameObject.activeSelf}");
            label.gameObject.SetActive(visible);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MapManager), nameof(MapManager.RemoveMarkerInstance))]
    public static void RemoveMarkerInstancePostfix(string key)
    {
        if (!key.StartsWith("Client_")) return;

        var canvasRT = CanvasRectTransform;
        if (canvasRT == null)
        {
            return;
        }
        
        MapNametagsPlugin.Logger.LogDebug($"Removing marker for {key}");
        
        var nameToRemove = GetRemotePlayerLabelName(key);
        var label = canvasRT.Find(nameToRemove);
        if (label == null) return;
        
        Object.Destroy(label.gameObject);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.Update))]
    public static void UpdateClampMarkerTiltPostfix(
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
        MapUIManager __instance)
    {
        var rt = __instance.m_playerReference;
        if (rt == null) return;

        var e = rt.localEulerAngles;
        rt.localEulerAngles = new Vector3(0f, 0f, e.z);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MapManager), nameof(MapManager.SpawnMapMarker))]
    public static void SpawnMapMarkerPostfix(
        string key,
        byte type)
    {
        var markerType = (MarkerType)type;

        if (markerType != MarkerType.Client)
            return;

        var marker = MapManager.Instance.GetMarker(key);
        if (marker == null) return;
        var markerRT = marker.GetComponent<RectTransform>();
        if (markerRT == null) return;

        var canvasRT = CanvasRectTransform;
        if (canvasRT == null) return;

        var name = GetRemotePlayerLabelName(key);
        if (canvasRT.Find(name) != null) return;

        var go = new GameObject(name,
            typeof(RectTransform),
            typeof(AbsoluteMapLabelBehaviour)
        );
        go.transform.SetParent(canvasRT, false);

        var lbl = go.GetComponent<AbsoluteMapLabelBehaviour>();

        lbl.Initialize(
            canvasRT,
            markerRT,
            16f,
            () =>
            {
                var playerId = key.StartsWith("Client_")
                    ? key[7..]
                    : key;
             
                return NetworkBootstrapper.instance.networkPlayers
                    .TryGetPlayerData(playerId, out var pd)
                    ? pd.username
                    : playerId;
            });
    }

    private static void AddLocalPlayerLabelBehaviour()
    {
        var ui = MapUIManager.Instance;
        var canvasRT = CanvasRectTransform;
        var markerRT = ui.m_playerReference;
        if (canvasRT == null || markerRT == null) return;

        if (canvasRT.Find(LocalPlayerLabelName) != null) return;
        var go = new GameObject(LocalPlayerLabelName,
            typeof(RectTransform),
            typeof(AbsoluteMapLabelBehaviour)
        );
        go.transform.SetParent(canvasRT, false);

        var playerNameLabel = go.GetComponent<AbsoluteMapLabelBehaviour>();
        playerNameLabel.Initialize(
            canvasRT,
            markerRT,
            16f,
            () => "You"
        );
    }
}