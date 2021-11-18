using Amilious.ProceduralTerrain.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using Sirenix.OdinInspector;

namespace Amilious.ValueAdds
{
	public class FlyCamera : MonoBehaviour, ICameraActions {
		
		#region Public Instance Variables
		
		// Check if Player has New Input System
		[ValidateInput("@IsInputSystemEnabled", "@inputSystemMessage", InfoMessageType.Error)]
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
		
		private MapManager mapManager;
		private Vector2    moveInput;
		private Vector2    flyInput;
		private Vector2    mousePosition;
		private float      yaw;
		private float      pitch;
		private float      sprintInput;
		private float      rotateInput;
		private float      mouseX;
		private float      mouseY;
		private bool       inputCaptured;
		private bool       limitReached;
		private bool       v_NewInputSystem;
		private string     inputSystemMessage = "";
		
		#endregion

		#region Validations

		public bool IsInputSystemEnabled {
			get {
				if (v_NewInputSystem) {
#if ENABLE_INPUT_SYSTEM
					return true;
#else
					inputSystemMessage = "New Input System is disabled";
					Debug.LogError(inputSystemMessage);
					return false;
#endif
				}
				inputSystemMessage = "New Input System is not installed";
				Debug.LogError(inputSystemMessage);
				return false;
			}
		}

		#endregion

		#region Event Functions

		/// <summary>
		/// This method is always called before any Start functions 
		/// </summary>
		private void Awake() => mapManager = FindObjectOfType<MapManager>();

		/// <summary>
		/// This function is called just after the object is enabled
		/// </summary>
		private void OnEnable() {
			if (!canCapture) CaptureInput();
		}

		/// <summary>
		/// Unity calls when the script is loaded or a value changes in the Inspector.
		/// </summary>
		private void OnValidate() => v_NewInputSystem = AmiliousValidator.ValidatePackage("inputsystem");

		/// <summary>
		/// Start is called before the first frame update only if the script instance is enabled
		/// </summary>
		private void Start() => mapManager.SetViewer(transform);

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
			inputCaptured = true;

			// Set the rotation of Camera
			var eulerAngles = transform.eulerAngles;
			yaw   = eulerAngles.y;
			pitch = eulerAngles.x;
		}


		/// <summary>
		/// This method is for if player has canceled holding MouserRight
		/// </summary>
		private void ReleaseInput() => inputCaptured = false;

		/// <summary>
		/// called when the application loses or gains focus
		/// </summary>
		/// <param name="focus">if gameObjects have focus</param>
		private void OnApplicationFocus(bool focus) {
			if (inputCaptured && !focus) ReleaseInput();
		}


		/// <summary>
		/// This function checks if user is in Capture
		/// </summary>
		/// <returns></returns>
		private bool InCapture() {
			if (!inputCaptured) {
				// Check if player is rotating 
				if (canCapture && rotateInput > 0)
					CaptureInput();
			}

			// check if player is holdingMouse or rotating
			switch (inputCaptured) {
				case false:
				case true when !canCapture || !(rotateInput > 0): return true;
				case true:
					ReleaseInput();
					break;
			}

			return false;
		}


		/// <summary>
		/// This function is used for updating the rotation of the camera
		/// </summary>
		private void RotationHandler() {
			
			// Apply speed to rotation
			float rotStrafe = mouseX * Time.deltaTime;
			float rotFwd    = mouseY * Time.deltaTime;
			
			// Apply speed to rotation
			yaw   = (yaw + lookSpeed * rotStrafe) % 360f;
			pitch = (pitch - lookSpeed * rotFwd) % 360f;

			Quaternion rot = Quaternion.AngleAxis(yaw,Vector3.up) * Quaternion.AngleAxis(pitch,Vector3.right);

			Vector3 camAngle = rot.eulerAngles;
			camAngle.x = camAngle.x > 180 ? camAngle.x-360 : camAngle.x;
			camAngle.x = Mathf.Clamp(camAngle.x, rotationLimit.x, rotationLimit.y);
			
			transform.rotation = Quaternion.Euler(camAngle);
		}


		/// <summary>
		/// This function is used for updating the movement of the camera
		/// </summary>
		private void MovementHandler() {
			// check if sprinting
			float speed = Time.deltaTime * (sprintInput > 0 ? sprintSpeed : moveSpeed);

			// get movementInput and speed
			float forward = speed * moveInput.y;
			float right   = speed * moveInput.x;
			float up      = speed * (flyInput.y - flyInput.x);

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
		public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
		public void OnFly(InputValue value) => flyInput = value.Get<Vector2>();
		public void OnSprint(InputValue value) => sprintInput = value.Get<float>();
		public void OnRotate(InputValue value) => rotateInput = value.Get<float>();
		public void OnMouseX(InputValue value) => mouseX = value.Get<float>();
		public void OnMouseY(InputValue value) => mouseY = value.Get<float>();

		#endregion
	}
}