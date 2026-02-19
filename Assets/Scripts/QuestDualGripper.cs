// QuestDualGripper.cs - Dual Hand Gripper Control
// Handles XR controller trigger input for both left and right grippers

using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Handles XR controller trigger input for both left and right grippers.
/// Provides trigger values and pinching state for both hands.
/// </summary>
public class QuestDualGripper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Trigger value threshold for pinching (0-1)")]
    public float pinchThreshold = 0.7f;

    // Right hand
    private float _rightTriggerValue = 0f;
    private bool _rightIsPinching = false;
    private float _rightJawPosition = 0f;

    // Left hand
    private float _leftTriggerValue = 0f;
    private bool _leftIsPinching = false;
    private float _leftJawPosition = 0f;

    // Controllers
    private InputDevice _rightController;
    private InputDevice _leftController;
    private bool _rightFound = false;
    private bool _leftFound = false;

    // Public properties - Right hand
    public float RightTriggerValue => _rightTriggerValue;
    public bool RightIsPinching => _rightIsPinching;
    public float rightJawPosition => _rightJawPosition;

    // Public properties - Left hand
    public float LeftTriggerValue => _leftTriggerValue;
    public bool LeftIsPinching => _leftIsPinching;
    public float leftJawPosition => _leftJawPosition;

    void Start()
    {
        TryGetControllers();
    }

    void Update()
    {
        // Try to find controllers if not found
        if (!_rightFound || !_leftFound)
        {
            TryGetControllers();
        }

        // Read right trigger
        if (_rightFound && _rightController.isValid)
        {
            _rightController.TryGetFeatureValue(CommonUsages.trigger, out _rightTriggerValue);
        }
        else
        {
            // Keyboard fallback for right hand
            _rightTriggerValue = Input.GetKey(KeyCode.RightControl) ? 1f : 0f;
        }

        // Read left trigger
        if (_leftFound && _leftController.isValid)
        {
            _leftController.TryGetFeatureValue(CommonUsages.trigger, out _leftTriggerValue);
        }
        else
        {
            // Keyboard fallback for left hand
            _leftTriggerValue = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        }

        // Update pinching state
        _rightIsPinching = _rightTriggerValue >= pinchThreshold;
        _leftIsPinching = _leftTriggerValue >= pinchThreshold;

        // Calculate jaw positions (0 to -2 range for compatibility)
        _rightJawPosition = -_rightTriggerValue * 2f;
        _leftJawPosition = -_leftTriggerValue * 2f;
    }

    void TryGetControllers()
    {
        _rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        _rightFound = _rightController.isValid;

        _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _leftFound = _leftController.isValid;

        if (_rightFound) Debug.Log($"[QuestDualGripper] Right controller found: {_rightController.name}");
        if (_leftFound) Debug.Log($"[QuestDualGripper] Left controller found: {_leftController.name}");
    }

    void OnGUI()
    {
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(Screen.width - 220, 10, 210, 120));
        GUILayout.Label("=== Dual Gripper ===");
        GUILayout.Label($"Right: {(_rightFound ? "OK" : "KB")} Trigger:{_rightTriggerValue:F2} Pinch:{_rightIsPinching}");
        GUILayout.Label($"Left: {(_leftFound ? "OK" : "KB")} Trigger:{_leftTriggerValue:F2} Pinch:{_leftIsPinching}");
        GUILayout.Label($"Jaw R:{_rightJawPosition:F2} L:{_leftJawPosition:F2}");
        GUILayout.EndArea();
    }
}
