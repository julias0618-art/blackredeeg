// XROriginLocker.cs - XR Origin 위치 고정 및 카메라 제어
// ★ 간단 모드: enableScript = false → 모든 잠금 해제, 자유로운 VR 사용!

using UnityEngine;

public class XROriginLocker : MonoBehaviour
{
    [Header("=== ★ 마스터 스위치 ★ ===")]
    [Tooltip("★ FALSE로 하면 모든 잠금 해제! 자유롭게 고개 돌리기 가능")]
    public bool enableScript = false;  // ★ 기본값 OFF - 자유 모드!
    
    [Header("=== XR Origin 위치/회전 고정 ===")]
    [Tooltip("XR Origin 위치 고정 (방 이동 방지)")]
    public bool lockOriginPosition = true;
    
    [Tooltip("XR Origin 회전 고정")]
    public bool lockOriginRotation = false;
    
    [Header("=== 카메라(머리) 트래킹 ===")]
    [Tooltip("★ TRUE: 화면 완전 고정 (머리 움직여도 안바뀜)\n★ FALSE: 고개 돌리면 따라감")]
    public bool lockCameraTracking = false;
    
    [Tooltip("고정 시 카메라 위치 (lockCameraTracking=true일 때)")]
    public Vector3 fixedCameraLocalPos = new Vector3(0, 1.6f, 0);
    
    [Tooltip("고정 시 카메라 회전 (lockCameraTracking=true일 때)")]
    public Vector3 fixedCameraLocalRot = Vector3.zero;
    
    [Header("=== XR Origin 고정 값 ===")]
    public Vector3 fixedOriginPosition = Vector3.zero;
    public Vector3 fixedOriginRotation = Vector3.zero;
    
    [Header("=== 유틸리티 ===")]
    [Tooltip("현재 Transform을 Fixed Values로 복사")]
    public bool captureCurrentValues = false;
    
    [Tooltip("Main Camera (자동 검색)")]
    public Camera targetCamera;

    private Transform _cameraTransform;
    private bool _initialized = false;

    void Start()
    {
        // 카메라 찾기
        if (targetCamera == null)
            targetCamera = Camera.main;
        
        if (targetCamera != null)
            _cameraTransform = targetCamera.transform;
        
        _initialized = true;
        
        if (!enableScript)
        {
            Debug.Log("[XROriginLocker] ★ 비활성화 상태 - 자유로운 VR 머리 트래킹!");
        }
        else
        {
            Debug.Log($"[XROriginLocker] 활성화 - LockCamera:{lockCameraTracking} LockOriginPos:{lockOriginPosition}");
        }
    }
    
    void OnValidate()
    {
        if (captureCurrentValues)
        {
            captureCurrentValues = false;
            fixedOriginPosition = transform.position;
            fixedOriginRotation = transform.eulerAngles;
            Debug.Log($"[XROriginLocker] Captured: Pos={fixedOriginPosition} Rot={fixedOriginRotation}");
        }
    }

    void LateUpdate()
    {
        // ★★★ 마스터 스위치 OFF면 아무것도 안함! ★★★
        if (!enableScript || !_initialized)
            return;

        // XR Origin 위치 고정
        if (lockOriginPosition)
        {
            transform.position = fixedOriginPosition;
        }
        
        // XR Origin 회전 고정
        if (lockOriginRotation)
        {
            transform.rotation = Quaternion.Euler(fixedOriginRotation);
        }
        
        // 카메라(머리) 트래킹 고정
        if (lockCameraTracking && _cameraTransform != null)
        {
            _cameraTransform.localPosition = fixedCameraLocalPos;
            _cameraTransform.localRotation = Quaternion.Euler(fixedCameraLocalRot);
        }
    }
    
    // 외부에서 호출 가능
    public void EnableLocking() { enableScript = true; }
    public void DisableLocking() { enableScript = false; }
}
