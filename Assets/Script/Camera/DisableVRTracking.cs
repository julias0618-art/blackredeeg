using UnityEngine;

/// <summary>
/// VR 헤드 트래킹을 끄는 스크립트
/// Main Camera에 추가하세요
/// </summary>
public class DisableVRTracking : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("VR 트래킹 비활성화")]
    public bool disableTracking = true;

    void Start()
    {
        if (disableTracking)
        {
            DisableTracking();
        }
    }

    void DisableTracking()
    {
        // 방법 1: InputSystem의 TrackedPoseDriver 끄기
        var trackedPoseDriver = GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (trackedPoseDriver != null)
        {
            trackedPoseDriver.enabled = false;
            Debug.Log("[DisableVRTracking] TrackedPoseDriver 비활성화됨");
            return;
        }

        // 방법 2: 레거시 SpatialTracking의 TrackedPoseDriver 끄기
#if UNITY_2019_1_OR_NEWER
        var legacyDriver = GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        if (legacyDriver != null)
        {
            legacyDriver.enabled = false;
            Debug.Log("[DisableVRTracking] Legacy TrackedPoseDriver 비활성화됨");
            return;
        }
#endif

        Debug.Log("[DisableVRTracking] TrackedPoseDriver를 찾을 수 없음 - 수동으로 비활성화하세요");
    }

    /// <summary>
    /// 트래킹 켜기/끄기 토글
    /// </summary>
    public void SetTrackingEnabled(bool enabled)
    {
        var trackedPoseDriver = GetComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
        if (trackedPoseDriver != null)
        {
            trackedPoseDriver.enabled = enabled;
            Debug.Log($"[DisableVRTracking] TrackedPoseDriver: {enabled}");
            return;
        }

#if UNITY_2019_1_OR_NEWER
        var legacyDriver = GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        if (legacyDriver != null)
        {
            legacyDriver.enabled = enabled;
            Debug.Log($"[DisableVRTracking] Legacy TrackedPoseDriver: {enabled}");
        }
#endif
    }
}
