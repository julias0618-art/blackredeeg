// IsPinchableDuo.cs - Enhanced with auto-material creation
// Modified for Quest Standalone - No ROS dependency
// ★ IMPROVED: Auto-creates materials if not found, better visual feedback

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(SphereCollider)), RequireComponent(typeof(Rigidbody))]
public class IsPinchableDuo : MonoBehaviour
{
    [Header("=== Visual Feedback Colors (색상 설정) ===")]
    [Tooltip("Color when gripper is in range (범위 안에 있을 때)")]
    public Color inRangeColor = Color.yellow;
    
    [Tooltip("Color when gripper physically touches (실제로 닿았을 때)")]
    public Color touchingColor = new Color(1f, 0.4f, 0.7f);  // 분홍색
    
    [Tooltip("Color when object is being held (잡고 있을 때)")]
    public Color pinchedColor = Color.green;
    
    [Tooltip("Color when both grippers can reach (양쪽 다 닿을 때)")]
    public Color switchableColor = Color.cyan;
    
    [Tooltip("Color when exchange was missed (교환 실패)")]
    public Color missedColor = Color.red;
    
    [Header("=== Pincher Settings ===")]
    public Transform pincherObject1;  // Right PSM tip
    public Transform pincherObject2;  // Left PSM tip
    
    [Header("=== Grab Range Multiplier ===")]
    [Tooltip("Multiply the collider radius for grab range (작을수록 더 가까이 가야 노란색)")]
    [Range(0.5f, 5f)]
    public float grabRangeMultiplier = 0.8f;  // ★ 기본값 줄임 (더 가까이 가야 노란색)
    
    [Header("=== Options ===")]
    public bool graphics = false;
    public bool restoreGravity = false;
    
    [Header("=== Debug Info (Read Only) ===")]
    public int whopinched = 0;
    public int whopinchable = 0;
    public float distanceToRight = 0f;
    public float distanceToLeft = 0f;
    public float effectiveRadius = 0f;

    [HideInInspector] public bool pinchingAction1 = false;
    [HideInInspector] public bool pinchingAction2 = false;
    [HideInInspector] public bool pinched1 = false;
    [HideInInspector] public bool pinched2 = false;
    [HideInInspector] public bool pinchable1 = false;
    [HideInInspector] public bool pinchable2 = false;
    [HideInInspector] public bool drop1 = false;
    [HideInInspector] public bool drop2 = false;
    [HideInInspector] public bool missexchange = false;
    [HideInInspector] public bool pinched = false;
    
    // Materials (auto-created)
    private Material materialOwn;
    private Material materialInRange;    // 범위 안 (노란색)
    private Material materialTouching;   // 실제 닿음 (분홍색)
    private Material materialPinched;
    private Material materialSwitchable;
    private Material materialMissed;
    
    // ★★★ 간단한 색상 변화 ★★★
    // 범위 안 = 노란색
    // 범위 안에서 trigger 눌렀다 떼면 = 영구 분홍색
    private bool wasInRangeAndTriggered = false;  // 범위 안에서 trigger를 누른 적 있음
    private bool permanentPink = false;           // 영구 분홍색 활성화
    
    // References
    private XRDualPSMDirectController _psmController;
    private Renderer _renderer;
    private SphereCollider _collider;
    private Rigidbody _rigidbody;
    
    // Initial state
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Color _originalColor;
    
    void Start()
    {
        // Cache components
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<SphereCollider>();
        _rigidbody = GetComponent<Rigidbody>();
        
        // Save initial state
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        
        // ★ 원래 색상 저장 (Material이 아닌 Color로!)
        if (_renderer != null && _renderer.sharedMaterial != null)
        {
            _originalColor = _renderer.sharedMaterial.color;
        }
        
        // Create feedback materials
        CreateMaterials();
        
        // ★ 원래 Material 새로 생성 (복제 시 꼬임 방지)
        materialOwn = new Material(Shader.Find("Standard"));
        materialOwn.color = _originalColor;
        materialOwn.SetFloat("_Glossiness", 0.8f);
        
        // ★ 시작 시 원래 색으로 초기화
        if (_renderer != null)
        {
            _renderer.sharedMaterial = materialOwn;
        }
        
        // ★ 상태 초기화 (복제 시 이전 상태 제거)
        permanentPink = false;
        wasInRangeAndTriggered = false;
        
        // Setup rigidbody
        if (_rigidbody != null)
        {
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
        }
        
        // Find pincher objects if not set
        FindPincherObjects();
        
        // Find PSM controller
        _psmController = FindObjectOfType<XRDualPSMDirectController>();
        
        if (_psmController != null)
        {
            Debug.Log($"[IsPinchableDuo] {gameObject.name} - Found PSM Controller");
            Debug.Log($"[IsPinchableDuo] {gameObject.name} - Pincher1: {(pincherObject1 != null ? pincherObject1.name : "NULL")}");
            Debug.Log($"[IsPinchableDuo] {gameObject.name} - Pincher2: {(pincherObject2 != null ? pincherObject2.name : "NULL")}");
        }
        else
        {
            Debug.LogWarning($"[IsPinchableDuo] {gameObject.name} - XRDualPSMDirectController not found!");
        }
        
        Debug.Log($"[IsPinchableDuo] {gameObject.name} - Original Color: {_originalColor}, GrabRange: {grabRangeMultiplier}");
    }
    
    void CreateMaterials()
    {
        // ★ 범위 안 = 노란색
        materialInRange = new Material(Shader.Find("Standard"));
        materialInRange.color = inRangeColor;
        materialInRange.SetFloat("_Glossiness", 0.8f);
        
        // ★ 실제 닿음 = 분홍색
        materialTouching = new Material(Shader.Find("Standard"));
        materialTouching.color = touchingColor;
        materialTouching.SetFloat("_Glossiness", 0.8f);
        
        // 잡힘 = 초록색
        materialPinched = new Material(Shader.Find("Standard"));
        materialPinched.color = pinchedColor;
        materialPinched.SetFloat("_Glossiness", 0.8f);
        
        // 양쪽 닿음 = 시안색
        materialSwitchable = new Material(Shader.Find("Standard"));
        materialSwitchable.color = switchableColor;
        materialSwitchable.SetFloat("_Glossiness", 0.8f);
        
        // 교환 실패 = 빨간색
        materialMissed = new Material(Shader.Find("Standard"));
        materialMissed.color = missedColor;
        materialMissed.SetFloat("_Glossiness", 0.8f);
        
        Debug.Log("[IsPinchableDuo] Materials created: InRange(Yellow), Touching(Pink), Pinched(Green)");
    }
    
    void FindPincherObjects()
    {
        // ★ PSM Controller에서 한 팔의 두 gripper 가져오기
        if (_psmController == null)
            _psmController = FindObjectOfType<XRDualPSMDirectController>();
            
        if (_psmController != null)
        {
            // ★ 한 팔(leftPSM)의 왼쪽/오른쪽 gripper를 pincherObject1/2로 사용
            if (pincherObject1 == null && _psmController.leftPSM.gripperLeft != null)
            {
                pincherObject1 = _psmController.leftPSM.gripperLeft;
                Debug.Log($"[IsPinchableDuo] pincherObject1 = leftPSM.gripperLeft: {pincherObject1.name}");
            }
            if (pincherObject2 == null && _psmController.leftPSM.gripperRight != null)
            {
                pincherObject2 = _psmController.leftPSM.gripperRight;
                Debug.Log($"[IsPinchableDuo] pincherObject2 = leftPSM.gripperRight: {pincherObject2.name}");
            }
            
            // rightPSM도 사용한다면 (백업)
            if (pincherObject1 == null && _psmController.rightPSM.gripperLeft != null)
            {
                pincherObject1 = _psmController.rightPSM.gripperLeft;
                Debug.Log($"[IsPinchableDuo] pincherObject1 = rightPSM.gripperLeft");
            }
            if (pincherObject2 == null && _psmController.rightPSM.gripperRight != null)
            {
                pincherObject2 = _psmController.rightPSM.gripperRight;
                Debug.Log($"[IsPinchableDuo] pincherObject2 = rightPSM.gripperRight");
            }
        }
        
        if (pincherObject1 == null || pincherObject2 == null)
        {
            Debug.LogWarning("[IsPinchableDuo] Gripper not found! Please assign pincherObject1/2 in Inspector.");
        }
    }

    void Update()
    {
        // Get pinch states from PSM controller
        if (_psmController != null)
        {
            pinchingAction1 = _psmController.rightPSM.triggerValue > 0.7f;
            pinchingAction2 = _psmController.leftPSM.triggerValue > 0.7f;
        }
        else
        {
            // Keyboard fallback for testing
            pinchingAction1 = Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.Mouse0);
            pinchingAction2 = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Mouse1);
        }

        // Check for drops
        drop1 = pinched1 && !pinchingAction1;
        drop2 = pinched2 && !pinchingAction2;

        // Calculate effective grab radius
        float baseRadius = _collider != null ? _collider.radius : 0.5f;
        float scale = transform.lossyScale.x;
        effectiveRadius = baseRadius * scale * grabRangeMultiplier;
        
        // Get positions
        Vector3 targetPos = transform.position;
        Vector3 tool1Pos = pincherObject1 != null ? pincherObject1.position : Vector3.zero;
        Vector3 tool2Pos = pincherObject2 != null ? pincherObject2.position : Vector3.zero;
        
        // Calculate distances
        distanceToRight = pincherObject1 != null ? Vector3.Distance(targetPos, tool1Pos) : float.MaxValue;
        distanceToLeft = pincherObject2 != null ? Vector3.Distance(targetPos, tool2Pos) : float.MaxValue;

        // Check pinchable state for gripper 1 (right)
        if (distanceToRight < effectiveRadius)
        {
            pinchable1 = true;
            whopinchable = 1;
            
            if (pinchingAction1)
            {
                pinched1 = true;
                whopinched = 1;
            }
            else
            {
                pinched1 = false;
                if (whopinched == 1) whopinched = 0;
            }
        }
        else
        {
            pinchable1 = false;
            if (whopinchable == 1) whopinchable = 0;
        }

        // Check pinchable state for gripper 2 (left)
        if (distanceToLeft < effectiveRadius)
        {
            pinchable2 = true;
            whopinchable = 2;
            
            if (pinchingAction2)
            {
                pinched2 = true;
                whopinched = 2;
            }
            else
            {
                pinched2 = false;
                if (whopinched == 2) whopinched = 0;
            }
        }
        else
        {
            pinchable2 = false;
            if (whopinchable == 2) whopinchable = 0;
        }

        // Release check
        if (!pinchingAction1) pinched1 = false;
        if (!pinchingAction2) pinched2 = false;
        
        // Miss exchange detection
        missexchange = false;
        if (pinched1 && !pinched2)
        {
            if (distanceToLeft < effectiveRadius * 1.5f && distanceToLeft > effectiveRadius && pinchingAction2)
            {
                missexchange = true;
            }
        }
        if (pinched2 && !pinched1)
        {
            if (distanceToRight < effectiveRadius * 1.5f && distanceToRight > effectiveRadius && pinchingAction1)
            {
                missexchange = true;
            }
        }
        if (distanceToRight < effectiveRadius * 1.5f && drop2 && !pinchingAction1) missexchange = true;
        if (distanceToLeft < effectiveRadius * 1.5f && drop1 && !pinchingAction2) missexchange = true;

        // Combined states
        bool pinchable = pinchable1 || pinchable2;
        pinched = pinched1 || pinched2;

        // Debug visualization
        if (graphics)
        {
            if (pincherObject1 != null) Debug.DrawLine(tool1Pos, targetPos, pinchable1 ? Color.yellow : Color.gray);
            if (pincherObject2 != null) Debug.DrawLine(tool2Pos, targetPos, pinchable2 ? Color.cyan : Color.gray);
        }

        // Handle physics and parenting
        HandleGrabPhysics();
        
        // ★ 범위 안에서 trigger 눌렀다 떼면 영구 분홍색
        CheckTriggerInRange(pinchable);
        
        // Update visual feedback (색상 변경!)
        UpdateVisualFeedback(pinchable);
    }
    
    void HandleGrabPhysics()
    {
        if (_rigidbody == null) return;
        
        if (pinched)
        {
            // Make non-kinematic for physics joint
            _rigidbody.isKinematic = false;
            
            // Create FixedJoint if not exists
            FixedJoint joint = GetComponent<FixedJoint>();
            if (joint == null)
            {
                joint = gameObject.AddComponent<FixedJoint>();
            }
            
            // Connect to appropriate gripper
            if (whopinched == 1 && pincherObject1 != null)
            {
                Rigidbody targetRb = pincherObject1.GetComponent<Rigidbody>();
                if (targetRb == null)
                {
                    // Add rigidbody to gripper if needed
                    targetRb = pincherObject1.gameObject.AddComponent<Rigidbody>();
                    targetRb.isKinematic = true;
                }
                joint.connectedBody = targetRb;
            }
            else if (whopinched == 2 && pincherObject2 != null)
            {
                Rigidbody targetRb = pincherObject2.GetComponent<Rigidbody>();
                if (targetRb == null)
                {
                    targetRb = pincherObject2.gameObject.AddComponent<Rigidbody>();
                    targetRb.isKinematic = true;
                }
                joint.connectedBody = targetRb;
            }
        }
        else
        {
            // Remove joint when released
            FixedJoint joint = GetComponent<FixedJoint>();
            if (joint != null)
            {
                DestroyImmediate(joint);
            }
            
            // Handle gravity/kinematic state
            if (restoreGravity)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.useGravity = true;
            }
            else
            {
                _rigidbody.isKinematic = true;
                _rigidbody.useGravity = false;
            }
        }
    }
    
    void UpdateVisualFeedback(bool pinchable)
    {
        if (_renderer == null) return;
        
        // ★★★ 영구 분홍색이면 다른 색상 무시! ★★★
        if (permanentPink)
        {
            if (_renderer.sharedMaterial != materialTouching)
            {
                _renderer.sharedMaterial = materialTouching;
            }
            return;
        }
        
        // ★★★ 범위 안 = 노란색 ★★★
        if (pinchable)
        {
            if (_renderer.sharedMaterial != materialInRange)
            {
                _renderer.sharedMaterial = materialInRange;
            }
        }
        else
        {
            // 범위 밖 = 원래 색
            if (_renderer.sharedMaterial != materialOwn && materialOwn != null)
            {
                _renderer.sharedMaterial = materialOwn;
            }
        }
    }
    
    // ★★★ 범위 안에서 trigger 눌렀다 떼면 영구 분홍색 ★★★
    void CheckTriggerInRange(bool pinchable)
    {
        if (permanentPink) return;  // 이미 분홍색이면 체크 안함
        
        bool triggerPressed = pinchingAction1 || pinchingAction2;
        
        // 범위 안에서 trigger를 누름
        if (pinchable && triggerPressed)
        {
            wasInRangeAndTriggered = true;
            if (graphics) Debug.Log("[IsPinchableDuo] Trigger pressed while in range!");
        }
        
        // 범위 안에서 trigger를 눌렀다가 뗌 → 영구 분홍색!
        if (wasInRangeAndTriggered && !triggerPressed)
        {
            permanentPink = true;
            if (graphics) Debug.Log("[IsPinchableDuo] ★ Trigger released in range → PERMANENT PINK!");
        }
    }
    
    // Gripper인지 확인
    bool IsGripper(Transform obj)
    {
        if (obj == null) return false;
        
        // pincherObject1 또는 pincherObject2와 같은지, 또는 그 자식인지 확인
        if (pincherObject1 != null)
        {
            if (obj == pincherObject1 || obj.IsChildOf(pincherObject1)) return true;
        }
        if (pincherObject2 != null)
        {
            if (obj == pincherObject2 || obj.IsChildOf(pincherObject2)) return true;
        }
        
        // 이름으로도 체크 (gripper 관련 이름)
        string name = obj.name.ToLower();
        if (name.Contains("gripper") || name.Contains("pinch") || name.Contains("tool"))
        {
            return true;
        }
        
        return false;
    }
    
    // Reset to initial position (call from other scripts if needed)
    public void ResetPosition()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        
        if (_rigidbody != null)
        {
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }
        
        // Reset states
        pinched1 = false;
        pinched2 = false;
        pinchable1 = false;
        pinchable2 = false;
        whopinched = 0;
        
        // ★ 색상도 리셋
        wasInRangeAndTriggered = false;
        permanentPink = false;
        
        // Remove any joint
        FixedJoint joint = GetComponent<FixedJoint>();
        if (joint != null)
        {
            DestroyImmediate(joint);
        }
    }
    
    // Draw grab range in Scene view
    void OnDrawGizmosSelected()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        if (col != null)
        {
            float radius = col.radius * transform.lossyScale.x * grabRangeMultiplier;
            
            // Draw grab range
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);
            
            // Draw inner collider
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, col.radius * transform.lossyScale.x);
        }
    }
}