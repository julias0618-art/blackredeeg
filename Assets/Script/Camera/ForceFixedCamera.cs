using UnityEngine;

/// <summary>
/// VR에서 카메라를 강제로 고정 - 월드 좌표 사용
/// 부모가 움직여도 상관없이 절대 위치로 고정
/// 
/// ★ Main Camera에 추가하세요 ★
/// </summary>
public class ForceFixedCamera : MonoBehaviour
{
    [Header("Fixed World Transform")]
    [Tooltip("월드 좌표 기준 고정 위치")]
    public Vector3 fixedWorldPosition = new Vector3(0f, 2f, -5f);
    
    [Tooltip("월드 좌표 기준 고정 회전")]
    public Vector3 fixedWorldRotation = new Vector3(30f, 0f, 0f);

    [Header("Control")]
    public bool forceEnabled = true;
    
    [Header("Debug")]
    public bool showDebugLog = true;

    int frameCount = 0;

    void Start()
    {
        if (showDebugLog)
        {
            Debug.Log($"[ForceFixedCamera] 활성화됨");
            Debug.Log($"[ForceFixedCamera] 목표 위치(World): {fixedWorldPosition}");
            Debug.Log($"[ForceFixedCamera] 목표 회전(World): {fixedWorldRotation}");
        }
    }

    void Update()
    {
        ApplyFixedTransform();
    }

    void LateUpdate()
    {
        ApplyFixedTransform();
    }

    void OnPreRender()
    {
        ApplyFixedTransform();
    }

    void ApplyFixedTransform()
    {
        if (!forceEnabled) return;

        // 월드 좌표로 강제 설정 (부모가 뭘 하든 상관없음)
        transform.position = fixedWorldPosition;
        transform.rotation = Quaternion.Euler(fixedWorldRotation);

        // 디버그 (10초마다 한번)
        if (showDebugLog && frameCount % 600 == 0)
        {
            Debug.Log($"[ForceFixedCamera] 현재 위치: {transform.position}, 부모: {(transform.parent != null ? transform.parent.name : "없음")}");
        }
        frameCount++;
    }

    /// <summary>
    /// Play 모드에서 올바른 화면일 때 호출하여 현재 위치 저장
    /// Inspector에서 컴포넌트 우클릭 → "Save Current World Transform"
    /// </summary>
    [ContextMenu("Save Current World Transform")]
    public void SaveCurrentWorldTransform()
    {
        fixedWorldPosition = transform.position;
        fixedWorldRotation = transform.eulerAngles;
        Debug.Log($"[ForceFixedCamera] 월드 좌표 저장됨 - 위치: {fixedWorldPosition}, 회전: {fixedWorldRotation}");
    }
}
