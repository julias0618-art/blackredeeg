using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;


using static StimulusController;


public class StimulusController : MonoBehaviour
{
    public enum TrialType { None, Move, Zoom }
    public enum MoveTask  { Left, Right, Up, Down }
    public enum ZoomTask  { ZoomIn, ZoomOut }

    [Header("Timing")]
    public float cueSec = 1.2f;
    public float moveDuration = 1.2f;

    [Header("Refs")]
    public CuePresenter cueUI;
    public MoveTaskRunner moveRunner;
    public ZoomTaskRunner zoomRunner;
    public LSLMarkerSender markerSender;

    TrialType trial = TrialType.None;
    MoveTask selMove;
    ZoomTask selZoom;

    bool executed = false;
    bool busy = false;

    int currentRunId = 0;
    Coroutine flowCo;


    void Update()
    {
        if (Keyboard.current == null || busy) return;

        // 수동 선택(센터 대기일 때만)
        if (!executed)
        {
            if (Pressed(Keyboard.current.leftArrowKey))  { trial = TrialType.Move; selMove = MoveTask.Left;  cueUI?.ShowMoveCue(selMove); }
            if (Pressed(Keyboard.current.rightArrowKey)) { trial = TrialType.Move; selMove = MoveTask.Right; cueUI?.ShowMoveCue(selMove); }
            if (Pressed(Keyboard.current.upArrowKey))    { trial = TrialType.Move; selMove = MoveTask.Up;    cueUI?.ShowMoveCue(selMove); }
            if (Pressed(Keyboard.current.downArrowKey))  { trial = TrialType.Move; selMove = MoveTask.Down;  cueUI?.ShowMoveCue(selMove); }

            if (Pressed(Keyboard.current.iKey)) { trial = TrialType.Zoom; selZoom = ZoomTask.ZoomIn;  cueUI?.ShowZoomCue(selZoom); }
            if (Pressed(Keyboard.current.oKey)) { trial = TrialType.Zoom; selZoom = ZoomTask.ZoomOut; cueUI?.ShowZoomCue(selZoom); }
        }

        if (Pressed(Keyboard.current.spaceKey))
        {
            // 1) 랜덤 선택만
            if (!executed && trial == TrialType.None)
            {
                AssignRandomTrial();
                ShowSelection();
                Debug.Log("[SELECT]");
                return;
            }

            // 2) 실행용 Space (✅ 여기서만 마커 전송)
            if (!executed && trial != TrialType.None)
            {
                string label = (trial == TrialType.Move)
                ? $"MOVE_{selMove}"
                : $"ZOOM_{selZoom}";

                // ✅ 오직 여기서만 전송
                markerSender?.SendOnSpace($"EXEC_{label}");

                if (flowCo != null) StopCoroutine(flowCo);
                flowCo = StartCoroutine(CoExecuteOnce());

                Debug.Log($"[EXECUTE] {label}");
                return;
            }       

            // 3) 복귀
            // 3) 복귀용 Space (❌ 마커 안 보냄)
            if (executed)
            {
                if (flowCo != null) StopCoroutine(flowCo);
                flowCo = StartCoroutine(CoReturnToCenter());

                Debug.Log("[RETURN]");
                return;
            }       
        }
    }

    bool Pressed(KeyControl k) => k != null && k.wasPressedThisFrame;

    void AssignRandomTrial()
    {
        trial = (Random.value < 0.5f) ? TrialType.Move : TrialType.Zoom;
        if (trial == TrialType.Move) selMove = GetRandomMove();
        else                         selZoom = GetRandomZoom();
    }

    MoveTask GetRandomMove()
    {
        MoveTask[] pool = { MoveTask.Left, MoveTask.Right, MoveTask.Up, MoveTask.Down };
        return pool[Random.Range(0, pool.Length)];
    }

    ZoomTask GetRandomZoom()
    {
        return (Random.value < 0.5f) ? ZoomTask.ZoomIn : ZoomTask.ZoomOut;
    }

    void ShowSelection()
    {
        if (trial == TrialType.Move) cueUI?.ShowMoveCue(selMove);
        else if (trial == TrialType.Zoom) cueUI?.ShowZoomCue(selZoom);
        else cueUI?.Hide();
    }

    bool IsRunValid(int runId) => runId == currentRunId;

    IEnumerator CoExecuteOnce()
    {
        busy = true;

        currentRunId++;
        int runId = currentRunId;

        // 남아있는 실행 정지
        moveRunner?.Stop();
        zoomRunner?.Stop();

        // ✅ 모달리티별 “준비 단계”에서 서로의 영역을 확실히 정리
        if (trial == TrialType.Move)
        {
            // Move는 공만 + 타겟 배치, Zoom 잔상(카메라)은 ZoomRunner가 baseline으로 복구시켜둠
            zoomRunner?.RestoreCameraBaseline(restoreFov: true); // Move 시작 전 카메라 baseline 강제
            moveRunner?.PrepareForMove();
            moveRunner?.PlaceMoveTarget(selMove);

            cueUI?.ShowMoveCue(selMove);

            moveRunner?.Run(selMove, moveDuration, runId, IsRunValid, null);
        }
        else // Zoom
        {
            // Zoom은 카메라만 + Follow 잠금, 공 이동은 MoveRunner가 멈춰있어야 함
            moveRunner?.Stop();
            moveRunner?.PrepareForMove(); // 검은공 센터로 (줌에서는 움직이면 안 되니까)
            zoomRunner?.PrepareForZoom();

            cueUI?.ShowZoomCue(selZoom);

            zoomRunner?.Run(selZoom, cueSec, runId, IsRunValid, null);
        }

        yield return new WaitForSeconds(cueSec);
        cueUI?.Hide();

        // Move는 cueSec 이후에도 이동 남을 수 있음 -> 남은 시간 대기
        if (trial == TrialType.Move)
        {
            float remain = Mathf.Max(0f, moveDuration - cueSec);
            if (remain > 0f) yield return new WaitForSeconds(remain);
        }

        executed = true;
        busy = false;
    }

    IEnumerator CoReturnToCenter()
    {
        busy = true;

        moveRunner?.Stop();
        zoomRunner?.Stop();

        // baseline 복구
        zoomRunner?.RestoreCameraBaseline(restoreFov: true);
        moveRunner?.PrepareForMove();

        yield return null;

        executed = false;
        trial = TrialType.None;
        cueUI?.Hide();

        busy = false;
    }


    public void TriggerSpaceFromLSL()
    {
        if (busy) return;

        // 아래 로직은 Update()에서 space 눌렀을 때와 동일하게 복붙
        if (!executed && trial == TrialType.None)
        {
            AssignRandomTrial();
            ShowSelection();
            Debug.Log("[SELECT] (LSL)");
            return;
        }

        if (!executed && trial != TrialType.None)
        {
            string label = (trial == TrialType.Move)
                ? $"MOVE_{selMove}"
                : $"ZOOM_{selZoom}";

            markerSender?.SendOnSpace($"EXEC_{label}"); // (원하면 이 줄은 OFF 가능)
            if (flowCo != null) StopCoroutine(flowCo);
            flowCo = StartCoroutine(CoExecuteOnce());

            Debug.Log($"[EXECUTE] {label} (LSL)");
            return;
        }

        if (executed)
        {
            if (flowCo != null) StopCoroutine(flowCo);
            flowCo = StartCoroutine(CoReturnToCenter());

            Debug.Log("[RETURN] (LSL)");
            return;
        }
    }


}


