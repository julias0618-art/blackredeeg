// QuestGripper.cs - Single Hand Gripper Control
// Handles XR controller trigger input for a single gripper

using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Handles XR controller trigger input for a single gripper.
/// Can be configured for either right or left hand.
/// </summary>
public class QuestGripper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Which hand to use")]
    public XRNode handNode = XRNode.RightHand;
    
    [Tooltip("Trigger value threshold for pinching (0-1)")]
    public float pinchThreshold = 0.7f;

    // State
    private float _triggerValue = 0f;
    private bool _isPinching = false;
    private float _jawPosition = 0f;

    // Controller
    private InputDevice _controller;
    private bool _controllerFound = false;

    // Public properties
    public float TriggerValue => _triggerValue;
    public bool IsPinching => _isPinching;
    public float jawPosition => _jawPosition;

    void Start()
    {
        TryGetController();
    }

    void Update()
    {
        if (!_controllerFound)
        {
            TryGetController();
        }

        // Read trigger
        if (_controllerFound && _controller.isValid)
        {
            _controller.TryGetFeatureValue(CommonUsages.trigger, out _triggerValue);
        }
        else
        {
            // Keyboard fallback
            _triggerValue = Input.GetKey(KeyCode.Space) ? 1f : 0f;
        }

        // Update pinching state
        _isPinching = _triggerValue >= pinchThreshold;

        // Calculate jaw position (0 to -2 range for compatibility)
        _jawPosition = -_triggerValue * 2f;
    }

    void TryGetController()
    {
        _controller = InputDevices.GetDeviceAtXRNode(handNode);
        _controllerFound = _controller.isValid;

        if (_controllerFound)
        {
            Debug.Log($"[QuestGripper] Controller found: {_controller.name}");
        }
    }
}
