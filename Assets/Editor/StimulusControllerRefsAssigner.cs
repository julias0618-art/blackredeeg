#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// StimulusController Refs를 드래그 없이 자동 연결합니다.
/// 메뉴: Tools > Assign StimulusController Refs
/// </summary>
public static class StimulusControllerRefsAssigner
{
    const string MenuPath = "Tools/Assign StimulusController Refs";

    [MenuItem(MenuPath)]
    public static void AssignRefs()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("[StimulusControllerRefs] 열린 씬이 없습니다.");
            return;
        }

        var gm = GameObject.Find("GameManager");
        if (gm == null)
        {
            Debug.LogWarning("[StimulusControllerRefs] Hierarchy에 'GameManager'가 없습니다.");
            return;
        }

        var stim = gm.GetComponent<StimulusController>();
        if (stim == null)
        {
            Debug.LogWarning("[StimulusControllerRefs] GameManager에 StimulusController가 없습니다.");
            return;
        }

        var canvas = GameObject.Find("Canvas");
        var blackball = GameObject.Find("Blackball");
        var mainCam = GameObject.Find("Main Camera");

        Undo.RecordObject(stim, "Assign StimulusController Refs");

        if (canvas != null)
        {
            var cue = canvas.GetComponent<CuePresenter>();
            if (cue != null) stim.cueUI = cue;
            else Debug.LogWarning("[StimulusControllerRefs] Canvas에 CuePresenter가 없습니다.");
        }
        else Debug.LogWarning("[StimulusControllerRefs] 'Canvas'를 찾을 수 없습니다.");

        if (blackball != null)
        {
            var move = blackball.GetComponent<MoveTaskRunner>();
            if (move != null) stim.moveRunner = move;
            else Debug.LogWarning("[StimulusControllerRefs] Blackball에 MoveTaskRunner가 없습니다.");
        }
        else Debug.LogWarning("[StimulusControllerRefs] 'Blackball'를 찾을 수 없습니다.");

        if (mainCam != null)
        {
            var zoom = mainCam.GetComponent<ZoomTaskRunner>();
            if (zoom != null) stim.zoomRunner = zoom;
            else Debug.LogWarning("[StimulusControllerRefs] Main Camera에 ZoomTaskRunner가 없습니다.");
        }
        else Debug.LogWarning("[StimulusControllerRefs] 'Main Camera'를 찾을 수 없습니다.");

        var marker = gm.GetComponent<LSLMarkerSender>();
        if (marker != null) stim.markerSender = marker;
        else Debug.LogWarning("[StimulusControllerRefs] GameManager에 LSLMarkerSender가 없습니다.");

        EditorUtility.SetDirty(stim);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[StimulusControllerRefs] Refs 할당 완료. 씬 저장(Ctrl+S) 권장.");
    }
}
#endif
