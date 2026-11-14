using UnityEngine;
using UnityEngine.UI;

public class OrientationLayout : MonoBehaviour
{
    [Header("References")]
    public CanvasScaler scaler;
    public RectTransform leftStick;
    public RectTransform rightStick;

    [Header("Portrait (1080 x 1920 reference)")]
    public Vector2 portraitMargin = new Vector2(200f, 240f);
    public float portraitSize = 260f;

    [Header("Landscape (1920 x 1080 reference)")]
    public Vector2 landscapeMargin = new Vector2(260f, 200f);
    public float landscapeSize = 300f;

    int lastW, lastH;

    void Update()
    {
        if (Screen.width == lastW && Screen.height == lastH) return;
        lastW = Screen.width;
        lastH = Screen.height;
        Apply(Screen.width > Screen.height);
    }

    void Apply(bool landscape)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = landscape
            ? new Vector2(1920f, 1080f)
            : new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Vector2 m = landscape ? landscapeMargin : portraitMargin;
        float size = landscape ? landscapeSize : portraitSize;

        Place(leftStick, new Vector2(0f, 0f), new Vector2(m.x, m.y), size);
        Place(rightStick, new Vector2(1f, 0f), new Vector2(-m.x, m.y), size);
    }

    void Place(RectTransform rt, Vector2 anchor, Vector2 pos, float size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        if (rt.childCount > 0)
        {
            var handle = rt.GetChild(0) as RectTransform;
            handle.sizeDelta = Vector2.one * size * 0.4f;
            handle.anchoredPosition = Vector2.zero;
        }
    }
}