using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using LSL;

public class StimulusManualController : MonoBehaviour
{
    public enum TaskType { None, Left, Right, Up, Down, ZoomIn, ZoomOut }

    [Header("Scene Objects")]
    public Transform blackBall;
    public Transform targetBall;
    public Camera mainCamera;

    [Header("UI (TextMeshPro)")]
    public TMP_Text cueText;
    public TMP_Text zoomText;

    [Header("Timing")]
    public float cueSec = 1.2f;        // 화살표/줌 텍스트 노출
    public float moveDuration = 1.2f;  // 검은공 이동 시간(방향일 때)
    public float stopDist = 0.25f;     // 빨간공 앞에서 멈추기

    [Header("Layout")]
    public float height = 0.5f;
    public float distance = 1.6f;

    [Header("Zoom")]
    public float normalScale = 0.25f;
    public float zoomBigScale = 0.55f;
    public float zoomSmallScale = 0.15f;
    public float zoomCenterYOffset = 0.15f;

    [Header("Camera Follow (optional)")]
    public CameraFollow camFollow;

    [Header("LSL (Markers)")]
    public bool enableLSL = true;
    public string lslStreamName = "UnityMarkers";
    public string lslStreamType = "Markers";
    public string lslSourceId = "unity_markers_001";
    public bool logLSLToConsole = true; // 콘솔 로그 끄고 싶으면 false

    // ===== State =====
    TaskType selected = TaskType.None;  // 방향키 누르기 전에는 None
    bool executed = false;              // false=센터대기, true=실행 후 멈춤(복귀 대기)
    bool busy = false;
    Coroutine running;

    // ===== LSL =====
    StreamOutlet outlet;

    // ✅ “한 실행(run)당 마커 1회” + “이전 코루틴이 늦게 끝나도 무시”
    int currentRunId = 0;
    bool stopMarkerSent = false;

    // 코루틴 참조(정리용)
    Coroutine moveCo;
    Coroutine scaleCo;

    void Awake()
    {
        StopAllCoroutines();
    }

    void Start()
    {
        if (enableLSL)
        {
            var info = new StreamInfo(
                lslStreamName,
                lslStreamType,
                1,
                0,
                channel_format_t.cf_string,
                lslSourceId
            );
            outlet = new StreamOutlet(info);
            if (logLSLToConsole) Debug.Log($"[LSL] Stream started: {lslStreamName}");
        }

        ResetToCenterInstant();
        HideTexts();
    }

    void Update()
    {
        if (Keyboard.current == null || busy) return;

        // ✅ 실행 전(센터)일 때만 "선택" 입력 받기
        if (!executed)
        {
            if (Pressed(Keyboard.current.leftArrowKey)) { selected = TaskType.Left; ShowSelection(); }
            if (Pressed(Keyboard.current.rightArrowKey)) { selected = TaskType.Right; ShowSelection(); }
            if (Pressed(Keyboard.current.upArrowKey)) { selected = TaskType.Up; ShowSelection(); }
            if (Pressed(Keyboard.current.downArrowKey)) { selected = TaskType.Down; ShowSelection(); }

            if (Pressed(Keyboard.current.iKey)) { selected = TaskType.ZoomIn; ShowSelection(); }
            if (Pressed(Keyboard.current.oKey)) { selected = TaskType.ZoomOut; ShowSelection(); }
        }

        // ✅ Space = 실행/복귀 토글
        if (Pressed(Keyboard.current.spaceKey))
        {
            if (!executed)
            {
                // ⭐ 아무 것도 선택 안 했으면 → 랜덤 선택
                if (selected == TaskType.None)
                {
                    selected = GetRandomTask();
                    ShowSelection();
                }

                if (running != null) StopCoroutine(running);
                running = StartCoroutine(ExecuteOnce(selected));
            }
            else
            {
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(ReturnToCenter());
            }
        }
    }

    bool Pressed(KeyControl k) => k != null && k.wasPressedThisFrame;

    IEnumerator ExecuteOnce(TaskType task)
    {
        busy = true;

        // ✅ 새 실행(run) 시작
        currentRunId++;
        int runId = currentRunId;
        stopMarkerSent = false;

        // 이전 코루틴 정리 (혹시 남아있으면)
        if (moveCo != null) { StopCoroutine(moveCo); moveCo = null; }
        if (scaleCo != null) { StopCoroutine(scaleCo); scaleCo = null; }

        bool isDir = (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down);

        // 카메라 follow는 방향일 때만 ON
        if (camFollow != null)
            camFollow.enabled = isDir;

        // 1) 타겟 배치 (줌이면 스케일 애니메이션 시작)
        PlaceTarget(task, runId);

        // 2) 텍스트(선택된 cue) 보여주기
        ShowCue(task);

        // 3) 방향이면 검은공 이동 시작
        if (isDir)
            moveCo = StartCoroutine(MoveBlackTowardTarget(task, moveDuration, runId));

        // 4) cueSec 후 텍스트 숨기기
        yield return new WaitForSeconds(cueSec);
        HideTexts();

        // 5) 방향이면 이동 끝까지 대기 (마커는 Move 코루틴 끝에서만 전송됨)
        if (isDir)
        {
            float remain = Mathf.Max(0f, moveDuration - cueSec);
            if (remain > 0f) yield return new WaitForSeconds(remain);

            // 최종 위치 보정(기존 동작 유지용)
            SnapBlackToStopPoint();
        }
        else
        {
            // 줌은 scale 코루틴이 cueSec 동안 돌고, 끝에서만 마커 전송됨
            // 여기서는 기존 로직처럼 cueSec만 기다리고 끝냄(기능 유지)
        }

        executed = true;
        busy = false;
    }

    IEnumerator ReturnToCenter()
    {
        busy = true;

        // 진행 중 코루틴 정리
        if (moveCo != null) { StopCoroutine(moveCo); moveCo = null; }
        if (scaleCo != null) { StopCoroutine(scaleCo); scaleCo = null; }

        // 카메라 원위치
        if (camFollow != null)
        {
            camFollow.enabled = false;
            camFollow.ResetToStart();
        }

        yield return null;

        ResetToCenterInstant();

        executed = false;
        selected = TaskType.None;
        HideTexts();

        busy = false;
    }

    void ResetToCenterInstant()
    {
        if (blackBall != null)
            blackBall.position = new Vector3(0, height, 0);

        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(false);
            targetBall.position = new Vector3(0, height, 0);
            targetBall.localScale = Vector3.one * normalScale;
        }
    }

    void PlaceTarget(TaskType task, int runId)
    {
        if (targetBall == null) return;

        Vector3 center = new Vector3(0, height, 0);
        targetBall.gameObject.SetActive(true);

        // 방향: 축 절편에 정확히
        if (task == TaskType.Right) { targetBall.position = center + new Vector3(+distance, 0, 0); targetBall.localScale = Vector3.one * normalScale; return; }
        if (task == TaskType.Left) { targetBall.position = center + new Vector3(-distance, 0, 0); targetBall.localScale = Vector3.one * normalScale; return; }
        if (task == TaskType.Up) { targetBall.position = center + new Vector3(0, 0, +distance); targetBall.localScale = Vector3.one * normalScale; return; }
        if (task == TaskType.Down) { targetBall.position = center + new Vector3(0, 0, -distance); targetBall.localScale = Vector3.one * normalScale; return; }

        // 줌: 중앙(살짝 위)에서 크기 변화
        targetBall.position = new Vector3(0, height + zoomCenterYOffset, 0);

        float to = (task == TaskType.ZoomIn) ? zoomBigScale : zoomSmallScale;

        // scale 코루틴 시작(끝에서만 마커 전송)
        if (scaleCo != null) StopCoroutine(scaleCo);
        scaleCo = StartCoroutine(AnimateScale(task, normalScale, to, cueSec, runId));
    }

    IEnumerator AnimateScale(TaskType task, float from, float to, float duration, int runId)
    {
        if (targetBall == null) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
            float s = Mathf.Lerp(from, to, a);
            targetBall.localScale = Vector3.one * s;
            yield return null;
        }
        targetBall.localScale = Vector3.one * to;

        // ✅ 줌 “완료 순간”은 여기 단 한 곳에서만
        OnTaskCompleted(task, runId);
    }

    IEnumerator MoveBlackTowardTarget(TaskType task, float seconds, int runId)
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

        // ✅ 방향 “완료 순간”은 여기 단 한 곳에서만
        OnTaskCompleted(task, runId);
    }

    void SnapBlackToStopPoint()
    {
        if (blackBall == null || targetBall == null) return;

        Vector3 start = blackBall.position;
        Vector3 tgt = targetBall.position;

        start.y = height;
        tgt.y = height;

        Vector3 dir = tgt - start;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        float dist = dir.magnitude;
        Vector3 end = start + dir.normalized * Mathf.Max(0f, dist - stopDist);
        end.y = height;

        blackBall.position = end;
    }

    // ✅ 완료 콜백: runId가 현재 실행과 맞을 때만, 한 번만 LSL 전송
    void OnTaskCompleted(TaskType task, int runId)
    {
        if (runId != currentRunId) return;  // 이전 실행에서 늦게 끝난 코루틴 무시
        SendLSLStopMarkerOnce(task);
    }

    void SendLSLStopMarkerOnce(TaskType task)
    {
        if (stopMarkerSent) return;   // ⭐ 중복 방지
        stopMarkerSent = true;

        if (!enableLSL || outlet == null) return;

        string marker = $"STOP_{task}";
        outlet.push_sample(new string[] { marker });

        if (logLSLToConsole)
        {
            double ts = LSL.LSL.local_clock();
            Debug.Log($"[LSL] Sent marker: {marker} | local_clock={ts}");
        }
    }

    void ShowSelection()
    {
        ShowCue(selected);
    }

    void ShowCue(TaskType task)
    {
        bool isDir = (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down);

        if (cueText != null)
        {
            cueText.text = isDir ? task switch
            {
                TaskType.Left => "←",
                TaskType.Right => "→",
                TaskType.Up => "↑",
                TaskType.Down => "↓",
                _ => ""
            } : "";
        }

        if (zoomText != null)
        {
            zoomText.enableAutoSizing = false;
            zoomText.fontSize = 36;
            zoomText.text = !isDir ? task switch
            {
                TaskType.ZoomIn => "ZOOM +",
                TaskType.ZoomOut => "ZOOM -",
                _ => ""
            } : "";
        }
    }

    void HideTexts()
    {
        if (cueText != null) cueText.text = "";
        if (zoomText != null) zoomText.text = "";
    }

    TaskType GetRandomTask()
    {
        TaskType[] pool = new TaskType[]
        {
            TaskType.Left,
            TaskType.Right,
            TaskType.Up,
            TaskType.Down,
            TaskType.ZoomIn,
            TaskType.ZoomOut
        };

        int idx = Random.Range(0, pool.Length);
        return pool[idx];
    }
}