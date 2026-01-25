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

    public void Stop()
    {
        if (co != null) { StopCoroutine(co); co = null; }
    }

    public void PrepareForMove()
    {
        // Move 시작 시 “줌 잔상”이랑 상관없이 공/타겟 상태만 정리
        if (blackBall != null) blackBall.position = new Vector3(0, height, 0);

        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(false);
            targetBall.localScale = Vector3.one * normalScale;
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
