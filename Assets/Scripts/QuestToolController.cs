// QuestToolController.cs - Single Hand Tool Control
// Controls a single surgical tool using an XR controller

using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Controls a single surgical tool using an XR controller.
/// Can be configured for either right or left hand.
/// </summary>
public class QuestToolController : MonoBehaviour
{
    [Header("=== Settings ===")]
    [Tooltip("Which hand to use")]
    public XRNode handNode = XRNode.RightHand;
    
    [Tooltip("Transform of the tool to control")]
    public Transform toolTransform;

    [Header("=== Movement Settings ===")]
    [Tooltip("Use relative movement (deltas from starting position)")]
    public bool useRelativeMovement = true;
    
    [Tooltip("Scale factor for position")]
    public float positionScale = 1f;
    
    [Tooltip("Scale factor for rotation")]
    public float rotationScale = 1f;
    
    [Tooltip("Smoothing factor")]
    [Range(0f, 0.95f)]
    public float smoothing = 0.1f;

    [Header("=== Offset Settings ===")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("=== Debug ===")]
    public bool showDebugInfo = false;

    // Controller
    private InputDevice _controller;
    private bool _controllerFound = false;

    // Tracking
    private Vector3 _targetPosition;
    private Quaternion _targetRotation;
    private Vector3 _prevControllerPos;
    private Quaternion _prevControllerRot;
    private bool _initialized = false;

    // Original position for reset
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;

    // Freeze state
    private bool _isFrozen = false;
    public bool IsFrozen => _isFrozen;

    void Start()
    {
        TryGetController();

        if (toolTransform != null)
        {
            _originalPosition = toolTransform.position;
            _originalRotation = toolTransform.rotation;
            _targetPosition = _originalPosition;
            _targetRotation = _originalRotation;
        }
    }

    void Update()
    {
        if (!_controllerFound)
        {
            TryGetController();
        }

        if (_isFrozen) return;

        if (_controllerFound && _controller.isValid && toolTransform != null)
        {
            UpdateTool();
        }

        // Keyboard fallback
        HandleKeyboardInput();
    }

    void UpdateTool()
    {
        Vector3 controllerPos;
        Quaternion controllerRot;

        if (!_controller.TryGetFeatureValue(CommonUsages.devicePosition, out controllerPos) ||
            !_controller.TryGetFeatureValue(CommonUsages.deviceRotation, out controllerRot))
        {
            return;
        }

        if (useRelativeMovement)
        {
            if (!_initialized)
            {
                _prevControllerPos = controllerPos;
                _prevControllerRot = controllerRot;
                _initialized = true;
                return;
            }

            // Calculate delta
            Vector3 delta = (controllerPos - _prevControllerPos) * positionScale;
            _targetPosition += delta;

            // Calculate rotation delta
            Quaternion deltaRot = controllerRot * Quaternion.Inverse(_prevControllerRot);
            _targetRotation = Quaternion.Slerp(_targetRotation, deltaRot * _targetRotation, rotationScale);

            _prevControllerPos = controllerPos;
            _prevControllerRot = controllerRot;
        }
        else
        {
            _targetPosition = controllerPos * positionScale + positionOffset;
            _targetRotation = controllerRot * Quaternion.Euler(rotationOffset);
        }

        // Apply with smoothing
        toolTransform.position = Vector3.Lerp(toolTransform.position, _targetPosition + positionOffset, 1f - smoothing);
        toolTransform.rotation = Quaternion.Slerp(toolTransform.rotation, _targetRotation * Quaternion.Euler(rotationOffset), 1f - smoothing);
    }

    void HandleKeyboardInput()
    {
        if (toolTransform == null) return;

        float moveSpeed = 0.5f * Time.deltaTime;

        // WASD + Q/E for movement
        if (Input.GetKey(KeyCode.W)) _targetPosition += Vector3.forward * moveSpeed;
        if (Input.GetKey(KeyCode.S)) _targetPosition += Vector3.back * moveSpeed;
        if (Input.GetKey(KeyCode.A)) _targetPosition += Vector3.left * moveSpeed;
        if (Input.GetKey(KeyCode.D)) _targetPosition += Vector3.right * moveSpeed;
        if (Input.GetKey(KeyCode.E)) _targetPosition += Vector3.up * moveSpeed;
        if (Input.GetKey(KeyCode.Q)) _targetPosition += Vector3.down * moveSpeed;

        // TFGH for rotation
        float rotSpeed = 30f * Time.deltaTime;
        if (Input.GetKey(KeyCode.T)) _targetRotation *= Quaternion.Euler(-rotSpeed, 0, 0);
        if (Input.GetKey(KeyCode.G)) _targetRotation *= Quaternion.Euler(rotSpeed, 0, 0);
        if (Input.GetKey(KeyCode.F)) _targetRotation *= Quaternion.Euler(0, -rotSpeed, 0);
        if (Input.GetKey(KeyCode.H)) _targetRotation *= Quaternion.Euler(0, rotSpeed, 0);
    }

    public void Freeze()
    {
        _isFrozen = true;
    }

    public void Unfreeze()
    {
        _isFrozen = false;
        _initialized = false;
    }

    public void ResetPosition()
    {
        _targetPosition = _originalPosition;
        _targetRotation = _originalRotation;
        if (toolTransform != null)
        {
            toolTransform.position = _originalPosition;
            toolTransform.rotation = _originalRotation;
        }
        _initialized = false;
    }

    void TryGetController()
    {
        _controller = InputDevices.GetDeviceAtXRNode(handNode);
        _controllerFound = _controller.isValid;
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 300, 250, 100));
        GUILayout.Label($"=== Quest Tool ({handNode}) ===");
        GUILayout.Label($"Controller: {(_controllerFound ? "OK" : "---")}");
        GUILayout.Label($"Frozen: {_isFrozen}");
        GUILayout.EndArea();
    }
}
