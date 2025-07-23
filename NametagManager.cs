using System;
using System.Collections.Generic;
using FlowStudio.Map;
using MapNametags.Behaviours;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MapNametags;

public static class NametagManager
{
    private static readonly Dictionary<string, AbsoluteMapLabelBehaviour> Labels = new();

    private static RectTransform CanvasRT
    {
        get
        {
            var ui = MapUIManager.Instance;
            return ui?.GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
        }
    }

    public static void CreateLocalPlayerNametag()
    {
        CreateNametag(
            "LocalPlayerNametag", 
            MapUIManager.Instance.m_playerReference, 
            () => "You",
            () => Color.red
        );
    }

    public static void CreateNametag(string key, RectTransform markerRT, Func<string> getText, Func<Color> getColor)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));
        if (markerRT == null)
            throw new ArgumentNullException(nameof(markerRT));

        if (Labels.TryGetValue(key, out var label) && label != null) return;

        var canvasRT = CanvasRT;
        if (canvasRT == null)
            throw new InvalidOperationException("CanvasRT is null!");

        var go = new GameObject($"Nametag_{key}",
            typeof(RectTransform),
            typeof(AbsoluteMapLabelBehaviour)
        );
        go.transform.SetParent(canvasRT, false);

        label = go.GetComponent<AbsoluteMapLabelBehaviour>();
        label.MarkerKey = key;

        label.Initialize(
            canvasRT,
            markerRT,
            16f,
            getText,
            getColor
        );

        Labels[key] = label;
    }

    public static void RemoveLabel(string key)
    {
        if (Labels.TryGetValue(key, out var label) && label != null)
        {
            Object.Destroy(label.gameObject);
            Labels.Remove(key);
        }
        else
        {
            // Fallback in case we messed up.
            var canvasRT = CanvasRT;
            if (canvasRT is null) return;
            foreach (var l in canvasRT.GetComponentsInChildren<AbsoluteMapLabelBehaviour>(true))
            {
                if (l.MarkerKey != key) continue;
                Object.Destroy(l.gameObject);
                break;
            }
        }
    }

    public static void SetAllLabelsVisible(bool visible)
    {
        var canvasRT = CanvasRT;
        if (canvasRT is null) return;
        foreach (var label in canvasRT.GetComponentsInChildren<AbsoluteMapLabelBehaviour>(true))
        {
            label.gameObject.SetActive(visible);
        }
    }

    public static void RemoveAllLabels()
    {
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var label in Labels.Values)
        {
            if (label != null)
            {
                Object.Destroy(label.gameObject);
            }
        }

        Labels.Clear();

        // Guard against "oddities" (my shit coding)
        foreach (var label in Object.FindObjectsOfType<AbsoluteMapLabelBehaviour>())
        {
            Object.Destroy(label.gameObject);
        }
    }
}