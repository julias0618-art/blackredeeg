using System;
using System.Collections;
using UnityEngine;

public class ZoomTaskRunner : MonoBehaviour
{
    public Camera mainCamera;
    public CameraFollow camFollow;
    public Transform targetBall;
    public bool isXR = false;

    [Header("TargetBall Scale Zoom (optional)")]
    public bool zoomByTargetScale = true; // 기본 줌은 FOV 대신 타겟 스케일로 처리
    public float normalScale = 0.50f;
    public float zoomBigScale = 2.00f;
    public float zoomSmallScale = 0.15f;
    public float zoomCenterYOffset = 0.15f;

    [Header("Camera Dolly (optional)")]
    public bool zoomByCameraDolly = false;
    public float zoomInDolly = 0.8f;
    public float zoomOutDolly = 0.5f;

    [Header("Camera FOV (recommended)")]
    public bool zoomByFOV = false; // FOV 변경은 과도한 줌/복구 문제 방지용으로 기본 비활성화
    public float zoomInFOV = 35f;
    public float zoomOutFOV = 80f;

    public AnimationCurve zoomCurve;

    Coroutine co;

    Vector3 camStartPos;
    Quaternion camStartRot;
    float camStartFOV;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            camStartPos = mainCamera.transform.position;
            camStartRot = mainCamera.transform.rotation;
            camStartFOV = mainCamera.fieldOfView;
        }
    }

    public void Stop()
    {
        if (co != null) { StopCoroutine(co); co = null; }
    }

    public void PrepareForZoom()
    {
        // ✅ Zoom 시작 전에 “카메라 기본값”에서 출발
        if (!isXR && mainCamera != null)
        {
            mainCamera.transform.position = camStartPos;
            mainCamera.transform.rotation = camStartRot;
            mainCamera.fieldOfView = camStartFOV;
        }

        // ✅ Follow 완전 차단
        if (camFollow != null)
        {
            camFollow.locked = true;
            camFollow.enabled = false;
            camFollow.ResetToStart();
        }

        if (targetBall != null)
        {
            targetBall.gameObject.SetActive(true);
            targetBall.position = new Vector3(0, targetBall.position.y + zoomCenterYOffset, 0);
            targetBall.localScale = Vector3.one * normalScale;
        }
    }

    public void RestoreCameraBaseline(bool restoreFov)
    {
        Stop();

        if (camFollow != null)
        {
            camFollow.locked = false;
            camFollow.enabled = false;
            camFollow.ResetToStart();
        }

        if (!isXR && mainCamera != null)
        {
            mainCamera.transform.position = camStartPos;
            mainCamera.transform.rotation = camStartRot;
            if (restoreFov) mainCamera.fieldOfView = camStartFOV;
        }
    }

    public void Run(StimulusController.ZoomTask z, float duration, int runId, Func<int, bool> isRunValid, Action onDone)
    {
        Stop();
        co = StartCoroutine(CoZoom(z, duration, runId, isRunValid, onDone));
    }

    IEnumerator CoZoom(StimulusController.ZoomTask z, float duration, int runId, Func<int, bool> isRunValid, Action onDone)
    {
        if (!isXR && zoomByFOV && mainCamera != null)
        {
            float from = mainCamera.fieldOfView;
            float to = (z == StimulusController.ZoomTask.ZoomIn) ? zoomInFOV : zoomOutFOV;

            float t = 0f;
            while (t < duration)
            {
                if (!isRunValid(runId)) yield break;

                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
                if (zoomCurve != null && zoomCurve.length > 0) a = zoomCurve.Evaluate(a);

                mainCamera.fieldOfView = Mathf.Lerp(from, to, a);
                yield return null;
            }

            mainCamera.fieldOfView = to;
            onDone?.Invoke();
            yield break;
        }

        if (!isXR && zoomByCameraDolly && mainCamera != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;

            float dolly = (z == StimulusController.ZoomTask.ZoomIn) ? zoomInDolly : -zoomOutDolly;
            Vector3 endPos = startPos + mainCamera.transform.forward * dolly;

            float t = 0f;
            while (t < duration)
            {
                if (!isRunValid(runId)) yield break;

                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
                if (zoomCurve != null && zoomCurve.length > 0) a = zoomCurve.Evaluate(a);

                mainCamera.transform.position = Vector3.Lerp(startPos, endPos, a);
                mainCamera.transform.rotation = startRot;
                yield return null;
            }

            mainCamera.transform.position = endPos;
            mainCamera.transform.rotation = startRot;
            onDone?.Invoke();
            yield break;
        }

        if (zoomByTargetScale && targetBall != null)
        {
            float from = normalScale;
            float to = (z == StimulusController.ZoomTask.ZoomIn) ? zoomBigScale : zoomSmallScale;

            float t = 0f;
            while (t < duration)
            {
                if (!isRunValid(runId)) yield break;

                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / Mathf.Max(0.001f, duration));
                if (zoomCurve != null && zoomCurve.length > 0) a = zoomCurve.Evaluate(a);

                float s = Mathf.Lerp(from, to, a);
                targetBall.localScale = Vector3.one * s;
                yield return null;
            }

            targetBall.localScale = Vector3.one * to;
            onDone?.Invoke();
            yield break;
        }

        // 아무 방식도 없으면 duration 후 종료
        float tt = 0f;
        while (tt < duration)
        {
            if (!isRunValid(runId)) yield break;
            tt += Time.deltaTime;
            yield return null;
        }
        onDone?.Invoke();
    }
}
