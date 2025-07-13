using System;
using FlowStudio.Map;
using TMPro;
using UnityEngine;

namespace MapNametags.Behaviours;

[DisallowMultipleComponent]
public class AbsoluteMapLabelBehaviour : MonoBehaviour
{
    private RectTransform _labelRt;
    private RectTransform _canvasRt;
    private Canvas _canvas;
    private RectTransform _markerRt;
    private float _pixelOffset;
    private Func<string> _getText;
    private TextMeshProUGUI _tmp;

    public void Initialize(
        RectTransform canvasRt,
        RectTransform markerRt,
        float pixelOffset,
        Func<string> getText)
    {
        _canvasRt = canvasRt;
        _canvas = canvasRt.GetComponent<Canvas>();
        _markerRt = markerRt;
        _pixelOffset = pixelOffset;
        _getText = getText;

        // initialize text now that getText is set
        if (_tmp != null && _getText != null)
        {
            _tmp.text = _getText();
        }

        gameObject.SetActive(MapUIManager.IsOpen);
    }

    private void Awake()
    {
        _labelRt = gameObject.GetComponent<RectTransform>();

        // anchors & pivot so anchoredPosition is center-bottom
        _labelRt.anchorMin = _labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        _labelRt.pivot = new Vector2(0.5f, 0f);
        _labelRt.sizeDelta = new Vector2(120f, 30f);

        // CanvasRenderer
        gameObject.AddComponent<CanvasRenderer>();

        // TMP setup
        _tmp = gameObject.AddComponent<TextMeshProUGUI>();
        _tmp.fontSize = 13.5f;
        _tmp.alignment = TextAlignmentOptions.Center;
        _tmp.raycastTarget = false;
        // Do not set text here; wait until Initialize
    }

    private void LateUpdate()
    {
        if (_markerRt == null || _canvasRt == null)
        {
            MapNametagsPlugin.Logger.LogWarning("Label LateUpdate: missing marker/canvas");
            return;
        }
        
        // Compute absolute screen->local pos
        var cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _canvas.worldCamera;

        var screenPt = RectTransformUtility.WorldToScreenPoint(cam, _markerRt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, screenPt, cam, out var localPt
        );

        localPt.y += _pixelOffset;
        _labelRt.anchoredPosition = localPt;
    }
}