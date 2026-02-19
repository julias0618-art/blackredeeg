// QuestDualToolController.cs - Dual Hand Tool Control
// Controls both left and right surgical tools using XR controllers

using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Controls both left and right surgical tools using corresponding XR controllers.
/// Supports relative movement mode for proper spatial alignment.
/// </summary>
public class QuestDualToolController : MonoBehaviour
{
    [Header("=== Tool Transforms ===")]
    [Tooltip("Transform of the right surgical tool")]
    public Transform rightToolTransform;
    
    [Tooltip("Transform of the left surgical tool")]
    public Transform leftToolTransform;

    [Header("=== Movement Settings ===")]
    [Tooltip("Use relative movement (deltas from starting position)")]
    public bool useRelativeMovement = true;
    
    [Tooltip("Scale factor for position")]
    public float positionScale = 1f;
    
    [Tooltip("Scale factor for rotation")]
    public float rotationScale = 1f;
    
    [Tooltip("Smoothing factor (0 = no smoothing, higher = more smoothing)")]
    [Range(0f, 0.95f)]
    public float smoothing = 0.1f;

    [Header("=== Offset Settings ===")]
    [Tooltip("Position offset applied to tools")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Tooltip("Rotation offset applied to tools (Euler angles)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("=== Calibration ===")]
    [Tooltip("Key to reset tools to original position")]
    public KeyCode resetPositionKey = KeyCode.R;
    
    [Tooltip("Key to move tools down")]
    public KeyCode moveDownKey = KeyCode.PageDown;
    
    [Tooltip("Key to move tools up")]
    public KeyCode moveUpKey = KeyCode.PageUp;
    
    [Tooltip("Height adjustment speed")]
    public float heightAdjustSpeed = 0.1f;

    [Header("=== Debug ===")]
    public bool showDebugInfo = true;

    // Controllers
    private InputDevice _rightController;
    private InputDevice _leftController;
    private bool _rightFound = false;
    private bool _leftFound = false;

    // Right hand tracking
    private Vector3 _rightTargetPosition;
    private Quaternion _rightTargetRotation;
    private Vector3 _rightPrevControllerPos;
    private Quaternion _rightPrevControllerRot;
    private bool _rightInitialized = false;

    // Left hand tracking
    private Vector3 _leftTargetPosition;
    private Quaternion _leftTargetRotation;
    private Vector3 _leftPrevControllerPos;
    private Quaternion _leftPrevControllerRot;
    private bool _leftInitialized = false;

    // Original positions for reset
    private Vector3 _rightOriginalPosition;
    private Vector3 _leftOriginalPosition;
    private Quaternion _rightOriginalRotation;
    private Quaternion _leftOriginalRotation;

    // Freeze state
    private bool _isFrozen = false;
    public bool IsFrozen => _isFrozen;

    void Start()
    {
        TryGetControllers();
        
        // Store original positions
        if (rightToolTransform != null)
        {
            _rightOriginalPosition = rightToolTransform.position;
            _rightOriginalRotation = rightToolTransform.rotation;
            _rightTargetPosition = _rightOriginalPosition;
            _rightTargetRotation = _rightOriginalRotation;
        }
        
        if (leftToolTransform != null)
        {
            _leftOriginalPosition = leftToolTransform.position;
            _leftOriginalRotation = leftToolTransform.rotation;
            _leftTargetPosition = _leftOriginalPosition;
            _leftTargetRotation = _leftOriginalRotation;
        }
    }

    void Update()
    {
        if (!_rightFound || !_leftFound)
        {
            TryGetControllers();
        }

        // Handle calibration input
        HandleCalibrationInput();

        if (_isFrozen) return;

        // Update right hand
        if (_rightFound && _rightController.isValid && rightToolTransform != null)
        {
            UpdateRightHand();
        }

        // Update left hand
        if (_leftFound && _leftController.isValid && leftToolTransform != null)
        {
            UpdateLeftHand();
        }

        // Keyboard fallback
        HandleKeyboardInput();
    }

    void UpdateRightHand()
    {
        Vector3 controllerPos;
        Quaternion controllerRot;

        if (!_rightController.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPos) ||
            !_rightController.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerRot))
        {
            return;
        }

        if (useRelativeMovement)
        {
            if (!_rightInitialized)
            {
                _rightPrevControllerPos = controllerPos;
                _rightPrevControllerRot = controllerRot;
                _rightInitialized = true;
                return;
            }

            // Calculate delta
            Vector3 delta = (controllerPos - _rightPrevControllerPos) * positionScale;
            _rightTargetPosition += delta;

            // Calculate rotation delta
            Quaternion deltaRot = controllerRot * Quaternion.Inverse(_rightPrevControllerRot);
            _rightTargetRotation = Quaternion.Slerp(_rightTargetRotation, deltaRot * _rightTargetRotation, rotationScale);

            _rightPrevControllerPos = controllerPos;
            _rightPrevControllerRot = controllerRot;
        }
        else
        {
            // Absolute position mode
            _rightTargetPosition = controllerPos * positionScale + positionOffset;
            _rightTargetRotation = controllerRot * Quaternion.Euler(rotationOffset);
        }

        // Apply with smoothing
        rightToolTransform.position = Vector3.Lerp(rightToolTransform.position, _rightTargetPosition + positionOffset, 1f - smoothing);
        rightToolTransform.rotation = Quaternion.Slerp(rightToolTransform.rotation, _rightTargetRotation * Quaternion.Euler(rotationOffset), 1f - smoothing);
    }

    void UpdateLeftHand()
    {
        Vector3 controllerPos;
        Quaternion controllerRot;

        if (!_leftController.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPos) ||
            !_leftController.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerRot))
        {
            return;
        }

        if (useRelativeMovement)
        {
            if (!_leftInitialized)
            {
                _leftPrevControllerPos = controllerPos;
                _leftPrevControllerRot = controllerRot;
                _leftInitialized = true;
                return;
            }

            // Calculate delta
            Vector3 delta = (controllerPos - _leftPrevControllerPos) * positionScale;
            _leftTargetPosition += delta;

            // Calculate rotation delta
            Quaternion deltaRot = controllerRot * Quaternion.Inverse(_leftPrevControllerRot);
            _leftTargetRotation = Quaternion.Slerp(_leftTargetRotation, deltaRot * _leftTargetRotation, rotationScale);

            _leftPrevControllerPos = controllerPos;
            _leftPrevControllerRot = controllerRot;
        }
        else
        {
            // Absolute position mode
            _leftTargetPosition = controllerPos * positionScale + positionOffset;
            _leftTargetRotation = controllerRot * Quaternion.Euler(rotationOffset);
        }

        // Apply with smoothing
        leftToolTransform.position = Vector3.Lerp(leftToolTransform.position, _leftTargetPosition + positionOffset, 1f - smoothing);
        leftToolTransform.rotation = Quaternion.Slerp(leftToolTransform.rotation, _leftTargetRotation * Quaternion.Euler(rotationOffset), 1f - smoothing);
    }

    void HandleCalibrationInput()
    {
        // Reset position
        if (Input.GetKeyDown(resetPositionKey))
        {
            ResetToOriginalPosition();
        }

        // Height adjustment
        if (Input.GetKey(moveUpKey))
        {
            AdjustHeight(heightAdjustSpeed * Time.deltaTime);
        }
        if (Input.GetKey(moveDownKey))
        {
            AdjustHeight(-heightAdjustSpeed * Time.deltaTime);
        }
    }

    void HandleKeyboardInput()
    {
        if (rightToolTransform == null) return;

        float moveSpeed = 0.5f * Time.deltaTime;
        float rotSpeed = 30f * Time.deltaTime;

        // Right hand: Numpad
        if (Input.GetKey(KeyCode.Keypad8)) _rightTargetPosition += Vector3.forward * moveSpeed;
        if (Input.GetKey(KeyCode.Keypad2)) _rightTargetPosition += Vector3.back * moveSpeed;
        if (Input.GetKey(KeyCode.Keypad4)) _rightTargetPosition += Vector3.left * moveSpeed;
        if (Input.GetKey(KeyCode.Keypad6)) _rightTargetPosition += Vector3.right * moveSpeed;
        if (Input.GetKey(KeyCode.Keypad9)) _rightTargetPosition += Vector3.up * moveSpeed;
        if (Input.GetKey(KeyCode.Keypad3)) _rightTargetPosition += Vector3.down * moveSpeed;

        // Left hand: WASD + Q/E
        if (leftToolTransform != null)
        {
            if (Input.GetKey(KeyCode.W)) _leftTargetPosition += Vector3.forward * moveSpeed;
            if (Input.GetKey(KeyCode.S)) _leftTargetPosition += Vector3.back * moveSpeed;
            if (Input.GetKey(KeyCode.A)) _leftTargetPosition += Vector3.left * moveSpeed;
            if (Input.GetKey(KeyCode.D)) _leftTargetPosition += Vector3.right * moveSpeed;
            if (Input.GetKey(KeyCode.E)) _leftTargetPosition += Vector3.up * moveSpeed;
            if (Input.GetKey(KeyCode.Q)) _leftTargetPosition += Vector3.down * moveSpeed;
        }
    }

    public void ResetToOriginalPosition()
    {
        if (rightToolTransform != null)
        {
            _rightTargetPosition = _rightOriginalPosition;
            _rightTargetRotation = _rightOriginalRotation;
            rightToolTransform.position = _rightOriginalPosition;
            rightToolTransform.rotation = _rightOriginalRotation;
            _rightInitialized = false;
        }

        if (leftToolTransform != null)
        {
            _leftTargetPosition = _leftOriginalPosition;
            _leftTargetRotation = _leftOriginalRotation;
            leftToolTransform.position = _leftOriginalPosition;
            leftToolTransform.rotation = _leftOriginalRotation;
            _leftInitialized = false;
        }

        Debug.Log("[QuestDualTool] Reset to original position");
    }

    public void AdjustHeight(float delta)
    {
        positionOffset.y += delta;
        Debug.Log($"[QuestDualTool] Height offset: {positionOffset.y:F3}");
    }

    public void Freeze()
    {
        _isFrozen = true;
        Debug.Log("[QuestDualTool] FROZEN");
    }

    public void Unfreeze()
    {
        _isFrozen = false;
        _rightInitialized = false;
        _leftInitialized = false;
        Debug.Log("[QuestDualTool] UNFROZEN");
    }

    void TryGetControllers()
    {
        _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        _rightFound = _rightController.isValid;

        _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _leftFound = _leftController.isValid;
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 400, 300, 200));
        GUILayout.Label("=== Quest Dual Tool ===");
        GUILayout.Label($"Right: {(_rightFound ? "OK" : "---")} | Left: {(_leftFound ? "OK" : "---")}");
        GUILayout.Label($"Frozen: {_isFrozen}");
        GUILayout.Label($"Offset: {positionOffset}");
        GUILayout.Label("---");
        GUILayout.Label("[R] Reset | [PgUp/Dn] Height");
        GUILayout.Label("Right: Numpad | Left: WASD+Q/E");
        GUILayout.EndArea();
    }
}
