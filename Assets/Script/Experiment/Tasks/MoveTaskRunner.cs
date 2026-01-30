using System;
using System.Collections;
using UnityEngine;

public class MoveTaskRunner : MonoBehaviour
{
    public Transform blackBall;
    public Transform targetBall;

    [Header("Layout")]
    public float height = 0.5f;
    public float distance = 1.6f;
    public float stopDist = 0.25f;
    public float normalScale = 0.50f;

    Coroutine co;
    
    // 게임 시작 시 저장되는 초기 위치
    Vector3 blackBallStartPos;
    Vector3 targetBallStartPos;
    bool startPosInitialized = false;

    void Start()
    {
        // 게임 시작 시 초기 위치 저장
        if (blackBall != null)
        {
            blackBallStartPos = blackBall.position;
            Debug.Log($"[MoveTaskRunner] BlackBall 시작 위치 저장: {blackBallStartPos}");
        }
        if (targetBall != null)
        {
            targetBallStartPos = targetBall.position;
        }
        startPosInitialized = true;
    }

    public void Stop()
    {
        if (co != null) { StopCoroutine(co); co = null; }
    }

    public void PrepareForMove()
    {
        // Move 시작 시 "줌 잔상"이랑 상관없이 공/타겟 상태만 정리
        ResetBlackBallToOrigin();

        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(false);
            targetBall.localScale = Vector3.one * normalScale;
        }
    }

    /// <summary>
    /// 검은 공을 게임 시작 시의 정확한 위치로 복귀
    /// </summary>
    public void ResetBlackBallToOrigin()
    {
        if (blackBall != null)
        {
            // 저장된 시작 위치로 복귀 (없으면 기본값 사용)
            Vector3 targetPos = startPosInitialized ? blackBallStartPos : new Vector3(0f, height, 0f);
            blackBall.position = targetPos;
            
            // Rigidbody가 있으면 물리 효과도 초기화
            Rigidbody rb = blackBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            Debug.Log($"[MoveTaskRunner] BlackBall 시작점으로 복귀: {blackBall.position}");
        }
        
        // 빨간공(타겟) 숨기고 크기 초기화
        HideTargetBall();
    }

    /// <summary>
    /// 빨간공(타겟)을 숨기고 크기를 원래대로 초기화
    /// </summary>
    public void HideTargetBall()
    {
        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(false);
            targetBall.localScale = Vector3.one * normalScale;
            Debug.Log("[MoveTaskRunner] TargetBall 숨김 및 크기 초기화");
        }
    }

    public void PlaceMoveTarget(StimulusController.MoveTask move)
    {
        if (targetBall == null) return;

        Vector3 center = new Vector3(0, height, 0);
        targetBall.gameObject.SetActive(true);
        targetBall.localScale = Vector3.one * normalScale;

        switch (move)
        {
            case StimulusController.MoveTask.Right: targetBall.position = center + new Vector3(+distance, 0, 0); break;
            case StimulusController.MoveTask.Left:  targetBall.position = center + new Vector3(-distance, 0, 0); break;
            case StimulusController.MoveTask.Up:    targetBall.position = center + new Vector3(0, 0, +distance); break;
            case StimulusController.MoveTask.Down:  targetBall.position = center + new Vector3(0, 0, -distance); break;
        }
    }

    public void Run(StimulusController.MoveTask move, float moveDuration, int runId, Func<int, bool> isRunValid, Action onDone)
    {
        Stop();
        co = StartCoroutine(CoMove(moveDuration, runId, isRunValid, onDone));
    }

    IEnumerator CoMove(float seconds, int runId, Func<int, bool> isRunValid, Action onDone)
    {
        if (blackBall == null || targetBall == null) yield break;

        Vector3 start = blackBall.position;
        Vector3 tgt   = targetBall.position;

        start.y = height;
        tgt.y   = height;

        Vector3 dir = tgt - start; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) yield break;

        float dist = dir.magnitude;
        Vector3 end = start + dir.normalized * Mathf.Max(0f, dist - stopDist);
        end.y = height;

        float t = 0f;
        while (t < seconds)
        {
            if (!isRunValid(runId)) yield break;

            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / Mathf.Max(0.001f, seconds));
            blackBall.position = Vector3.Lerp(start, end, a);
            yield return null;
        }

        blackBall.position = end;
        onDone?.Invoke();
    }
}
