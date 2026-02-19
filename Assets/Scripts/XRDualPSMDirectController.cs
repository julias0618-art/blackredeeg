using UnityEngine;
using UnityEngine.XR;

public class XRDualPSMDirectController : MonoBehaviour
{
    [System.Serializable]
    public class PSMArm
    {
        public string name = "PSM";

        [Header("Joint Transforms")]
    [Tooltip("Main insertion link (Prismatic - depth control)")]
    public Transform insertionLink;
    
    [Tooltip("Yaw link (Revolute - left/right)")]
    public Transform yawLink;
    
    [Tooltip("Pitch link (Revolute - up/down)")]
    public Transform pitchLink;
    
    [Tooltip("Tool roll link (Revolute - rotation)")]
    public Transform rollLink;
    
    [Header("Link Synchronization")]
    [Tooltip("FORCE SYNC: Roll link ALWAYS follows insertion link movement")]
    public bool forceRollSync = true;
    
    [Tooltip("Manual world offset (roll.position - insertion.position)")]
    public Vector3 manualWorldOffset = Vector3.zero;
    
    [HideInInspector] public bool offsetInitialized = false;
    [HideInInspector] public Vector3 rollInsertionOffset = Vector3.zero;
    [HideInInspector] public Vector3 worldOffsetRollToInsertion = Vector3.zero;
    
    [Header("Per-Arm Axis Overrides")]
    [Tooltip("Use custom axes for this arm (for mirrored robots)")]
    public bool useCustomAxes = false;
    public Vector3 customPitchAxis = Vector3.right;
    public Vector3 customYawAxis = Vector3.up;
    public Vector3 customRollAxis = Vector3.right;
    
    [Header("Per-Arm Invert")]
    public bool invertYaw = false;
    public bool invertPitch = false;
    public bool invertInsertion = false;
    public bool invertRoll = false;
    
    [Header("Gripper")]
    public Transform gripperLeft;
    public Transform gripperRight;
    
    [Tooltip("Point between gripper jaws where objects are grabbed")]
    public Transform grabPoint;
    
    [Tooltip("Detection radius for grabbable objects")]
    public float grabRadius = 0.02f;
    
    [Tooltip("Enable grab functionality")]
    public bool enableGrab = false;
    
    [Tooltip("Only grab objects containing this name")]
    public string grabTargetName = "needle";
    
    // Grab state
    [HideInInspector] public GameObject grabbedObject;
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public bool isGrabbing;
    [HideInInspector] public float triggerValue;
    
    // Runtime state
    [HideInInspector] public float insertionValue;
    [HideInInspector] public float yawValue;
    [HideInInspector] public float pitchValue;
    [HideInInspector] public float rollValue;
    [HideInInspector] public float lateralValue;
    
    [HideInInspector] public Vector3 insertionInitialPos;
    [HideInInspector] public Quaternion insertionInitialRot;
    [HideInInspector] public Vector3 rollInitialPos;
    [HideInInspector] public Quaternion yawInitialRot;
    [HideInInspector] public Quaternion pitchInitialRot;
    [HideInInspector] public Quaternion rollInitialRot;
    
    [HideInInspector] public Vector3 prevControllerPos;
    [HideInInspector] public Quaternion prevControllerRot;
    [HideInInspector] public Quaternion initialControllerRot;  // Roll 초기 회전
    [HideInInspector] public bool initialized;
    
    // ★ Roll 떨림 방지용 필터링
    [HideInInspector] public float smoothedRoll = 0f;
    
    // ★ World 축 이동 오프셋
    [HideInInspector] public float worldAxisOffset = 0f;
}

[Header("=== PSM Arms ===")]
public PSMArm rightPSM;
public PSMArm leftPSM;

[Header("=== Joint Axes ===")]
[Tooltip("Axis for yaw rotation")]
public Vector3 yawAxis = Vector3.up;

[Tooltip("Axis for pitch rotation")]
public Vector3 pitchAxis = Vector3.right;

[Tooltip("Axis for roll rotation - X axis")]
public Vector3 rollAxis = Vector3.right;

[Tooltip("Axis for gripper rotation")]
public Vector3 gripperAxis = Vector3.right;

[Header("=== Sensitivity ===")]
[Tooltip("Roll sensitivity (multiplier for controller rotation)")]
public float rollSensitivity = 0.3f;  // ★ 감도 낮춤

[Tooltip("Roll deadzone in degrees (떨림 방지)")]
public float rollDeadzone = 15f;  // ★ 데드존 크게 증가

[Tooltip("Gripper max angle (degrees)")]
public float gripperMaxAngle = 30f;

[Header("=== Limits ===")]
public float insertionMin = 0f;
public float insertionMax = 0.24f;

public float yawMin = -90f;
public float yawMax = 90f;

public float pitchMin = -60f;
public float pitchMax = 60f;

[Header("Lateral Movement (좌우 이동)")]
public float lateralMin = -0.02f;
public float lateralMax = 0.02f;

[Header("=== Smoothing ===")]
[Range(0f, 0.95f)]
public float smoothing = 0.7f;  // ★ 부드러움 증가

[Tooltip("Roll smoothing (higher = smoother, less jitter)")]
[Range(0f, 0.95f)]
public float rollSmoothing = 0.85f;  // ★ Roll 전용 부드러움

[Header("=== Control ===")]
public bool isFrozen = false;
public bool disableYawPitchControl = true;

[Header("=== Thumbstick Control ===")]
public bool useThumbstickControl = true;

public enum ThumbstickXMode
{
    Yaw,
    LateralMove,
    Disabled
}

public ThumbstickXMode thumbstickXMode = ThumbstickXMode.LateralMove;
public bool thumbstickYForInsertion = true;

[Header("--- Thumbstick Sensitivity ---")]
public float thumbstickYawSpeed = 20f;
[Tooltip("상하 이동 속도 (Local Y)")]
public float thumbstickInsertionSpeed = 0.1f;  // ★ 증가: 0.03 → 0.1
[Tooltip("좌우 이동 속도 (Local X)")]
public float thumbstickLateralSpeed = 0.1f;    // ★ 증가: 0.05 → 0.1

[Range(0f, 0.5f)]
[Tooltip("데드존 (작을수록 민감)")]
public float thumbstickDeadzone = 0.25f;       // ★ 약간 증가

[Tooltip("★ 좌우/상하 분리 모드 (한 방향만 인식)")]
public bool separateAxes = true;

public bool invertInsertion = false;
public bool invertYaw = false;

[Header("=== X/Y Button World Movement ===")]
[Tooltip("X/Y 버튼으로 World 축 이동 활성화")]
public bool enableWorldMovement = true;

public enum WorldAxis { X, Y, Z }
[Tooltip("이동할 World 축 선택")]
public WorldAxis worldMoveAxis = WorldAxis.Y;

[Tooltip("World 축 이동 속도")]
public float worldMoveSpeed = 0.05f;

[Header("=== Direction Flip ===")]
public bool flipYawDirection = false;
public bool flipPitchDirection = false;
public bool flipInsertionDirection = false;

[Header("=== Debug ===")]
public bool showDebugInfo = true;
public bool logControllerMovement = false;

// Controllers
private InputDevice _rightController;
private InputDevice _leftController;
private bool _rightFound, _leftFound;

void Start()
{
    InitializeArm(rightPSM);
    InitializeArm(leftPSM);
    TryGetControllers();
    
    EnforceLinkSync(rightPSM);
    EnforceLinkSync(leftPSM);
}

void InitializeArm(PSMArm arm)
{
    if (arm.insertionLink != null)
    {
        arm.insertionInitialPos = arm.insertionLink.localPosition;
        arm.insertionInitialRot = arm.insertionLink.localRotation;
        
        Debug.Log($"[XRDualPSM] {arm.name} Initial LOCAL Position: {arm.insertionInitialPos}");
    }
    if (arm.yawLink != null)
        arm.yawInitialRot = arm.yawLink.localRotation;
    if (arm.pitchLink != null)
        arm.pitchInitialRot = arm.pitchLink.localRotation;
    if (arm.rollLink != null)
    {
        arm.rollInitialRot = arm.rollLink.localRotation;
        arm.rollInitialPos = arm.rollLink.localPosition;
        
        if (arm.insertionLink != null && !arm.offsetInitialized)
        {
            if (arm.manualWorldOffset != Vector3.zero)
            {
                arm.worldOffsetRollToInsertion = arm.manualWorldOffset;
                arm.rollInsertionOffset = arm.manualWorldOffset;
            }
            else
            {
                arm.rollInsertionOffset = arm.rollLink.localPosition - arm.insertionLink.localPosition;
                arm.worldOffsetRollToInsertion = arm.rollLink.position - arm.insertionLink.position;
            }
            arm.offsetInitialized = true;
        }
    }
        
    arm.insertionValue = 0f;
    arm.yawValue = 0f;
    arm.pitchValue = 0f;
    arm.rollValue = 0f;
    arm.smoothedRoll = 0f;
    arm.lateralValue = 0f;
    arm.worldAxisOffset = 0f;  // World 축 오프셋 리셋
    arm.initialized = false;
}

void Update()
{
    if (!_rightFound || !_leftFound)
    {
        TryGetControllers();
    }

    if (isFrozen) return;

    if (_rightFound && _rightController.isValid)
    {
        UpdateArm(_rightController, rightPSM);
    }

    if (_leftFound && _leftController.isValid)
    {
        UpdateArm(_leftController, leftPSM);
    }

    HandleKeyboardInput();
}

void LateUpdate()
{
    if (!isFrozen)
    {
        EnforceLinkSync(rightPSM);
        EnforceLinkSync(leftPSM);
    }
}

void EnforceLinkSync(PSMArm arm)
{
    if (arm == null || arm.insertionLink == null || arm.rollLink == null) return;
    if (!arm.forceRollSync) return;
    
    bool sameParent = (arm.insertionLink.parent == arm.rollLink.parent);
    
    if (sameParent)
    {
        Vector3 targetLocalPos = arm.insertionLink.localPosition + arm.rollInsertionOffset;
        arm.rollLink.localPosition = targetLocalPos;
    }
    else
    {
        Vector3 targetWorldPos = arm.insertionLink.position + arm.worldOffsetRollToInsertion;
        arm.rollLink.position = targetWorldPos;
    }
}

void UpdateArm(InputDevice controller, PSMArm arm)
{
    Vector3 controllerPos;
    Quaternion controllerRot;
    float triggerValue;

    if (!controller.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPos) ||
        !controller.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerRot))
    {
        return;
    }

    controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);
    
    Vector2 thumbstick = Vector2.zero;
    controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out thumbstick);
    
    // 데드존 적용
    if (Mathf.Abs(thumbstick.x) < thumbstickDeadzone) thumbstick.x = 0f;
    if (Mathf.Abs(thumbstick.y) < thumbstickDeadzone) thumbstick.y = 0f;
    
    // ★★★ 좌우/상하 분리 모드: 더 큰 축만 인식 ★★★
    if (separateAxes && thumbstick.x != 0f && thumbstick.y != 0f)
    {
        if (Mathf.Abs(thumbstick.x) > Mathf.Abs(thumbstick.y))
        {
            // 좌우가 더 크면 상하 무시
            thumbstick.y = 0f;
        }
        else
        {
            // 상하가 더 크면 좌우 무시
            thumbstick.x = 0f;
        }
    }
    
    // ★★★ X/Y 버튼으로 World 축 이동 ★★★
    // Camera Mode일 때는 X/Y 버튼 무시 (카메라 줌에 사용)
    bool xButton = false;
    bool yButton = false;
    controller.TryGetFeatureValue(CommonUsages.primaryButton, out xButton);    // X (왼쪽) / A (오른쪽)
    controller.TryGetFeatureValue(CommonUsages.secondaryButton, out yButton);  // Y (왼쪽) / B (오른쪽)
    
    // ★ Camera Mode가 아닐 때만 World 이동
    if (enableWorldMovement && !QuestCameraController.IsInCameraMode)
    {
        if (yButton)  // Y/B 버튼 = + 방향
        {
            arm.worldAxisOffset += worldMoveSpeed * Time.deltaTime;
            Debug.Log($"[{arm.name}] Y Button → World {worldMoveAxis} Offset: {arm.worldAxisOffset:F4}");
        }
        if (xButton)  // X/A 버튼 = - 방향
        {
            arm.worldAxisOffset -= worldMoveSpeed * Time.deltaTime;
            Debug.Log($"[{arm.name}] X Button → World {worldMoveAxis} Offset: {arm.worldAxisOffset:F4}");
        }
    }

    if (!arm.initialized)
    {
        arm.prevControllerPos = controllerPos;
        arm.prevControllerRot = controllerRot;
        arm.initialControllerRot = controllerRot;
        arm.initialized = true;
        return;
    }

    // ★★★ Roll: 절대 회전 (초기 위치 기준) + 강력한 떨림 방지 ★★★
    Quaternion rotFromInitial = controllerRot * Quaternion.Inverse(arm.initialControllerRot);
    float absoluteRoll = NormalizeAngle(rotFromInitial.eulerAngles.z);
    
    // 1차 필터: 데드존 (큰 값)
    float rawTargetRoll = 0f;
    if (Mathf.Abs(absoluteRoll) > rollDeadzone)
    {
        rawTargetRoll = absoluteRoll * rollSensitivity;
    }
    
    // 2차 필터: 지수 평활화 (떨림 방지)
    arm.smoothedRoll = Mathf.Lerp(arm.smoothedRoll, rawTargetRoll, 1f - rollSmoothing);
    
    // 3차 필터: 매우 작은 변화 무시
    float rollDiff = Mathf.Abs(arm.smoothedRoll - arm.rollValue);
    if (rollDiff < 0.5f)
    {
        // 0.5도 미만 변화는 무시
        arm.smoothedRoll = arm.rollValue;
    }
    
    float yawDelta = 0f;
    float pitchDelta = 0f;
    float insertionDelta = 0f;
    float lateralDelta = 0f;
    
    // THUMBSTICK MODE
    if (useThumbstickControl)
    {
        switch (thumbstickXMode)
        {
            case ThumbstickXMode.Yaw:
                yawDelta = thumbstick.x * thumbstickYawSpeed * Time.deltaTime;
                if (invertYaw) yawDelta = -yawDelta;
                break;
                
            case ThumbstickXMode.LateralMove:
                lateralDelta = thumbstick.x * thumbstickLateralSpeed * Time.deltaTime;
                break;
        }
        
        if (thumbstickYForInsertion)
        {
            insertionDelta = thumbstick.y * thumbstickInsertionSpeed * Time.deltaTime;
            if (invertInsertion) insertionDelta = -insertionDelta;
        }
        
        pitchDelta = 0f;
    }

    // Apply smoothing
    insertionDelta = Mathf.Lerp(insertionDelta, 0, smoothing);
    yawDelta = Mathf.Lerp(yawDelta, 0, smoothing);
    pitchDelta = Mathf.Lerp(pitchDelta, 0, smoothing);
    lateralDelta = Mathf.Lerp(lateralDelta, 0, smoothing);

    // Update values
    arm.insertionValue = Mathf.Clamp(arm.insertionValue + insertionDelta, insertionMin, insertionMax);
    arm.yawValue = Mathf.Clamp(arm.yawValue + yawDelta, yawMin, yawMax);
    arm.pitchValue = Mathf.Clamp(arm.pitchValue + pitchDelta, pitchMin, pitchMax);
    arm.lateralValue = Mathf.Clamp(arm.lateralValue + lateralDelta, lateralMin, lateralMax);
    
    // ★★★ Roll: 필터링된 값 사용 ★★★
    arm.rollValue = arm.smoothedRoll;

    ApplyArmTransforms(arm);
    UpdateGripper(arm, triggerValue);

    arm.prevControllerPos = controllerPos;
    arm.prevControllerRot = controllerRot;
}

void ApplyArmTransforms(PSMArm arm)
{
    Vector3 yAxis = arm.useCustomAxes ? arm.customYawAxis : yawAxis;
    Vector3 pAxis = arm.useCustomAxes ? arm.customPitchAxis : pitchAxis;
    Vector3 rAxis = arm.useCustomAxes ? arm.customRollAxis : rollAxis;
    
    float yawVal = arm.yawValue;
    float pitchVal = arm.pitchValue;
    float insertionVal = arm.insertionValue;
    float rollVal = arm.rollValue;
    float lateralVal = arm.lateralValue;
    
    if (arm.invertYaw) yawVal = -yawVal;
    if (arm.invertPitch) pitchVal = -pitchVal;
    if (arm.invertInsertion) insertionVal = -insertionVal;
    if (arm.invertRoll) rollVal = -rollVal;
    
    if (flipYawDirection) yawVal = -yawVal;
    if (flipPitchDirection) pitchVal = -pitchVal;
    if (flipInsertionDirection) insertionVal = -insertionVal;

    // ★★★ INSERTION - LOCAL 좌표 + World 축 오프셋 ★★★
    // 아날로그 상하: Local Y (상=+Y, 하=-Y)
    // 아날로그 좌우: Local X (좌=+X, 우=-X)  
    // X/Y 버튼: World 축 (Inspector에서 선택)
    if (arm.insertionLink != null)
    {
        float newX = arm.insertionInitialPos.x - lateralVal;    // 좌=+X, 우=-X
        float newY = arm.insertionInitialPos.y + insertionVal;  // 상=+Y, 하=-Y
        float newZ = arm.insertionInitialPos.z;  // Local Z는 고정
        
        // 먼저 Local Position 설정
        arm.insertionLink.localPosition = new Vector3(newX, newY, newZ);
        
        // ★★★ World 축 오프셋 적용 ★★★
        if (arm.worldAxisOffset != 0f)
        {
            Vector3 worldPos = arm.insertionLink.position;
            switch (worldMoveAxis)
            {
                case WorldAxis.X:
                    worldPos.x += arm.worldAxisOffset;
                    break;
                case WorldAxis.Y:
                    worldPos.y += arm.worldAxisOffset;
                    break;
                case WorldAxis.Z:
                    worldPos.z += arm.worldAxisOffset;
                    break;
            }
            arm.insertionLink.position = worldPos;
        }
        
        // 회전은 초기 상태 유지
        arm.insertionLink.localRotation = arm.insertionInitialRot;
        
        if (logControllerMovement)
        {
            Debug.Log($"[{arm.name}] LOCAL: X={newX:F4} Y={newY:F4} Z={newZ:F4} | World{worldMoveAxis}Offset={arm.worldAxisOffset:F4}");
        }
    }

    // Yaw
    if (!disableYawPitchControl && arm.yawLink != null)
    {
        arm.yawLink.localRotation = arm.yawInitialRot * Quaternion.AngleAxis(yawVal, yAxis);
    }

    // Pitch
    if (!disableYawPitchControl && arm.pitchLink != null)
    {
        arm.pitchLink.localRotation = arm.pitchInitialRot * Quaternion.AngleAxis(pitchVal, pAxis);
    }

    // Roll - 필터링된 값으로 회전
    if (arm.rollLink != null)
    {
        arm.rollLink.localRotation = arm.rollInitialRot * Quaternion.AngleAxis(rollVal, rAxis);
    }
}

void UpdateGripper(PSMArm arm, float triggerValue)
{
    arm.triggerValue = triggerValue;
    float angle = triggerValue * gripperMaxAngle;

    if (arm.gripperLeft != null)
    {
        arm.gripperLeft.localRotation = Quaternion.AngleAxis(-angle, gripperAxis);
    }
    if (arm.gripperRight != null)
    {
        arm.gripperRight.localRotation = Quaternion.AngleAxis(angle, gripperAxis);
    }
    
    if (arm.enableGrab)
    {
        bool shouldGrab = triggerValue > 0.7f;
        
        if (shouldGrab && !arm.isGrabbing)
        {
            TryGrab(arm);
        }
        else if (!shouldGrab && arm.isGrabbing)
        {
            Release(arm);
        }
    }
}

void TryGrab(PSMArm arm)
{
    Transform grabPoint = arm.grabPoint;
    
    if (grabPoint == null)
    {
        if (arm.gripperLeft != null)
            grabPoint = arm.gripperLeft;
        else if (arm.rollLink != null)
            grabPoint = arm.rollLink;
        else
            return;
    }
    
    Collider[] nearby = Physics.OverlapSphere(grabPoint.position, arm.grabRadius);
    
    foreach (Collider col in nearby)
    {
        if (IsPartOfRobot(col.transform)) continue;
        if (col.isTrigger) continue;
        
        string objectName = col.gameObject.name.ToLower();
        if (!string.IsNullOrEmpty(arm.grabTargetName))
        {
            if (!objectName.Contains(arm.grabTargetName.ToLower()))
            {
                continue;
            }
        }
        
        Rigidbody rb = col.attachedRigidbody;
        if (rb != null)
        {
            rb.isKinematic = true;
            arm.grabbedObject = rb.gameObject;
            arm.originalParent = rb.transform.parent;
            rb.transform.SetParent(grabPoint);
            arm.isGrabbing = true;
            Debug.Log($"[XRDualPSM] {arm.name} GRABBED: {rb.gameObject.name}");
            return;
        }
        else
        {
            arm.grabbedObject = col.gameObject;
            arm.originalParent = col.transform.parent;
            col.transform.SetParent(grabPoint);
            arm.isGrabbing = true;
            Debug.Log($"[XRDualPSM] {arm.name} GRABBED: {col.gameObject.name}");
            return;
        }
    }
}

bool IsPartOfRobot(Transform t)
{
    if (rightPSM != null)
    {
        if (rightPSM.insertionLink != null && t.IsChildOf(rightPSM.insertionLink.root)) return true;
        if (rightPSM.yawLink != null && t.IsChildOf(rightPSM.yawLink.root)) return true;
        if (rightPSM.gripperLeft != null && t.IsChildOf(rightPSM.gripperLeft.root)) return true;
    }
    if (leftPSM != null)
    {
        if (leftPSM.insertionLink != null && t.IsChildOf(leftPSM.insertionLink.root)) return true;
        if (leftPSM.yawLink != null && t.IsChildOf(leftPSM.yawLink.root)) return true;
        if (leftPSM.gripperLeft != null && t.IsChildOf(leftPSM.gripperLeft.root)) return true;
    }
    if (t.IsChildOf(transform)) return true;
    return false;
}

void Release(PSMArm arm)
{
    if (arm.grabbedObject != null)
    {
        arm.grabbedObject.transform.SetParent(arm.originalParent);
        
        Rigidbody rb = arm.grabbedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
        
        Debug.Log($"[XRDualPSM] {arm.name} RELEASED: {arm.grabbedObject.name}");
        arm.grabbedObject = null;
        arm.originalParent = null;
    }
    arm.isGrabbing = false;
}

void HandleKeyboardInput()
{
    if (Input.GetKeyDown(KeyCode.R))
    {
        ResetAllArms();
    }

    float keySpeed = 30f * Time.deltaTime;
    float insertSpeed = 0.1f * Time.deltaTime;

    if (Input.GetKey(KeyCode.PageUp))
        rightPSM.insertionValue = Mathf.Clamp(rightPSM.insertionValue + insertSpeed, insertionMin, insertionMax);
    if (Input.GetKey(KeyCode.PageDown))
        rightPSM.insertionValue = Mathf.Clamp(rightPSM.insertionValue - insertSpeed, insertionMin, insertionMax);

    if (Input.GetKey(KeyCode.E))
        leftPSM.insertionValue = Mathf.Clamp(leftPSM.insertionValue + insertSpeed, insertionMin, insertionMax);
    if (Input.GetKey(KeyCode.Q))
        leftPSM.insertionValue = Mathf.Clamp(leftPSM.insertionValue - insertSpeed, insertionMin, insertionMax);

    ApplyArmTransforms(rightPSM);
    ApplyArmTransforms(leftPSM);
}

void ResetArm(PSMArm arm)
{
    InitializeArm(arm);
    ApplyArmTransforms(arm);
}

void TryGetControllers()
{
    _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    _rightFound = _rightController.isValid;

    _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    _leftFound = _leftController.isValid;
}

float NormalizeAngle(float angle)
{
    while (angle > 180f) angle -= 360f;
    while (angle < -180f) angle += 360f;
    return angle;
}

public void ResetAllArms()
{
    InitializeArm(rightPSM);
    InitializeArm(leftPSM);
    ApplyArmTransforms(rightPSM);
    ApplyArmTransforms(leftPSM);
    Debug.Log("[XRDualPSM] All arms reset");
}

public void Freeze()
{
    isFrozen = true;
    Debug.Log("[XRDualPSM] Frozen");
}

public void Unfreeze()
{
    isFrozen = false;
    Debug.Log("[XRDualPSM] Unfrozen");
}

void OnGUI()
{
    if (!showDebugInfo) return;

    GUILayout.BeginArea(new Rect(10, 10, 350, 450));
    GUILayout.Label("=== XR Dual PSM Controller ===");
    GUILayout.Label($"Controllers: R:{(_rightFound ? "OK" : "--")} L:{(_leftFound ? "OK" : "--")}");

    GUILayout.Space(10);
    GUILayout.Label($"--- {rightPSM.name} (Right) ---");
    GUILayout.Label($"  Roll:{rightPSM.rollValue:F1}°");
    
    GUILayout.Space(5);
    GUILayout.Label($"--- {leftPSM.name} (Left) ---");
    GUILayout.Label($"  Roll:{leftPSM.rollValue:F1}°");
    
    // Show LOCAL position debug
    if (leftPSM.insertionLink != null)
    {
        Vector3 localPos = leftPSM.insertionLink.localPosition;
        Vector3 initial = leftPSM.insertionInitialPos;
        
        GUI.color = Color.cyan;
        GUILayout.Label($"★ LOCAL 좌표 (main_insertion_link):");
        GUILayout.Label($"  Initial: X={initial.x:F4} Y={initial.y:F4} Z={initial.z:F4}");
        GUILayout.Label($"  Current: X={localPos.x:F4} Y={localPos.y:F4} Z={localPos.z:F4}");
        
        float xDelta = localPos.x - initial.x;
        float yDelta = localPos.y - initial.y;
        float zDelta = localPos.z - initial.z;
        
        // Y가 변하면 빨간색으로 경고
        GUI.color = Mathf.Abs(yDelta) > 0.0001f ? Color.red : Color.green;
        GUILayout.Label($"  Delta Y={yDelta:F4} (should be 0!)");
        
        GUI.color = Color.white;
        GUILayout.Label($"  Delta X={xDelta:F4} Z={zDelta:F4}");
        
        GUI.color = Color.yellow;
        GUILayout.Label("상=+Y | 하=-Y | 좌=+X | 우=-X");
        GUI.color = Color.white;
    }
    
    GUILayout.Space(10);
    
    // Roll jitter detection
    GUI.color = Mathf.Abs(leftPSM.rollValue - leftPSM.smoothedRoll) < 1f ? Color.green : Color.red;
    GUILayout.Label($"Roll Smooth: {(Mathf.Abs(leftPSM.rollValue - leftPSM.smoothedRoll) < 1f ? "OK" : "Filtering...")}");
    GUI.color = Color.white;
    
    GUILayout.Label("Keyboard: PgUp/Dn (R) | Q/E (L) | R=Reset");
    
    // World axis position
    if (leftPSM.insertionLink != null)
    {
        GUI.color = Color.magenta;
        GUILayout.Label($"★ World {worldMoveAxis} Offset: {leftPSM.worldAxisOffset:F4}");
        Vector3 wPos = leftPSM.insertionLink.position;
        float axisVal = worldMoveAxis == WorldAxis.X ? wPos.x : (worldMoveAxis == WorldAxis.Y ? wPos.y : wPos.z);
        GUILayout.Label($"★ World {worldMoveAxis} Pos: {axisVal:F4}");
        GUILayout.Label($"X버튼=-{worldMoveAxis} | Y버튼=+{worldMoveAxis}");
        GUI.color = Color.white;
    }
    
    GUILayout.Space(5);
    GUI.color = Color.green;
    GUILayout.Label("아날로그 상하=Local Y | 좌우=Local X");
    GUILayout.Label($"  Insertion(Y): {leftPSM.insertionValue:F4}");
    GUILayout.Label($"  Lateral(X): {leftPSM.lateralValue:F4}");
    GUI.color = Color.white;

    GUILayout.EndArea();
}
}
