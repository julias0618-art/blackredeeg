using TMPro;
using UnityEngine;

public class CuePresenter : MonoBehaviour
{
    public TMP_Text cueText;
    public TMP_Text zoomText;

    void Start()
    {
        // 디버그: 텍스트 필드 연결 상태 확인
        Debug.Log($"[CuePresenter] cueText={cueText != null}, zoomText={zoomText != null}");
    }

    public void ShowMoveCue(StimulusController.MoveTask m)
    {
        Debug.Log($"[CuePresenter] ShowMoveCue: {m}, cueText={cueText != null}");
        
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
            Debug.Log($"[CuePresenter] cueText.text = '{cueText.text}'");
        }
        if (zoomText != null) zoomText.text = "";
    }

    public void ShowZoomCue(StimulusController.ZoomTask z)
    {
        Debug.Log($"[CuePresenter] ShowZoomCue: {z}, zoomText={zoomText != null}");
        
        if (cueText != null) cueText.text = "";
        if (zoomText != null)
        {
            zoomText.enableAutoSizing = false;
            zoomText.fontSize = 36;
            zoomText.text = (z == StimulusController.ZoomTask.ZoomIn) ? "ZOOM +" : "ZOOM -";
            Debug.Log($"[CuePresenter] zoomText.text = '{zoomText.text}'");
        }
    }

    public void Hide()
    {
        if (cueText != null) cueText.text = "";
        if (zoomText != null) zoomText.text = "";
    }
}
