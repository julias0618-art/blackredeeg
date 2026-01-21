using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class StimulusPresenter : MonoBehaviour
{
    public enum TaskType { Left, Right, Up, Down, ZoomIn, ZoomOut }

    [Header("Scene Objects")]
    public Transform blackBall;
    public Transform targetBall;
    public Camera mainCamera;

    [Header("UI (TextMeshPro)")]
    public TMP_Text cueText;   // ← → ↑ ↓
    public TMP_Text zoomText;  // "ZOOM +", "ZOOM -"

    [Header("Timing (seconds)")]
    public float interTrialSec = 5.0f;
    public float cueSec = 0.7f;
    public float targetSec = 2.5f;

    [Header("World Layout")]
    public float height = 0.5f;
    public float distance = 1.6f;

    [Header("Ball Scale")]
    public float normalScale = 0.25f;
    public float zoomBigScale = 0.55f;
    public float zoomSmallScale = 0.15f;
    public float zoomCenterYOffset = 0.15f;

    [Header("IMPORTANT: Cue Safety (world units)")]
    public float ballRadius = 0.125f;
    public float cueMargin = 0.12f;
    public float cuePreferredFromBlack = 0.45f;

    [Header("Black Ball Movement (Only for Direction Tasks)")]
    public bool moveBlackTowardTarget = true;
    public float moveDuration = 1.2f;
    public bool resetBlackToCenterEachTrial = true;

    [Header("Camera Follow (only for direction tasks)")]
    public CameraFollow camFollow;

    // =========================
    // ✅ Manual Trigger Settings
    // =========================
    [Header("Manual Trigger (Keyboard -> next trial)")]
    public bool enableManualTrigger = true;
    public KeyCode keyLeft = KeyCode.LeftArrow;
    public KeyCode keyRight = KeyCode.RightArrow;
    public KeyCode keyUp = KeyCode.UpArrow;
    public KeyCode keyDown = KeyCode.DownArrow;
    public KeyCode keyZoomIn = KeyCode.I;
    public KeyCode keyZoomOut = KeyCode.O;

    // "다음 트라이얼 1회" 예약
    private bool hasPendingTask = false;
    private TaskType pendingTask;

    // =========================
    // ✅ Random "Shuffle Bag"
    // =========================
    [Header("Random Mode")]
    public bool useShuffleBag = true; // true면 더 랜덤처럼 보임(연속 패턴 줄어듦)

    private List<TaskType> bag = new List<TaskType>();
    private Coroutine loopCo;
    private Coroutine moveCo;

    void Start()
    {
        if (blackBall != null)
            blackBall.position = new Vector3(0, height, 0);

        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(false);
            targetBall.localScale = Vector3.one * normalScale;
        }

        if (cueText != null) cueText.text = "";
        if (zoomText != null)
        {
            zoomText.text = "";
            zoomText.enableAutoSizing = false;
            zoomText.fontSize = 36;
        }

        FillAndShuffleBag();

        loopCo = StartCoroutine(StimulusLoop());
    }

    void Update()
    {
        if (!enableManualTrigger) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // 방향키
        if (kb.leftArrowKey.wasPressedThisFrame) { pendingTask = TaskType.Left; hasPendingTask = true; }
        else if (kb.rightArrowKey.wasPressedThisFrame) { pendingTask = TaskType.Right; hasPendingTask = true; }
        else if (kb.upArrowKey.wasPressedThisFrame) { pendingTask = TaskType.Up; hasPendingTask = true; }
        else if (kb.downArrowKey.wasPressedThisFrame) { pendingTask = TaskType.Down; hasPendingTask = true; }

        // 줌 키: I / O (대문자/소문자 상관 없음)
        else if (kb.iKey.wasPressedThisFrame) { pendingTask = TaskType.ZoomIn; hasPendingTask = true; }
        else if (kb.oKey.wasPressedThisFrame) { pendingTask = TaskType.ZoomOut; hasPendingTask = true; }
    }


    IEnumerator StimulusLoop()
    {
        while (true)
        {
            // 쉬는 시간(베이스라인)
            yield return new WaitForSeconds(interTrialSec);

            // 0) 이번 트라이얼 task 결정: (예약이 있으면 우선)
            TaskType task = GetNextTask();

            bool isDirection =
                (task == TaskType.Left || task == TaskType.Right ||
                 task == TaskType.Up || task == TaskType.Down);

            // 1) 방향 과제면 Follow ON, 줌이면 OFF
            if (camFollow != null)
            {
                camFollow.enabled = isDirection;

                // ✅ 방향 트라이얼이면, 시작 순간 카메라를 즉시 맞춰서 "튕김" 제거
                if (isDirection)
                    camFollow.SnapNow();
            }


            // 2) (선택) 트라이얼 시작마다 검은공 원위치
            if (resetBlackToCenterEachTrial && blackBall != null)
                blackBall.position = new Vector3(0, height, 0);

            // 3) 타겟/줌 배치
            ShowTargetOrZoom(task);

            // 4) 방향 과제(상하좌우)일 때만 검은공 이동
            if (moveBlackTowardTarget && isDirection)
            {
                if (moveCo != null) StopCoroutine(moveCo);
                moveCo = StartCoroutine(MoveBlackToTargetForSeconds(moveDuration));
            }

            // 5) 큐(화살표/줌텍스트) 표시
            ShowCue(task);

            // 6) 방향 과제면 화살표 위치 배치
            if (isDirection)
                PlaceCueOnLine_Safe();

            // 7) cueSec 후 텍스트 숨김(공은 유지)
            yield return new WaitForSeconds(cueSec);
            HideTextsOnly();

            // 8) 남은 시간 동안 공 유지
            float remain = Mathf.Max(0f, targetSec - cueSec);
            if (remain > 0f) yield return new WaitForSeconds(remain);

            // 9) 종료
            HideAll();
            if (camFollow != null)
            {
                camFollow.enabled = false;
                // camFollow.ResetToStart();  // ✅ 일단 주석 처리해서 튕김 줄이기
            }

        }
    }

    TaskType GetNextTask()
    {
        // ✅ 예약이 있으면 우선 실행 (다음 트라이얼 1회)
        if (hasPendingTask)
        {
            hasPendingTask = false;
            return pendingTask;
        }

        // ✅ 기본은 랜덤
        if (!useShuffleBag)
            return (TaskType)Random.Range(0, 6);

        // Shuffle-bag 방식(한 바퀴에 각 task를 1번씩)
        if (bag.Count == 0) FillAndShuffleBag();

        TaskType t = bag[0];
        bag.RemoveAt(0);
        return t;
    }

    void FillAndShuffleBag()
    {
        bag.Clear();
        bag.Add(TaskType.Left);
        bag.Add(TaskType.Right);
        bag.Add(TaskType.Up);
        bag.Add(TaskType.Down);
        bag.Add(TaskType.ZoomIn);
        bag.Add(TaskType.ZoomOut);

        // Fisher–Yates shuffle
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }
    }

    IEnumerator MoveBlackToTargetForSeconds(float seconds)
    {
        if (blackBall == null || targetBall == null) yield break;

        Vector3 start = blackBall.position;

        Vector3 dir = targetBall.position - start;
        dir.y = 0f;

        float stopDist = 0.25f; // 0.2~0.4 추천
        Vector3 end = start;

        if (dir.sqrMagnitude > 1e-6f)
            end = start + dir.normalized * Mathf.Max(0f, dir.magnitude - stopDist);

        start.y = height;
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

    void ShowCue(TaskType task)
    {
        if (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down)
        {
            if (cueText != null)
            {
                cueText.text = task switch
                {
                    TaskType.Left => "←",
                    TaskType.Right => "→",
                    TaskType.Up => "↑",
                    TaskType.Down => "↓",
                    _ => ""
                };
            }
            if (zoomText != null) zoomText.text = "";
            return;
        }

        if (cueText != null) cueText.text = "";
        if (zoomText != null)
            zoomText.text = (task == TaskType.ZoomIn) ? "ZOOM +" : "ZOOM -";
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

    void HideAll()
    {
        if (targetBall != null) targetBall.gameObject.SetActive(false);
        if (cueText != null) cueText.text = "";
        if (zoomText != null) zoomText.text = "";

        if (moveCo != null)
        {
            StopCoroutine(moveCo);
            moveCo = null;
        }

        if (blackBall != null)
            blackBall.position = new Vector3(0, height, 0);

        if (camFollow != null)
        {
            camFollow.enabled = false;
            camFollow.ResetToStart();
        }
    }

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
        bool noRoomOnLine = (dMax <= dMin);

        float d = noRoomOnLine ? dMin : Mathf.Clamp(preferredFromBlackPx, dMin, dMax);
        Vector2 pos = sb + dir * d;

        bool vertical = Mathf.Abs(dir.y) > 0.85f;
        if (vertical)
        {
            Vector2 normal = new Vector2(-dir.y, dir.x);
            float sidePx = 55f;
            pos += normal * sidePx;
        }

        if (cueText.text == "↓")
        {
            pos += Vector2.up * 35f;
        }

        cueText.rectTransform.position = new Vector3(pos.x, pos.y, 0);
    }
}
