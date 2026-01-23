using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

    // ===== State =====
    TaskType selected = TaskType.None;  // 방향키 누르기 전에는 None
    bool executed = false;              // false=센터대기, true=실행 후 멈춤(복귀 대기)
    bool busy = false;
    Coroutine running;

    void Awake()
    {
        // ✅ 혹시 다른 스크립트가 코루틴 돌려도 이 오브젝트에서 도는 건 전부 중단
        StopAllCoroutines();
    }

    void Start()
    {
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
                // ⭐ 아무 것도 선택 안 했으면 → 랜덤으로 하나 선택
                if (selected == TaskType.None)
                {
                    selected = GetRandomTask();
                    ShowSelection();   // 화살표 / ZOOM 글씨 보여주기
                }

                if (running != null) StopCoroutine(running);
                running = StartCoroutine(ExecuteOnce(selected));
            }
            else
            {
                // 실행 끝난 상태 → 초기 화면으로 복귀
                if (running != null) StopCoroutine(running);
                running = StartCoroutine(ReturnToCenter());
            }
        }
    }

    bool Pressed(KeyControl k) => k != null && k.wasPressedThisFrame;

    IEnumerator ExecuteOnce(TaskType task)
    {
        busy = true;

        bool isDir = (task == TaskType.Left || task == TaskType.Right || task == TaskType.Up || task == TaskType.Down);

        // 카메라 follow는 방향일 때만 ON
        if (camFollow != null)
            camFollow.enabled = isDir;

        // 1) 타겟 배치
        PlaceTarget(task);

        // 2) 텍스트(선택된 cue) 보여주기
        ShowCue(task);

        // 3) 방향이면 검은공 이동
        Coroutine moveCo = null;
        if (isDir)
            moveCo = StartCoroutine(MoveBlackTowardTarget(moveDuration));

        // 4) cueSec 후 텍스트 숨기기(공은 유지)
        yield return new WaitForSeconds(cueSec);
        HideTexts();

        // 5) 이동 끝까지 기다리기 (moveDuration 끝날 때까지)
        if (isDir)
            yield return new WaitForSeconds(Mathf.Max(0f, moveDuration - cueSec));

        if (moveCo != null) StopCoroutine(moveCo);

        // ✅ 여기서 "멈춤": 아무것도 자동으로 안 함
        executed = true;
        busy = false;
    }

    IEnumerator ReturnToCenter()
    {
        busy = true;

        // 카메라 원위치
        if (camFollow != null)
        {
            camFollow.enabled = false;
            camFollow.ResetToStart();
        }

        // 한 프레임 기다려서 카메라 리셋 반영
        yield return null;

        ResetToCenterInstant();

        // 복귀 후에는 다시 선택해야 실행 가능
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

    void PlaceTarget(TaskType task)
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
        StopCoroutineSafe(nameof(AnimateScale));
        StartCoroutine(AnimateScale(normalScale, to, cueSec));
    }

    IEnumerator AnimateScale(float from, float to, float duration)
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
    }

    IEnumerator MoveBlackTowardTarget(float seconds)
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

    void ShowSelection()
    {
        // 선택만 했을 때 미리 보여주고 싶으면 유지
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

    void StopCoroutineSafe(string methodName)
    {
        // 코루틴이 없으면 예외 안 나게 보호
        try { StopCoroutine(methodName); } catch { }
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