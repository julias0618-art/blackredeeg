using UnityEngine;

/// <summary>
/// VR에서 Canvas가 항상 카메라 앞에 보이도록 따라다니게 하는 스크립트
/// Canvas에 추가하고 World Space로 설정하세요
/// </summary>
public class VRCanvasFollower : MonoBehaviour
{
    [Header("Target Camera")]
    [Tooltip("따라다닐 카메라 (비워두면 Main Camera 자동 사용)")]
    public Camera targetCamera;

    [Header("Position Settings")]
    [Tooltip("카메라로부터의 거리 (앞쪽)")]
    public float distanceFromCamera = 2f;
    
    [Tooltip("카메라 기준 높이 오프셋")]
    public float heightOffset = 0f;

    [Header("Behavior")]
    [Tooltip("카메라를 바라보도록 회전")]
    public bool faceCamera = true;
    
    [Tooltip("부드럽게 따라가기 (0이면 즉시)")]
    public float smoothSpeed = 0f;

    Canvas canvas;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        
        // Canvas를 World Space로 자동 설정
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            Debug.Log("[VRCanvasFollower] Canvas를 World Space로 변경함");
        }

        // 카메라 자동 설정
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("[VRCanvasFollower] 카메라를 찾을 수 없습니다!");
        }
        else
        {
            Debug.Log($"[VRCanvasFollower] 타겟 카메라: {targetCamera.name}");
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // 목표 위치 계산 (카메라 앞)
        Vector3 targetPos = targetCamera.transform.position 
                          + targetCamera.transform.forward * distanceFromCamera
                          + Vector3.up * heightOffset;

        // 위치 이동
        if (smoothSpeed > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPos;
        }

        // 카메라를 바라보도록 회전
        if (faceCamera)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position);
        }
    }
}
