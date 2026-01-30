using UnityEngine;

public class BillboardNoRoll : MonoBehaviour
{
    public Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // 핵심:
        // -cam.forward 를 바라보게 하면 "항상 카메라를 정면으로" 향함
        // cam.up 을 up으로 쓰면 "화면에서 항상 똑바로(roll 없음)" 유지됨
        transform.rotation = Quaternion.LookRotation(-cam.transform.forward, cam.transform.up);
    }
}
