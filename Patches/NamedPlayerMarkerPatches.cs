using FlowStudio.Map;
using HarmonyLib;
using UnityEngine;

namespace MapNametags.Patches;

public static class NamedPlayerMarkerPatches
{
    [HarmonyPostfix, HarmonyPatch(typeof(MapManager), nameof(MapManager.RemoveMarkerInstance))]
    public static void RemoveMarkerInstancePostfix(string key)
    {
        if (!key.StartsWith("Client_")) return;
        NametagManager.RemoveLabel(key);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.Update))]
    public static void UpdateClampMarkerTiltPostfix()
    {
        var rt = MapUIManager.Instance.m_playerReference;
        if (rt == null) return;

        var e = rt.localEulerAngles;
        rt.localEulerAngles = new Vector3(0f, 0f, e.z);
    }
    
    [HarmonyPostfix, HarmonyPatch(typeof(MapUIManager), nameof(MapUIManager.Init))]
    public static void InitPostfix()
    {
        if (MapNametagsPlugin.DisplayLocalPlayerNametag.Value == false) return;
        NametagManager.CreateLocalPlayerNametag();
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

        NametagManager.CreateNametag(key, markerRT, GetPlayerName, GetColor);
        return;

        Color GetColor()
        {
            var playerId = key.StartsWith("Client_")
                ? key[7..]
                : key;

            return NetworkBootstrapper.instance.networkPlayers.GetPlayerColor(playerId);
        }

        string GetPlayerName()
        {
            var playerId = key.StartsWith("Client_")
                ? key[7..]
                : key;

            return NetworkBootstrapper.instance.networkPlayers.TryGetPlayerData(playerId, out var pd)
                ? pd.username
                : playerId;
        }
    }
}