using TMPro;
using UnityEngine;

public class CuePresenter : MonoBehaviour
{
    public TMP_Text cueText;
    public TMP_Text zoomText;

    public void ShowMoveCue(StimulusController.MoveTask m)
    {
        if (cueText != null)
        {
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
            zoomText.enableAutoSizing = false;
            zoomText.fontSize = 36;
            zoomText.text = (z == StimulusController.ZoomTask.ZoomIn) ? "ZOOM +" : "ZOOM -";
        }
    }

    public void Hide()
    {
        if (cueText != null) cueText.text = "";
        if (zoomText != null) zoomText.text = "";
    }
}
