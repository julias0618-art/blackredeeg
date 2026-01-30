using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;   // 검은공

    [Header("Follow Axes (Top-Down)")]
    public bool followX = true;
    public bool followY = false;
    public bool followZ = true;

    [Header("Lock (External Control)")]
    public bool locked = false; // ✅ Zoom 중 true면 LateUpdate에서 절대 카메라 안 건드림

    Vector3 offset;
    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (locked) return;            // ✅ 핵심
        if (!enabled) return;
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

        if (target != null)
            offset = transform.position - target.position;
    }
}
