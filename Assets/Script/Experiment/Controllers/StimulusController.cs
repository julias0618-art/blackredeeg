using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// ✅ XR 타입 충돌 방지 alias
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using UnityEngine.XR;


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

    [Header("VR Controller Settings")]
    [Tooltip("Thumbstick 방향 인식 임계값 (0~1)")]
    public float thumbstickThreshold = 0.7f;

    TrialType trial = TrialType.None;
    MoveTask selMove;
    ZoomTask selZoom;

    bool executed = false;   // 실행까지 끝났는지
    bool decided  = false;   // DECIDE 마커까지 보냈는지
    bool busy     = false;

    int currentRunId = 0;
    Coroutine flowCo;
    int trialId = 0;         // DECIDE 순간에만 증가

    // VR 입력 상태(릴리즈/새 입력 감지)
    bool prevTriggerPressed = false;
    bool prevAPressed = false;
    bool prevBPressed = false;
    bool prevThumbstickActive = false;

    string CurrentLabel()
    {
        return (trial == TrialType.Move) ? $"MOVE_{selMove}" :
               (trial == TrialType.Zoom) ? $"ZOOM_{selZoom}" :
               "";
    }

    void Update()
    {
        if (busy) return;

        HandleKeyboardInput();
        HandleVRControllerInput();
    }

    bool Pressed(KeyControl k) => k != null && k.wasPressedThisFrame;

    // -------------------------
    // Keyboard
    // -------------------------
    void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

        // 선택 프리뷰(DECIDE 전까지만)
        if (!decided && !executed)
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
            TriggerSpaceFromLSL();
        }
    }

    // -------------------------
    // Meta Quest (Unity XR)
    // -------------------------
    void HandleVRControllerInput()
    {
        // 오른손 컨트롤러
        XRInputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);


        // (선택) 연결상태 디버그
        // if (Time.frameCount % 60 == 0)
        //     Debug.Log($"[VR Debug] RightHand isValid: {rightHand.isValid}, name: {rightHand.name}");

        if (!rightHand.isValid) return;

        // ✅ DECIDE 전까지만 "선택 프리뷰" 입력 허용
        if (!decided && !executed)
        {
            // 1) Thumbstick → Move 방향 프리뷰
            Vector2 thumbstick = Vector2.zero;
            if (rightHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out thumbstick))
            {
                bool thumbstickActive = thumbstick.magnitude > thumbstickThreshold;

                if (thumbstickActive && !prevThumbstickActive)
                {
                    // 가장 큰 축 기준으로 방향 결정
                    if (Mathf.Abs(thumbstick.x) > Mathf.Abs(thumbstick.y))
                    {
                        trial = TrialType.Move;
                        selMove = (thumbstick.x > 0) ? MoveTask.Right : MoveTask.Left;
                        cueUI?.ShowMoveCue(selMove);
                        Debug.Log($"[VR] Thumbstick → {selMove}");
                    }
                    else
                    {
                        trial = TrialType.Move;
                        selMove = (thumbstick.y > 0) ? MoveTask.Up : MoveTask.Down;
                        cueUI?.ShowMoveCue(selMove);
                        Debug.Log($"[VR] Thumbstick → {selMove}");
                    }
                }

                prevThumbstickActive = thumbstickActive;
            }

            // 2) A 버튼 → ZoomIn 프리뷰
            bool aPressed = false;
            if (rightHand.TryGetFeatureValue(XRCommonUsages.primaryButton, out aPressed))
            {
                if (aPressed && !prevAPressed)
                {
                    trial = TrialType.Zoom;
                    selZoom = ZoomTask.ZoomIn;
                    cueUI?.ShowZoomCue(selZoom);
                    Debug.Log("[VR] A Button → Zoom In");
                }
            }
            prevAPressed = aPressed;

            // 3) B 버튼 → ZoomOut 프리뷰
            bool bPressed = false;
            if (rightHand.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bPressed))
            {
                if (bPressed && !prevBPressed)
                {
                    trial = TrialType.Zoom;
                    selZoom = ZoomTask.ZoomOut;
                    cueUI?.ShowZoomCue(selZoom);
                    Debug.Log("[VR] B Button → Zoom Out");
                }
            }
            prevBPressed = bPressed;
        }

        // 4) Trigger → Space 역할(DECIDE/EXEC/RESET)
        bool triggerPressed = false;
        if (rightHand.TryGetFeatureValue(XRCommonUsages.triggerButton, out triggerPressed))
        {
            if (triggerPressed && !prevTriggerPressed)
            {
                Debug.Log("[VR] Trigger Pressed");
                TriggerSpaceFromLSL();
            }
        }
        prevTriggerPressed = triggerPressed;
    }

    public void TriggerSpaceFromLSL()
    {
        if (busy) return;

        // 1) DECIDE (첫 트리거)  ← decided 기준으로!
        if (!executed && !decided)
        {
            if (trial == TrialType.None)
                AssignRandomTrial();

            ShowSelection();

            trialId++;
            decided = true;   // ✅ 이거 꼭 필요

            string label = CurrentLabel();
            markerSender?.SendDecide(trialId, label);

            Debug.Log($"[DECIDE] (LSL) trial={trialId} {label}");
            return;
        }

        // 2) EXEC (두 번째)
        if (!executed && decided)
        {
            string label = CurrentLabel();
            markerSender?.SendExec(trialId, label);

            if (flowCo != null) StopCoroutine(flowCo);
            flowCo = StartCoroutine(CoExecuteOnce());

            Debug.Log($"[EXEC] (LSL) trial={trialId} {label}");
            return;
        }

        // 3) RESET (세 번째)
        if (executed)
        {
            markerSender?.SendReset(trialId);

            if (flowCo != null) StopCoroutine(flowCo);
            flowCo = StartCoroutine(CoReturnToCenter());

            Debug.Log($"[RESET] (LSL) trial={trialId}");
            return;
        }
    }



    

    

    // -------------------------
    // Trial selection helpers
    // -------------------------
    void AssignRandomTrial()
{
    trial = (Random.value < 0.5f) ? TrialType.Move : TrialType.Zoom;

    if (trial == TrialType.Move)
    {
        // Left/Right/Up/Down 랜덤
        MoveTask[] pool = { MoveTask.Left, MoveTask.Right, MoveTask.Up, MoveTask.Down };
        selMove = pool[Random.Range(0, pool.Length)];
    }
    else
    {
        // ZoomIn/ZoomOut 랜덤
        selZoom = (Random.value < 0.5f) ? ZoomTask.ZoomIn : ZoomTask.ZoomOut;
    }
}

    void ShowSelection()
    {
        if (trial == TrialType.Move) cueUI?.ShowMoveCue(selMove);
        else if (trial == TrialType.Zoom) cueUI?.ShowZoomCue(selZoom);
        else cueUI?.Hide();
    }

    bool IsRunValid(int runId) => runId == currentRunId;

    // -------------------------
    // Execution
    // -------------------------
    IEnumerator CoExecuteOnce()
    {
        busy = true;

        currentRunId++;
        int runId = currentRunId;

        // 남아있는 실행 정지
        moveRunner?.Stop();
        zoomRunner?.Stop();

        if (trial == TrialType.Move)
        {
            // Move는 공 + 타겟
            zoomRunner?.RestoreCameraBaseline(restoreFov: true);
            moveRunner?.PrepareForMove();
            moveRunner?.PlaceMoveTarget(selMove);

            cueUI?.ShowMoveCue(selMove);

            moveRunner?.Run(selMove, moveDuration, runId, IsRunValid, null);
        }
        else // Zoom
        {
            // Zoom은 카메라
            moveRunner?.Stop();
            moveRunner?.PrepareForMove();
            zoomRunner?.PrepareForZoom();

            cueUI?.ShowZoomCue(selZoom);

            zoomRunner?.Run(selZoom, cueSec, runId, IsRunValid, null);
        }

        yield return new WaitForSeconds(cueSec);
        cueUI?.Hide();

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

        zoomRunner?.RestoreCameraBaseline(restoreFov: true);
        moveRunner?.PrepareForMove();

        yield return null;

        // ✅ 상태 초기화
        executed = false;
        decided  = false;
        trial    = TrialType.None;
        cueUI?.Hide();

        // ✅ (선택) VR 입력 이전 상태 초기화해주면 다음 trial 입력이 더 안정적
        prevThumbstickActive = false;
        prevAPressed = false;
        prevBPressed = false;
        // prevTriggerPressed는 Update에서 다시 잡히므로 굳이 안 건드려도 됨

        busy = false;
    }
}
