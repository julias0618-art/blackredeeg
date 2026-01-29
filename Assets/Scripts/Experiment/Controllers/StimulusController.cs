using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class StimulusController : MonoBehaviour
{
    public enum TrialType { None, Move, Zoom }
    public enum MoveTask  { Left, Right, Up, Down }
    public enum ZoomTask  { ZoomIn, ZoomOut }
    public enum InputMode { Keyboard, XR }

    [Header("Timing")]
    public float cueSec = 1.2f;
    public float moveDuration = 1.2f;

    [Header("Input")]
    public InputMode inputMode = InputMode.Keyboard;
    public InputActionReference selectAction;   // XR "select/confirm"
    public InputActionReference moveLeftAction;
    public InputActionReference moveRightAction;
    public InputActionReference moveUpAction;
    public InputActionReference moveDownAction;
    public InputActionReference zoomInAction;
    public InputActionReference zoomOutAction;

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

    void OnEnable()
    {
        SetActionEnabled(selectAction, true);
        SetActionEnabled(moveLeftAction, true);
        SetActionEnabled(moveRightAction, true);
        SetActionEnabled(moveUpAction, true);
        SetActionEnabled(moveDownAction, true);
        SetActionEnabled(zoomInAction, true);
        SetActionEnabled(zoomOutAction, true);
    }

    void OnDisable()
    {
        SetActionEnabled(selectAction, false);
        SetActionEnabled(moveLeftAction, false);
        SetActionEnabled(moveRightAction, false);
        SetActionEnabled(moveUpAction, false);
        SetActionEnabled(moveDownAction, false);
        SetActionEnabled(zoomInAction, false);
        SetActionEnabled(zoomOutAction, false);
    }


    void Update()
    {
        if (busy) return;
        if (inputMode == InputMode.Keyboard && Keyboard.current == null) return;

        // 수동 선택(센터 대기일 때만)
        if (!executed)
        {
            if (inputMode == InputMode.Keyboard)
            {
                if (Pressed(Keyboard.current.leftArrowKey))  { trial = TrialType.Move; selMove = MoveTask.Left;  cueUI?.ShowMoveCue(selMove); }
                if (Pressed(Keyboard.current.rightArrowKey)) { trial = TrialType.Move; selMove = MoveTask.Right; cueUI?.ShowMoveCue(selMove); }
                if (Pressed(Keyboard.current.upArrowKey))    { trial = TrialType.Move; selMove = MoveTask.Up;    cueUI?.ShowMoveCue(selMove); }
                if (Pressed(Keyboard.current.downArrowKey))  { trial = TrialType.Move; selMove = MoveTask.Down;  cueUI?.ShowMoveCue(selMove); }

                if (Pressed(Keyboard.current.iKey)) { trial = TrialType.Zoom; selZoom = ZoomTask.ZoomIn;  cueUI?.ShowZoomCue(selZoom); }
                if (Pressed(Keyboard.current.oKey)) { trial = TrialType.Zoom; selZoom = ZoomTask.ZoomOut; cueUI?.ShowZoomCue(selZoom); }
            }
            else
            {
                if (Pressed(moveLeftAction))  { trial = TrialType.Move; selMove = MoveTask.Left;  cueUI?.ShowMoveCue(selMove); }
                if (Pressed(moveRightAction)) { trial = TrialType.Move; selMove = MoveTask.Right; cueUI?.ShowMoveCue(selMove); }
                if (Pressed(moveUpAction))    { trial = TrialType.Move; selMove = MoveTask.Up;    cueUI?.ShowMoveCue(selMove); }
                if (Pressed(moveDownAction))  { trial = TrialType.Move; selMove = MoveTask.Down;  cueUI?.ShowMoveCue(selMove); }

                if (Pressed(zoomInAction))  { trial = TrialType.Zoom; selZoom = ZoomTask.ZoomIn;  cueUI?.ShowZoomCue(selZoom); }
                if (Pressed(zoomOutAction)) { trial = TrialType.Zoom; selZoom = ZoomTask.ZoomOut; cueUI?.ShowZoomCue(selZoom); }
            }
        }

        bool selectPressed = inputMode == InputMode.Keyboard
            ? Pressed(Keyboard.current.spaceKey)
            : Pressed(selectAction);

        if (selectPressed)
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
    bool Pressed(InputActionReference a) => a != null && a.action != null && a.action.WasPressedThisFrame();

    void SetActionEnabled(InputActionReference a, bool enabled)
    {
        if (a == null || a.action == null) return;
        if (enabled)
        {
            if (!a.action.enabled) a.action.Enable();
        }
        else
        {
            if (a.action.enabled) a.action.Disable();
        }
    }

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
}
