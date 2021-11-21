using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using Amilious.Core.Extensions;
using Amilious.ProceduralTerrain.Map;

namespace Amilious.ValueAdds
{
	public class FlyCamera : MonoBehaviour, ICameraActions {
		
		#region Public Instance Variables
		
		// Check if Player has New Input System
		#if UNITY_EDITOR
		[ValidateInput("@IsInputSystemEnabled", "@inputSystemMessage", InfoMessageType.Error)]
		#endif
		[MinMaxSlider(-80,80, true)]
		[TabGroup("General")]
		public Vector2 rotationLimit;
		[TabGroup("General")]
		public bool canCapture = true;
		
		[TabGroup("Speed")]
		[Range(0,20)]
		public float lookSpeed   = 5f;
		[TabGroup("Speed")]
		[Range(0,100)]
		public float moveSpeed   = 5f;
		[TabGroup("Speed")]
		[Range(0,200)]
		public float sprintSpeed = 50f;

		#endregion
		
		#region Private Instance Variables
		
		private MapManager _mapManager;
		
		private Vector2 _moveInput;
		private Vector2 _flyInput;
		
		private float _sprintInput;
		private float _rotateInput;
		private float _yaw;
		private float _pitch;
		private float _mouseX;
		private float _mouseY;
		
		private bool _inputCaptured;
		private bool _vNewInputSystem;
		private bool _isMoving;
		private bool _isFlying;
		private bool _isRotating;
		
		private string _inputSystemMessage = "";
		
		#endregion
		#region Inspector Validation

		#if UNITY_EDITOR
		public bool IsInputSystemEnabled {
			get {
				if (_vNewInputSystem) {
					#if ENABLE_INPUT_SYSTEM
					return true;
					#else
					inputSystemMessage = "New Input System is disabled";
					Debug.LogError(inputSystemMessage);
					return false;
					#endif
				}
				_inputSystemMessage = "New Input System is not installed";
				Debug.LogError(_inputSystemMessage);
				return false;
			}
		}
		
		#endif
		#endregion

		#region Event Functions

		/// <summary>
		/// This method is always called before any Start functions 
		/// </summary>
		private void Awake() => _mapManager = FindObjectOfType<MapManager>();

		/// <summary>
		/// This function is called just after the object is enabled
		/// </summary>
		private void OnEnable() {
			if (!canCapture) CaptureInput();
		}

		/// <summary>
		/// Unity calls when the script is loaded or a value changes in the Inspector.
		/// </summary>
		private void OnValidate() => _vNewInputSystem = AmiliousValidator.ValidatePackage("inputsystem");

		/// <summary>
		/// Start is called before the first frame update only if the script instance is enabled
		/// </summary>
		private void Start() => _mapManager.SetViewer(transform);

		/// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled
		/// Updating the behaviour of FlyCamera
		/// </summary>
		private void Update() {
			if (InCapture()) return;
			MovementHandler();
			RotationHandler();
		}


		/// <summary>
		/// This function is called when the behaviour becomes disabled
		/// </summary>
		private void OnDisable() => ReleaseInput();

		#endregion

		#region Private Methods

		/// <summary>
		/// When player is holding RightMouse
		/// </summary>
		private void CaptureInput() {
			_inputCaptured = true;
			transform.eulerAngles.GetXYValues(out _pitch,out _yaw);
		}


		/// <summary>
		/// This method is for if player has canceled holding MouserRight
		/// </summary>
		private void ReleaseInput() => _inputCaptured = false;

		/// <summary>
		/// called when the application loses or gains focus
		/// </summary>
		/// <param name="focus">if gameObjects have focus</param>
		private void OnApplicationFocus(bool focus) {
			if (_inputCaptured && !focus) ReleaseInput();
		}


		/// <summary>
		/// This function checks if user is in Capture
		/// </summary>
		/// <returns></returns>
		private bool InCapture() {
			if (!_inputCaptured) {
				// Check if player is rotating 
				if (canCapture && _isRotating) CaptureInput();
			}

			// check if player is holdingMouse or rotating
			switch (_inputCaptured) {
				case false:
				case true when !canCapture || !(_isRotating): return true;
				case true: ReleaseInput(); break;
			}
			return false;
		}


		/// <summary>
		/// This function is used for updating the rotation of the camera
		/// </summary>
		private void RotationHandler() {
			if (_mouseX == 0 && _mouseY == 0) return;
			
			// Apply speed to rotation
			var rotStrafe = _mouseX * Time.deltaTime;
			var rotFwd    = _mouseY * Time.deltaTime;
			
			transform.eulerAngles.GetXYValues(out _pitch, out _yaw);
			
			_yaw   = (_yaw + lookSpeed * rotStrafe) % 360f;
			_pitch = (_pitch - lookSpeed * rotFwd) % 360f;
			
			var rot = Quaternion.AngleAxis(_yaw, Vector3.up) * Quaternion.AngleAxis(_pitch, Vector3.right);

			Vector2 camAngle = rot.eulerAngles;
			camAngle.x = camAngle.x > 180 ? camAngle.x-360 : camAngle.x;
			camAngle.x = Mathf.Clamp(camAngle.x, rotationLimit.x, rotationLimit.y);
			
			transform.rotation = Quaternion.Euler(camAngle);
		}
		
		/// <summary>
		/// This function is used for updating the movement of the camera
		/// </summary>
		private void MovementHandler() {
			if (!_isMoving && !_isFlying) return;
			
			// check if sprinting
			var speed = Time.deltaTime * (_sprintInput > 0 ? sprintSpeed : moveSpeed);

			// get movementInput and speed
			var forward = speed * _moveInput.y;
			var right   = speed * _moveInput.x;
			var up      = speed * (_flyInput.y - _flyInput.x);

			// set movement
			transform.position += transform.forward * forward + transform.right * right + Vector3.up * up;
		}

		#endregion
		
		#region CameraActions
		
		/// <summary>
		/// This applies only to the new Input system
		/// Gets different values from the InputValue
		/// </summary>
		/// <param name="value">The values you get from the user input</param>
		public void OnMove(InputValue value) {
			_moveInput = value.Get<Vector2>();
			_isMoving  = _moveInput.x != 0 || _moveInput.y != 0;
		}

		public void OnFly(InputValue value) {
			_flyInput = value.Get<Vector2>();
			_isFlying = _flyInput.x != 0 || _flyInput.y != 0;
		}

		public void OnSprint(InputValue value) => _sprintInput = value.Get<float>();
		
		public void OnRotate(InputValue value) {
			_rotateInput = value.Get<float>();
			_isRotating  = _rotateInput > 0;
		}
		
		public void OnMouseX(InputValue value) => _mouseX = value.Get<float>();
		
		public void OnMouseY(InputValue value) => _mouseY = value.Get<float>();

		#endregion
	}
}