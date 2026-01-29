using TMPro;
using UnityEngine;

public class CuePresenter : MonoBehaviour
{
    public TMP_Text cueText;
    public TMP_Text zoomText;

    [Header("XR")]
    public bool isXR = false;
    public float xrFontScale = 1.3f;

    float cueBaseSize;
    float zoomBaseSize;

    void Awake()
    {
        if (cueText != null) cueBaseSize = cueText.fontSize;
        if (zoomText != null) zoomBaseSize = zoomText.fontSize;
    }

    public void ShowMoveCue(StimulusController.MoveTask m)
    {
        if (cueText != null)
        {
            ApplySize(cueText, cueBaseSize);
            cueText.text = m switch
            {
                StimulusController.MoveTask.Left  => "←",
                StimulusController.MoveTask.Right => "→",
                StimulusController.MoveTask.Up    => "↑",
                StimulusController.MoveTask.Down  => "↓",
                _ => ""
            };
        }
        if (zoomText != null) zoomText.text = "";
    }

    public void ShowZoomCue(StimulusController.ZoomTask z)
    {
        if (cueText != null) cueText.text = "";
        if (zoomText != null)
        {
            ApplySize(zoomText, zoomBaseSize);
            zoomText.text = (z == StimulusController.ZoomTask.ZoomIn) ? "ZOOM +" : "ZOOM -";
        }
    }

    public void Hide()
    {
        if (cueText != null) cueText.text = "";
        if (zoomText != null) zoomText.text = "";
    }

    void ApplySize(TMP_Text text, float baseSize)
    {
        if (text == null) return;
        float size = baseSize > 0f ? baseSize : text.fontSize;
        text.enableAutoSizing = false;
        text.fontSize = isXR ? size * Mathf.Max(1f, xrFontScale) : size;
    }
}
