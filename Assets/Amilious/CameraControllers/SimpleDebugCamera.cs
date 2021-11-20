 using Amilious.Core.Extensions;
 using UnityEngine;
using UnityEngine.InputSystem;
#if ENABLE_INPUT_SYSTEM
#endif

namespace Amilious.CameraControllers {
    
    public class SimpleDebugCamera : MonoBehaviour {
        private class CameraState {
            public float yaw;
            public float pitch;
            private float _roll;
            private float _x;
            private float _y;
            private float _z;

            public void SetFromTransform(Transform t) {
                t.eulerAngles.GetValues(out pitch, out yaw, out _roll);
                t.position.GetValues(out _x,out _y,out _z);
            }

            public void Translate(Vector3 translation) {
                var rotatedTranslation = Quaternion.Euler(pitch, yaw, _roll) * translation;
                _x += rotatedTranslation.x;
                _y += rotatedTranslation.y;
                _z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct) {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                _roll = Mathf.Lerp(_roll, target._roll, rotationLerpPct);
                
                _x = Mathf.Lerp(_x, target._x, positionLerpPct);
                _y = Mathf.Lerp(_y, target._y, positionLerpPct);
                _z = Mathf.Lerp(_z, target._z, positionLerpPct);
            }

            public void UpdateTransform(Transform t) {
                t.eulerAngles = new Vector3(pitch, yaw, _roll);
                t.position = new Vector3(_x, _y, _z);
            }
        }

        private readonly CameraState _mTargetCameraState = new CameraState();
        private readonly CameraState _mInterpolatingCameraState = new CameraState();

        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f, 0f, 5f), 
            new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        #if ENABLE_INPUT_SYSTEM
        private InputAction _movementAction;
        private InputAction _verticalMovementAction;
        private InputAction _lookAction;
        private InputAction _boostFactorAction;

        private void Start() {
            var map = new InputActionMap("Simple Camera Controller");

            _lookAction = map.AddAction("look", binding: "<Mouse>/delta");
            _movementAction = map.AddAction("move", binding: "<Gamepad>/leftStick");
            _verticalMovementAction = map.AddAction("Vertical Movement");
            _boostFactorAction = map.AddAction("Boost Factor", binding: "<Mouse>/scroll");

            _lookAction.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=15, y=15)");
            _movementAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            _verticalMovementAction.AddCompositeBinding("Dpad")
                .With("Up", "<Keyboard>/pageUp")
                .With("Down", "<Keyboard>/pageDown")
                .With("Up", "<Keyboard>/e")
                .With("Down", "<Keyboard>/q")
                // ReSharper disable once StringLiteralTypo
                .With("Up", "<Gamepad>/rightshoulder")
                // ReSharper disable once StringLiteralTypo
                .With("Down", "<Gamepad>/leftshoulder");
            _boostFactorAction.AddBinding("<Gamepad>/Dpad").WithProcessor("scaleVector2(x=1, y=4)");

            _movementAction.Enable();
            _lookAction.Enable();
            _verticalMovementAction.Enable();
            _boostFactorAction.Enable();
        }
        #endif

        private void OnEnable() {
            _mTargetCameraState.SetFromTransform(transform);
            _mInterpolatingCameraState.SetFromTransform(transform);
        }

        private Vector3 GetInputTranslationDirection() {
            var direction = Vector3.zero;
            #if ENABLE_INPUT_SYSTEM
            var moveDelta = _movementAction.ReadValue<Vector2>();
            direction.x = moveDelta.x;
            direction.z = moveDelta.y;
            direction.y = _verticalMovementAction.ReadValue<Vector2>().y;
            #else
            if (Input.GetKey(KeyCode.W))direction += Vector3.forward;
            if (Input.GetKey(KeyCode.S))direction += Vector3.back;
            if (Input.GetKey(KeyCode.A))direction += Vector3.left;
            if (Input.GetKey(KeyCode.D))direction += Vector3.right;
            if (Input.GetKey(KeyCode.Q))direction += Vector3.down;
            if (Input.GetKey(KeyCode.E))direction += Vector3.up;
            #endif
            return direction;
        }

        private void Update() {
            // Exit Sample  

            if (IsEscapePressed()) {
                Application.Quit();
				#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false; 
				#endif
            }

            // Hide and lock cursor when right mouse button pressed
            if (IsRightMouseButtonDown()) {
                Cursor.lockState = CursorLockMode.Locked;
            }

            // Unlock and show cursor when right mouse button released
            if (IsRightMouseButtonUp()) {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            // Rotation
            if (IsCameraRotationAllowed()) {
                var mouseMovement = GetInputLookRotation() * (Time.deltaTime * 5);
                if (invertY) mouseMovement.y = -mouseMovement.y;
                
                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                _mTargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                _mTargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }
            
            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (IsBoostPressed()) {
                translation *= 10.0f;
            }
            
            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += GetBoostFactor();
            translation *= Mathf.Pow(2.0f, boost);

            _mTargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            _mInterpolatingCameraState.LerpTowards(_mTargetCameraState, positionLerpPct, rotationLerpPct);

            _mInterpolatingCameraState.UpdateTransform(transform);
        }

        private float GetBoostFactor() {
            #if ENABLE_INPUT_SYSTEM
            return _boostFactorAction.ReadValue<Vector2>().y * 0.01f;
            #else
            return Input.mouseScrollDelta.y * 0.2f;
            #endif
        }

        private Vector2 GetInputLookRotation() {
            #if ENABLE_INPUT_SYSTEM
            return _lookAction.ReadValue<Vector2>();
            #else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10;
            #endif
        }

        private static bool IsBoostPressed() {
            #if ENABLE_INPUT_SYSTEM
            var boost = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed; 
            boost |= Gamepad.current != null && Gamepad.current.xButton.isPressed;
            return boost;
            #else
            return Input.GetKey(KeyCode.LeftShift);
            #endif
        }

        private static bool IsEscapePressed() {
            #if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.isPressed; 
            #else
            return Input.GetKey(KeyCode.Escape);
            #endif
        }

        private static bool IsCameraRotationAllowed() {
            #if ENABLE_INPUT_SYSTEM
            var canRotate = Mouse.current != null && Mouse.current.rightButton.isPressed;
            canRotate |= Gamepad.current != null && Gamepad.current.rightStick.ReadValue().magnitude > 0;
            return canRotate;
            #else
            return Input.GetMouseButton(1);
            #endif
        }

        private static bool IsRightMouseButtonDown() {
            #if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.isPressed;
            #else
            return Input.GetMouseButtonDown(1);
            #endif
        }

        private static bool IsRightMouseButtonUp() {
            #if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && !Mouse.current.rightButton.isPressed;
            #else
            return Input.GetMouseButtonUp(1);
            #endif
        }

    }

}