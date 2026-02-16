using UnityEngine;

/// <summary>
/// VR에서 XR Origin이 타겟(검은공)을 따라가도록 하는 스크립트
/// XR Origin 오브젝트에 추가하세요
/// </summary>
public class XROriginFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("따라갈 대상 (검은공)")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("X축 따라가기")]
    public bool followX = true;
    [Tooltip("Y축 따라가기 (보통 false)")]
    public bool followY = false;
    [Tooltip("Z축 따라가기")]
    public bool followZ = true;

    [Header("Offset")]
    [Tooltip("타겟으로부터의 오프셋")]
    public Vector3 offset = Vector3.zero;

    [Header("Control")]
    [Tooltip("따라가기 일시정지")]
    public bool locked = false;

    Vector3 initialOffset;
    bool initialized = false;

    void Start()
    {
        if (target != null)
        {
            // 시작 시 현재 오프셋 저장
            initialOffset = transform.position - target.position;
            initialized = true;
            Debug.Log($"[XROriginFollow] 초기화 완료. 타겟: {target.name}, 오프셋: {initialOffset}");
        }
        else
        {
            Debug.LogWarning("[XROriginFollow] 타겟이 설정되지 않았습니다!");
        }
    }

    void LateUpdate()
    {
        if (locked) return;
        if (target == null) return;
        if (!initialized) return;

        // 목표 위치 계산
        Vector3 targetPos = target.position + initialOffset + offset;
        Vector3 currentPos = transform.position;

        // 선택된 축만 따라가기
        Vector3 newPos = currentPos;
        if (followX) newPos.x = targetPos.x;
        if (followY) newPos.y = targetPos.y;
        if (followZ) newPos.z = targetPos.z;

        transform.position = newPos;
    }

    /// <summary>
    /// 현재 위치를 기준으로 오프셋 재설정
    /// </summary>
    public void ResetOffset()
    {
        if (target != null)
        {
            initialOffset = transform.position - target.position;
            Debug.Log($"[XROriginFollow] 오프셋 재설정: {initialOffset}");
        }
    }

    /// <summary>
    /// 따라가기 잠금/해제
    /// </summary>
    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        Debug.Log($"[XROriginFollow] Locked: {locked}");
    }
}
