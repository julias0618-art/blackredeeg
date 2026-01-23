using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class StimulusPresenter_ManualStep : MonoBehaviour
{
    public enum TaskType { None, Left, Right, Up, Down, ZoomIn, ZoomOut }

    [Header("Scene Objects")]
    public Transform blackBall;
    public Transform targetBall;
    public Camera mainCamera;

    [Header("UI (TextMeshPro)")]
    public TMP_Text cueText;   // ← → ↑ ↓
    public TMP_Text zoomText;  // "ZOOM +", "ZOOM -"

    [Header("Timing (seconds)")]
    public float cueSec = 1.2f;      // 화살표/줌 텍스트 유지 시간
    public float holdAfterSec = 0.0f; // (선택) 동작 후 자동으로 더 기다릴 시간 (0이면 즉시 멈춤)

    [Header("World Layout")]
    public float height = 0.5f;
    public float distance = 1.6f;

    [Header("Ball Scale (Zoom)")]
    public float normalScale = 0.25f;
    public float zoomBigScale = 0.55f;
    public float zoomSmallScale = 0.15f;
    public float zoomCenterYOffset = 0.15f;

    [Header("Black Ball Movement (direction only)")]
    public float moveDuration = 1.2f;  // 검은공 이동 시간
    public float stopDist = 0.25f;     // 빨간공 앞에서 멈추기(겹침 방지)

    [Header("Camera Follow")]
    public CameraFollow camFollow;    // 있으면 방향일 때만 enabled, 복귀 시 ResetToStart()

    [Header("Cue Placement")]
    public bool placeCueOnLineSafe = true;

    // ===== 내부 상태 =====
    TaskType selected = TaskType.None; // 방향키 누르기 전까지 None
    bool waitingReturn = false;        // 실행 후 멈춘 상태면 true
    bool busy = false;                 // 실행/복귀 중 입력 막기

    void Start()
    {
        ResetToCenterInstant();
        UpdateSelectionUI(); // 처음엔 아무것도 안 보이게 됨
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (busy) return;

        // 1) 실행 전(센터 상태)일 때만: 방향/줌 선택 가능
        if (!waitingReturn)
        {
            if (WasPressed(Keyboard.current.leftArrowKey)) { selected = TaskType.Left; UpdateSelectionUI(); }
            if (WasPressed(Keyboard.current.rightArrowKey)) { selected = TaskType.Right; UpdateSelectionUI(); }
            if (WasPressed(Keyboard.current.upArrowKey)) { selected = TaskType.Up; UpdateSelectionUI(); }
            if (WasPressed(Keyboard.current.downArrowKey)) { selected = TaskType.Down; UpdateSelectionUI(); }

            if (WasPressed(Keyboard.current.iKey)) { selected = TaskType.ZoomIn; UpdateSelectionUI(); }
            if (WasPressed(Keyboard.current.oKey)) { selected = TaskType.ZoomOut; UpdateSelectionUI(); }
        }

        // 2) Space 처리: (a) 실행 or (b) 복귀
        if (WasPressed(Keyboard.current.spaceKey))
        {
            if (!waitingReturn)
            {
                // 방향키/줌키를 누르지 않았으면 아무것도 안 함
                if (selected == TaskType.None) return;

                StartCoroutine(RunOnce(selected));
            }
            else
            {
                StartCoroutine(ReturnToCenter());
            }
        }
    }

    bool WasPressed(KeyControl key) => key != null && key.wasPressedThisFrame;

    IEnumerator RunOnce(TaskType task)
    {
        busy = true;

        bool isDirection = (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down);

        // 카메라 follow: 방향일 때만 ON
        if (camFollow != null)
            camFollow.enabled = isDirection;

        // 1) 타겟/줌 배치
        ShowTargetOrZoom(task);

        // 2) cue 표시
        ShowCue(task);

        // (선택) cue 안전 배치
        if (isDirection && placeCueOnLineSafe)
            PlaceCueOnLine_Safe();

        // 3) 방향이면 검은공 이동
        Coroutine moveCo = null;
        if (isDirection)
            moveCo = StartCoroutine(MoveBlackToTarget(moveDuration));

        // 4) cueSec 후 텍스트 숨김(공은 유지)
        yield return new WaitForSeconds(cueSec);
        HideTextsOnly();

        // 5) (선택) 추가로 조금 더 멈추고 싶으면
        if (holdAfterSec > 0f)
            yield return new WaitForSeconds(holdAfterSec);

        // 이동 코루틴 정리
        if (moveCo != null) StopCoroutine(moveCo);

        // ✅ 여기서 "멈춤 상태"로 전환 (아무것도 자동으로 안 바뀜)
        waitingReturn = true;
        busy = false;
    }

    IEnumerator ReturnToCenter()
    {
        busy = true;

        // 카메라 복귀
        if (camFollow != null)
        {
            camFollow.enabled = false;
            camFollow.ResetToStart();
        }

        // 한 프레임 기다려서 카메라 리셋 반영
        yield return null;

        ResetToCenterInstant();

        // 복귀 후엔 다시 "선택 전" 상태로
        waitingReturn = false;
        selected = TaskType.None;
        UpdateSelectionUI();

        busy = false;
    }

    void ResetToCenterInstant()
    {
        if (blackBall != null)
            blackBall.position = new Vector3(0, height, 0);

        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(false);
            targetBall.localScale = Vector3.one * normalScale;
            targetBall.position = new Vector3(0, height, 0);
        }

        HideTextsOnly();
    }

    IEnumerator MoveBlackToTarget(float seconds)
    {
        if (blackBall == null || targetBall == null) yield break;

        Vector3 start = blackBall.position;
        Vector3 tgt = targetBall.position;

        start.y = height;
        tgt.y = height;

        Vector3 dir = tgt - start;
        dir.y = 0f;

        if (dir.sqrMagnitude < 1e-6f) yield break;

        float dist = dir.magnitude;
        Vector3 end = start + dir.normalized * Mathf.Max(0f, dist - stopDist);
        end.y = height;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, seconds));
            blackBall.position = Vector3.Lerp(start, end, a);
            yield return null;
        }
        blackBall.position = end;
    }

    void UpdateSelectionUI()
    {
        // 선택 안 했으면 아무것도 안 보이게
        if (selected == TaskType.None)
        {
            if (cueText != null) cueText.text = "";
            if (zoomText != null) zoomText.text = "";
            return;
        }

        // 선택된 방향/줌을 "미리보기"로 보여주고 싶으면 유지
        ShowCue(selected);
    }

    void ShowCue(TaskType task)
    {
        bool isDir = (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down);

        if (cueText != null)
        {
            cueText.text = isDir ? (task switch
            {
                TaskType.Left => "←",
                TaskType.Right => "→",
                TaskType.Up => "↑",
                TaskType.Down => "↓",
                _ => ""
            }) : "";
        }

        if (zoomText != null)
        {
            zoomText.enableAutoSizing = false;
            zoomText.fontSize = 36;

            zoomText.text = !isDir ? (task switch
            {
                TaskType.ZoomIn => "ZOOM +",
                TaskType.ZoomOut => "ZOOM -",
                _ => ""
            }) : "";
        }
    }

    void HideTextsOnly()
    {
        if (cueText != null) cueText.text = "";
        if (zoomText != null) zoomText.text = "";
    }

    void ShowTargetOrZoom(TaskType task)
    {
        if (targetBall == null) return;

        Vector3 center = new Vector3(0, height, 0);

        // 방향 과제: 타겟을 축 절편에 정확히
        if (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down)
        {
            targetBall.gameObject.SetActive(true);
            targetBall.localScale = Vector3.one * normalScale;

            Vector3 offset = task switch
            {
                TaskType.Right => new Vector3(+distance, 0, 0),
                TaskType.Left => new Vector3(-distance, 0, 0),
                TaskType.Up => new Vector3(0, 0, +distance),
                TaskType.Down => new Vector3(0, 0, -distance),
                _ => Vector3.zero
            };

            targetBall.position = center + offset;
            return;
        }

        // 줌 과제: 중앙에서 크기 변화
        targetBall.gameObject.SetActive(true);
        targetBall.position = new Vector3(0, height + zoomCenterYOffset, 0);

        float to = (task == TaskType.ZoomIn) ? zoomBigScale : zoomSmallScale;
        StartCoroutine(AnimateScale(normalScale, to, cueSec));
    }

    IEnumerator AnimateScale(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
            float s = Mathf.Lerp(from, to, a);
            if (targetBall != null) targetBall.localScale = Vector3.one * s;
            yield return null;
        }
        if (targetBall != null) targetBall.localScale = Vector3.one * to;
    }

    // (옵션) 직선 위 안전 배치: 필요 없으면 placeCueOnLineSafe=false
    void PlaceCueOnLine_Safe()
    {
        if (cueText == null || blackBall == null || targetBall == null || mainCamera == null) return;
        if (!targetBall.gameObject.activeSelf) return;

        Vector2 sb = mainCamera.WorldToScreenPoint(blackBall.position);
        Vector2 st = mainCamera.WorldToScreenPoint(targetBall.position);

        Vector2 v = st - sb;
        float L = v.magnitude;
        if (L < 5f) return;

        Vector2 dir = v / L;

        float minFromBlackPx = 120f;
        float minGapToTargetPx = 140f;
        float preferredFromBlackPx = 180f;

        float dMin = minFromBlackPx;
        float dMax = L - minGapToTargetPx;

        float d = (dMax <= dMin) ? dMin : Mathf.Clamp(preferredFromBlackPx, dMin, dMax);
        Vector2 pos = sb + dir * d;

        if (cueText.text == "↓") pos += Vector2.up * 35f;

        cueText.rectTransform.position = new Vector3(pos.x, pos.y, 0);
    }
}