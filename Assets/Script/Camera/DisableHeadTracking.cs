using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// VR 헤드 트래킹을 강제로 비활성화
/// 아무 활성화된 오브젝트에 추가하면 됨 (Main Camera 권장)
/// </summary>
public class DisableHeadTracking : MonoBehaviour
{
    void Start()
    {
        DisableTracking();
    }

    void DisableTracking()
    {
        // 위치 트래킹 비활성화
        InputTracking.disablePositionalTracking = true;
        Debug.Log("[DisableHeadTracking] 위치 트래킹 비활성화됨");

        // XR 자동 카메라 트래킹 비활성화
        if (Camera.main != null)
        {
            XRDevice.DisableAutoXRCameraTracking(Camera.main, true);
            Debug.Log("[DisableHeadTracking] XR 카메라 트래킹 비활성화됨");
        }
        else
        {
            Debug.LogWarning("[DisableHeadTracking] Main Camera를 찾을 수 없음!");
        }
    }
}
