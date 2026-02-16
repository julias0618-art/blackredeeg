using UnityEngine;

/// <summary>
/// 카메라가 검은공(타겟)을 항상 따라가서
/// 검은공이 화면 중심에 있도록 함
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("★ 검은공을 여기에 연결! ★")]
    public Transform target;

    [Header("Camera Position (타겟 기준)")]
    [Tooltip("카메라 높이 (타겟 위로)")]
    public float height = 6f;
    
    [Tooltip("카메라 Z 오프셋 (0 = 타겟 바로 위, 음수 = 뒤로)")]
    public float zOffset = 0f;

    [Header("Camera Rotation")]
    [Tooltip("카메라 X축 회전 (90 = 바로 아래를 봄, 30 = 앞을 봄)")]
    public float angle = 90f;

    [Header("Control")]
    [Tooltip("체크하면 카메라 움직임 일시정지")]
    public bool paused = false;

    [Header("Debug")]
    public bool showDebugLog = true;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("[CameraFollow] ★★★ TARGET이 연결되지 않음! Inspector에서 검은공을 연결하세요! ★★★");
        }
        else
        {
            Debug.Log($"[CameraFollow] 타겟 연결됨: {target.name}");
            // 즉시 카메라 위치 설정
            ForceUpdatePosition();
        }
    }

    void LateUpdate()
    {
        if (paused) return;
        if (target == null) return;

        ForceUpdatePosition();
    }

    void ForceUpdatePosition()
    {
        // 타겟(검은공) 위치
        Vector3 targetPos = target.position;

        // 카메라 위치 = 타겟 위치 + 오프셋
        // 탑다운 뷰: X는 타겟과 같음, Y는 위로 height, Z는 zOffset
        Vector3 newPos = new Vector3(
            targetPos.x,            // X: 타겟과 같은 X
            targetPos.y + height,   // Y: 타겟 위로 height만큼
            targetPos.z + zOffset   // Z: 타겟 위치 + zOffset
        );

        // 카메라 회전 (90 = 바로 아래를 바라봄)
        Quaternion newRot = Quaternion.Euler(angle, 0f, 0f);

        // 적용
        transform.position = newPos;
        transform.rotation = newRot;

        
    }

    /// <summary>
    /// 외부에서 호출 가능 - 즉시 위치 업데이트
    /// </summary>
    public void ResetToStart()
    {
        if (target != null)
        {
            ForceUpdatePosition();
        }
    }
}
