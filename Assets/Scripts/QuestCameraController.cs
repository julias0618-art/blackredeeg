// QuestCameraController.cs - Camera Mode Control
// ★ 스페이스바로 Tool Mode ↔ Camera Mode 전환
// Camera Mode: 조이스틱으로 카메라 이동, X=줌인, Y=줌아웃

using UnityEngine;
using UnityEngine.XR;



public class QuestCameraController : MonoBehaviour
{
    [Header("=== LSL ===")]
    public LSLMarkerSender markerSender;
    private int _trialId = 0;

    public enum ControlMode
    {
        ToolMode,
        CameraMode
    }

    [Header("=== EEG Inference Motion ===")]
    public float inferredMoveSpeed = 0.25f;   // units per second (tune)
    public float inferredZoomSpeed = 0.15f;   // scale per second (tune)

    private string _activeDirection = null;   // "left/right/up/down/zoom_in/zoom_out"

    private float _directionStartTime = -1f;
    public float maxInferredDuration = 2f;   // 2 seconds max movement


    [Header("=== ★ 현재 모드 ★ ===")]
    [Tooltip("현재 컨트롤 모드")]
    public ControlMode currentMode = ControlMode.ToolMode;

    [Header("=== 모드 전환 설정 ===")]
    [Tooltip("Camera Mode 키 (누르고 있는 동안만 Camera Mode)")]
    public KeyCode cameraModeKey = KeyCode.Space;
    
    [Tooltip("USB 페달 등 추가 입력")]
    public KeyCode altCameraModeKey = KeyCode.None;

    [Header("=== Camera Reference ===")]
    [Tooltip("이동할 카메라/XR Origin (자동 검색)")]
    public Transform cameraRig;
    
    [Tooltip("실제 카메라 Transform (자동 검색)")]
    public Transform cameraTransform;

    [Header("=== Tool Controller ===")]
    [Tooltip("PSM Controller (자동 검색)")]
    public XRDualPSMDirectController psmController;
    
    [Header("=== Movement Direction (고정 방향) ===")]
    [Tooltip("조이스틱 좌우 → Z축 이동 (우=-Z, 좌=+Z)")]
    public bool useFixedDirection = true;
    
    [Tooltip("카메라 이동 방향 기준 오브젝트 (선택사항)")]
    public Transform movementReference;

    [Header("=== Camera Movement Settings ===")]
    [Tooltip("카메라 이동 속도")]
    public float cameraMoveSpeed = 0.5f;
    
    [Tooltip("줌 속도 (스케일 변화)")]
    public float zoomSpeed = 0.1f;
    
    [Tooltip("최소 스케일 (줌인 한계)")]
    public float minScale = 0.001f;
    
    [Tooltip("최대 스케일 (줌아웃 한계)")]
    public float maxScale = 1f;

    [Header("=== Joystick Settings ===")]
    [Range(0f, 0.5f)]
    public float joystickDeadzone = 0.2f;

    [Header("=== Debug ===")]
    public bool showDebugInfo = true;

    // Controllers
    private InputDevice _leftController;
    private bool _leftFound = false;

    // Input state
    private Vector2 _leftJoystick;
    private bool _xButton;
    private bool _yButton;

    // Static accessor
    public static bool IsInCameraMode { get; private set; }
    
    // 현재 스케일 저장
    private float _currentScale = 1f;

    void Start()
    {
        TryGetControllers();

        // Auto-find camera rig (XR Origin)
        if (cameraRig == null)
        {
            GameObject xrOrigin = GameObject.Find("XR Origin");
            if (xrOrigin != null)
            {
                cameraRig = xrOrigin.transform;
            }
            else
            {
                // Fallback to main camera parent
                Camera cam = Camera.main;
                if (cam != null && cam.transform.parent != null)
                {
                    cameraRig = cam.transform.parent;
                }
            }
        }

        // Auto-find camera
        if (cameraTransform == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

        // Auto-find PSM controller
        if (psmController == null)
        {
            psmController = FindObjectOfType<XRDualPSMDirectController>();
        }
        
        // Auto-find movement reference (main_insertion_link)
        if (movementReference == null && psmController != null)
        {
            if (psmController.leftPSM.insertionLink != null)
            {
                movementReference = psmController.leftPSM.insertionLink;
                Debug.Log($"[CameraController] Movement Reference: {movementReference.name}");
            }
        }
        
        // 초기 스케일 저장
        if (cameraRig != null)
        {
            _currentScale = cameraRig.localScale.x;
        }

        Debug.Log($"[CameraController] Started - CameraRig: {(cameraRig != null ? cameraRig.name : "NULL")}");
    }

    void Update()
    {
        if (!_leftFound) TryGetControllers();
    
        ReadControllerInputs();
        CheckModeToggle();
    
        bool holding =
            Input.GetKey(cameraModeKey) ||
            (altCameraModeKey != KeyCode.None && Input.GetKey(altCameraModeKey));
    
        if (currentMode == ControlMode.CameraMode && !holding)
            ExitCameraMode();
    
        // ✅ only move while in CameraMode
        if (currentMode == ControlMode.CameraMode)
        {
            UpdateCameraMode(); // joystick/manual (optional)
    
            if (!string.IsNullOrEmpty(_activeDirection))
            {
                float elapsed = Time.time - _directionStartTime;
    
                if (elapsed <= maxInferredDuration)
                {
                    ApplyInferredDirectionContinuous(_activeDirection);
                }
                else
                {
                    _activeDirection = null;
                    Debug.Log("[INFERRED] movement auto-stopped (2s)");
                }
            }
        }
        else
        {
            // ✅ extra safety: never keep direction active in ToolMode
            _activeDirection = null;
        }
    
        IsInCameraMode = (currentMode == ControlMode.CameraMode);
    }



    void ReadControllerInputs()
    {
        if (_leftFound && _leftController.isValid)
        {
            _leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out _leftJoystick);
            _leftController.TryGetFeatureValue(CommonUsages.primaryButton, out _xButton);    // X button
            _leftController.TryGetFeatureValue(CommonUsages.secondaryButton, out _yButton);  // Y button
            
            // 데드존 적용
            if (Mathf.Abs(_leftJoystick.x) < joystickDeadzone) _leftJoystick.x = 0f;
            if (Mathf.Abs(_leftJoystick.y) < joystickDeadzone) _leftJoystick.y = 0f;
        }
    }

    void CheckModeToggle()
    {
        // Hold Space = CameraMode
        bool down =
            Input.GetKeyDown(cameraModeKey) ||
            (altCameraModeKey != KeyCode.None && Input.GetKeyDown(altCameraModeKey));

        bool up =
            Input.GetKeyUp(cameraModeKey) ||
            (altCameraModeKey != KeyCode.None && Input.GetKeyUp(altCameraModeKey));

        if (down)
        {
            EnterCameraMode();
        }

        if (up)
        {
            ExitCameraMode();
        }
    }


    void EnterCameraMode()
    {
        if (currentMode == ControlMode.CameraMode) return;
        currentMode = ControlMode.CameraMode;

        _trialId++;
        markerSender?.SendExec(_trialId, ""); // inference trigger

        if (psmController != null) psmController.Freeze();
        Debug.Log("[CameraController] ★★★ CAMERA MODE start ★★★");
    }
    
    void ExitCameraMode()
    {
        if (currentMode == ControlMode.ToolMode) return;
        currentMode = ControlMode.ToolMode;
        _activeDirection = null;

        if (psmController != null) psmController.Unfreeze();
        Debug.Log("[CameraController] ★★★ TOOL MODE 복귀 ★★★");
    }


    void UpdateCameraMode()
    {
        if (cameraRig == null) return;
        
        // ★★★ 조이스틱 입력 - 한 방향만 인식 ★★★
        float joystickX = _leftJoystick.x;
        float joystickY = _leftJoystick.y;
        
        // 더 큰 축만 사용 (섞이지 않게)
        if (Mathf.Abs(joystickX) > 0.1f || Mathf.Abs(joystickY) > 0.1f)
        {
            if (Mathf.Abs(joystickX) > Mathf.Abs(joystickY))
            {
                // 좌우가 더 크면 상하 무시
                joystickY = 0f;
            }
            else
            {
                // 상하가 더 크면 좌우 무시
                joystickX = 0f;
            }
        }
        
        // ★★★ 조이스틱 상하로 줌 (반전: 위=줌아웃, 아래=줌인) ★★★
        float zoomDelta = 0f;
        
        if (joystickY > 0.1f)  // 조이스틱 위 = 줌아웃
        {
            zoomDelta = zoomSpeed * Time.deltaTime;
        }
        if (joystickY < -0.1f)  // 조이스틱 아래 = 줌인
        {
            zoomDelta = -zoomSpeed * Time.deltaTime;
        }
        
        // 키보드 대체 (테스트용)
        if (Input.GetKey(KeyCode.Z)) zoomDelta = -zoomSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.X)) zoomDelta = zoomSpeed * Time.deltaTime;
        
        if (zoomDelta != 0f)
        {
            _currentScale = Mathf.Clamp(_currentScale + zoomDelta, minScale, maxScale);
            cameraRig.localScale = Vector3.one * _currentScale;
        }
        
        // ★★★ 조이스틱 좌우로 카메라 Z 이동 ★★★
        if (Mathf.Abs(joystickX) > 0.1f)
        {
            Vector3 movement = Vector3.zero;
            // 좌=-Z, 우=+Z
            movement.z = joystickX * cameraMoveSpeed * Time.deltaTime;
            cameraRig.position += movement;
        }

        // ★★★ X/Y 버튼으로 카메라 Y 이동 ★★★
        if (_xButton)  // X 버튼 = -Y 이동
        {
            Vector3 movement = Vector3.zero;
            movement.y = -cameraMoveSpeed * Time.deltaTime;
            cameraRig.position += movement;
        }
        if (_yButton)  // Y 버튼 = +Y 이동
        {
            Vector3 movement = Vector3.zero;
            movement.y = cameraMoveSpeed * Time.deltaTime;
            cameraRig.position += movement;
        }
    }

    void TryGetControllers()
    {
        _leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        _leftFound = _leftController.isValid;
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 200));
        
        // 모드 표시 (큰 글씨, 색상)
        GUIStyle modeStyle = new GUIStyle(GUI.skin.label);
        modeStyle.fontSize = 18;
        modeStyle.fontStyle = FontStyle.Bold;
        
        if (currentMode == ControlMode.CameraMode)
        {
            GUI.color = Color.cyan;
            GUILayout.Label("★ CAMERA MODE ★", modeStyle);
            GUI.color = Color.white;
            GUILayout.Label("조이스틱 상=줌아웃, 하=줌인");
            GUILayout.Label("조이스틱 좌=-Z, 우=+Z");
            GUILayout.Label("X버튼=-Y | Y버튼=+Y");
            GUILayout.Label("(한 방향만 인식)");
        }
        else
        {
            GUI.color = Color.green;
            GUILayout.Label("TOOL MODE", modeStyle);
            GUI.color = Color.white;
            GUILayout.Label("조이스틱: 도구 이동");
        }
        
        GUILayout.Space(10);
        GUILayout.Label($"[Space] Hold = Camera Mode");
        GUILayout.Label($"Scale: {_currentScale:F4}");
        
        if (cameraRig != null)
        {
            GUILayout.Label($"Cam Pos: {cameraRig.localPosition}");
        }
        
        GUILayout.EndArea();
    }

    public void OnInferredDirection(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        msg = msg.Trim().ToLower();

        // expected: dir|direction=right
        if (!msg.StartsWith("dir|")) return;

        string direction = null;
        var parts = msg.Split('|');
        foreach (var p in parts)
        {
            if (p.StartsWith("direction="))
            {
                direction = p.Substring("direction=".Length).Trim();
                break;
            }
        }

        if (string.IsNullOrEmpty(direction)) return;

        _activeDirection = direction;
        _directionStartTime = Time.time;   // start timer
        Debug.Log("[INFERRED] active direction = " + _activeDirection);
    }

    void ApplyInferredDirectionContinuous(string direction)
    {
        if (cameraRig == null) return;

        float move = inferredMoveSpeed * Time.deltaTime;
        float zoom = inferredZoomSpeed * Time.deltaTime;

        switch (direction)
        {
            case "left":
                cameraRig.position += new Vector3(0, 0, -move);
                break;

            case "right":
                cameraRig.position += new Vector3(0, 0, move);
                break;

            case "up":
                cameraRig.position += new Vector3(0, move, 0);
                break;

            case "down":
                cameraRig.position += new Vector3(0, -move, 0);
                break;

            case "zoom_in":
                _currentScale = Mathf.Clamp(_currentScale - zoom, minScale, maxScale);
                cameraRig.localScale = Vector3.one * _currentScale;
                break;

            case "zoom_out":
                _currentScale = Mathf.Clamp(_currentScale + zoom, minScale, maxScale);
                cameraRig.localScale = Vector3.one * _currentScale;
                break;
        }
    }
}
