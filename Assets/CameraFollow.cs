using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;   // 검은공

    [Header("Follow Axes (Top-Down)")]
    public bool followX = true;
    public bool followY = false;  // 탑다운이면 보통 Y는 고정
    public bool followZ = true;

    [Header("Smoothing")]
    public bool smooth = true;
    public float smoothTime = 0.05f;

    Vector3 offset;
    Vector3 velocity;
    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        Vector3 current = transform.position;

        if (!followX) desired.x = current.x;
        if (!followY) desired.y = current.y;
        if (!followZ) desired.z = current.z;

       
        transform.position = desired;
    }

    public void ResetToStart()
    {
        transform.position = startPos;
        velocity = Vector3.zero;

        if (target != null)
            offset = transform.position - target.position;
    }
    public void SnapNow()
    {
        if (target == null) return;

        // Start에서 계산했던 offset 사용
        Vector3 desired = target.position + offset;

        Vector3 current = transform.position;
        if (!followX) desired.x = current.x;
        if (!followY) desired.y = current.y;
        if (!followZ) desired.z = current.z;

        transform.position = desired;
        velocity = Vector3.zero;
    }

}
