using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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

    bool executed = false;
    bool busy = false;

    int currentRunId = 0;
    Coroutine flowCo;

    // VR 컨트롤러 입력 상태 추적 (버튼 릴리즈 감지용)
    bool prevTriggerPressed = false;
    bool prevAPressed = false;
    bool prevBPressed = false;
    bool prevThumbstickActive = false;

    void Update()
    {
        if (busy) return;

        // 키보드 입력 처리
        HandleKeyboardInput();

        // VR 컨트롤러 입력 처리
        HandleVRControllerInput();
    }

    void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

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
            ExecuteSpaceAction();
        }
    }

    void HandleVRControllerInput()
    {
        // 오른손 컨트롤러 가져오기 (UnityEngine.XR.InputDevice 명시)
        UnityEngine.XR.InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        
        // 디버그: 컨트롤러 연결 상태 확인 (1초마다 출력)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VR Debug] RightHand isValid: {rightHand.isValid}, name: {rightHand.name}");
        }
        
        if (!rightHand.isValid) return;

        // === Thumbstick으로 Move 방향 선택 ===
        Vector2 thumbstick = Vector2.zero;
        if (!executed && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out thumbstick))
        {
            bool thumbstickActive = thumbstick.magnitude > thumbstickThreshold;

            // Thumbstick이 새로 활성화될 때만 (wasPressedThisFrame 효과)
            if (thumbstickActive && !prevThumbstickActive)
            {
                // 가장 큰 방향 감지
                if (Mathf.Abs(thumbstick.x) > Mathf.Abs(thumbstick.y))
                {
                    // 좌우
                    if (thumbstick.x > 0)
                    {
                        trial = TrialType.Move;
                        selMove = MoveTask.Right;
                        cueUI?.ShowMoveCue(selMove);
                        Debug.Log("[VR] Thumbstick → Right");
                    }
                    else
                    {
                        trial = TrialType.Move;
                        selMove = MoveTask.Left;
                        cueUI?.ShowMoveCue(selMove);
                        Debug.Log("[VR] Thumbstick → Left");
                    }
                }
                else
                {
                    // 상하
                    if (thumbstick.y > 0)
                    {
                        trial = TrialType.Move;
                        selMove = MoveTask.Up;
                        cueUI?.ShowMoveCue(selMove);
                        Debug.Log("[VR] Thumbstick → Up");
                    }
                    else
                    {
                        trial = TrialType.Move;
                        selMove = MoveTask.Down;
                        cueUI?.ShowMoveCue(selMove);
                        Debug.Log("[VR] Thumbstick → Down");
                    }
                }
            }
            prevThumbstickActive = thumbstickActive;
        }

        // === A 버튼 → Zoom In ===
        bool aPressed = false;
        if (!executed && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out aPressed))
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

        // === B 버튼 → Zoom Out ===
        bool bPressed = false;
        if (!executed && rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bPressed))
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

        // === Trigger → 실행/복귀 (Space 역할) ===
        bool triggerPressed = false;
        if (rightHand.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out triggerPressed))
        {
            if (triggerPressed && !prevTriggerPressed)
            {
                Debug.Log("[VR] Trigger Pressed");
                ExecuteSpaceAction();
            }
        }
        prevTriggerPressed = triggerPressed;
    }

    void ExecuteSpaceAction()
    {
        // 1) 랜덤 선택만
        if (!executed && trial == TrialType.None)
        {
            AssignRandomTrial();
            ShowSelection();
            Debug.Log("[SELECT]");
            return;
        }

        // 2) 실행용 (✅ 여기서만 마커 전송)
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

        // 3) 복귀용 (❌ 마커 안 보냄)
        if (executed)
        {
            if (flowCo != null) StopCoroutine(flowCo);
            flowCo = StartCoroutine(CoReturnToCenter());

            Debug.Log("[RETURN]");
            return;
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
        Debug.Log($"[DEBUG] ShowSelection: trial={trial}, cueUI={cueUI != null}");
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

        // 디버그: Runner 참조 확인
        Debug.Log($"[DEBUG] moveRunner={moveRunner != null}, zoomRunner={zoomRunner != null}");

        // 남아있는 실행 정지
        moveRunner?.Stop();
        zoomRunner?.Stop();

        // ✅ 모달리티별 “준비 단계”에서 서로의 영역을 확실히 정리
        if (trial == TrialType.Move)
        {
            Debug.Log($"[DEBUG] Move 실행: {selMove}, blackBall={moveRunner?.blackBall != null}");
            // Move는 공만 + 타겟 배치, Zoom 잔상(카메라)은 ZoomRunner가 baseline으로 복구시켜둠
            zoomRunner?.RestoreCameraBaseline(restoreFov: true); // Move 시작 전 카메라 baseline 강제
            moveRunner?.PrepareForMove();
            moveRunner?.PlaceMoveTarget(selMove);

            cueUI?.ShowMoveCue(selMove);

            moveRunner?.Run(selMove, moveDuration, runId, IsRunValid, null);
        }
        else // Zoom
        {
            Debug.Log($"[DEBUG] Zoom 실행: {selZoom}, mainCamera={zoomRunner?.mainCamera != null}, zoomByFOV={zoomRunner?.zoomByFOV}");
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

        Debug.Log("[RETURN] 원점 복귀 시작");

        // 모든 진행 중인 코루틴 정지
        moveRunner?.Stop();
        zoomRunner?.Stop();

        // 검은 공을 정확히 원점으로 복귀
        moveRunner?.ResetBlackBallToOrigin();

        // 카메라/줌 baseline 복구
        zoomRunner?.RestoreCameraBaseline(restoreFov: true);

        // 한 프레임 대기 후 다시 한번 원점 확인 (안정화)
        yield return null;
        
        // 혹시 모를 drift 방지를 위해 다시 한번 원점으로
        moveRunner?.ResetBlackBallToOrigin();

        yield return null;

        executed = false;
        trial = TrialType.None;
        cueUI?.Hide();

        busy = false;
        
        Debug.Log("[RETURN] 원점 복귀 완료");
    }
}
