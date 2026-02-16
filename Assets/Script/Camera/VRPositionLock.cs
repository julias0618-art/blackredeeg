using UnityEngine;

/// <summary>
/// 게임 시작 시 카메라의 위치/회전을 저장하고,
/// VR 헤드셋이 움직여도 그 시점을 유지하는 스크립트
/// 
/// ★ Main Camera에 추가하세요 ★
/// </summary>
public class VRPositionLock : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("VR 추적 무시하고 시점 고정")]
    public bool lockEnabled = true;

    // 게임 시작 시 저장되는 초기 Transform
    Vector3 savedPosition;
    Quaternion savedRotation;
    bool initialized = false;

    void Start()
    {
        // 게임 시작 시 현재 카메라 위치/회전 저장
        savedPosition = transform.localPosition;
        savedRotation = transform.localRotation;
        initialized = true;
        
        Debug.Log($"[VRPositionLock] 초기 시점 저장 완료 - Pos: {savedPosition}, Rot: {savedRotation.eulerAngles}");
    }

    void LateUpdate()
    {
        if (!lockEnabled || !initialized) return;

        // VR이 변경한 위치/회전을 원래대로 복원
        transform.localPosition = savedPosition;
        transform.localRotation = savedRotation;
    }

    /// <summary>
    /// 현재 시점을 새로운 고정 시점으로 저장
    /// </summary>
    public void SaveCurrentView()
    {
        savedPosition = transform.localPosition;
        savedRotation = transform.localRotation;
        Debug.Log($"[VRPositionLock] 새 시점 저장 - Pos: {savedPosition}, Rot: {savedRotation.eulerAngles}");
    }
}
