using System.Text;
using Amilious.ProceduralTerrain.Map;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using Sirenix.OdinInspector;

namespace Amilious.ValueAdds
{
	public partial class FlyCamera : MonoBehaviour, ICameraActions
	{
		
		
		private static readonly string RootPath  = @"C:\Users\Abdikarim\FileSystem\manifest.json";
		private static          string _maniFest = File.ReadAllText(RootPath, Encoding.Default);


		
		private bool HasNewInputSystem() => _maniFest.Contains("inputsystem");


		#region Public Instance Variables
		
		[ValidateInput("HasNewInputSystem", "New Input System is not installed", InfoMessageType.Error)]
		public float lookSpeed   = 5f;
		public float moveSpeed   = 5f;
		public float sprintSpeed = 50f;
		
		public bool  enableInputCapture = true;
		public bool  lockAndHideCursor;
		public bool  holdRightMouseCapture = true;

		#endregion
		
		#region Private Instance Variables
		
		private MapManager mapManager;
		
		private Vector2    moveInput;
		private Vector2    flyInput;
		private Vector2    mousePosition;
		
		private float m_yaw;
		private float m_pitch;

		private float sprintInput;
		private float rotateInput;

		private float mouseX;
		private float mouseY;
		
		private bool  m_inputCaptured;
		private bool  hasReachedLimit;
		

		#endregion

		#region Event Functions
		
		/// <summary>
		/// This function is always called before any Start functions 
		/// </summary>
		private void Awake() => mapManager = FindObjectOfType<MapManager>();

		/// <summary>
		/// This function is called just after the object is enabled
		/// </summary>
		private void OnEnable()
		{
			if (enableInputCapture && !holdRightMouseCapture)
				CaptureInput();
		}
		
		/// <summary>
		/// Unity calls when the script is loaded or a value changes in the Inspector.
		/// </summary>
		private void OnValidate()
		{
			if (Application.isPlaying)
				enabled = enableInputCapture;
		}

		/// <summary>
		/// Start is called before the first frame update only if the script instance is enabled
		/// </summary>
		private void Start()
		{
			enabled = enableInputCapture;
			mapManager.SetViewer(transform);
		}
		
		/// <summary>
		/// Update is called every frame, if the MonoBehaviour is enabled
		/// Updating the behaviour of FlyCamera
		/// </summary>
		private void Update()
		{
			if (InCapture()) return;
			MovementHandler();
			RotationHandler();
		}
		

		/// <summary>
		/// This function is called when the behaviour becomes disabled
		/// </summary>
		private void OnDisable() => ReleaseInput();

		#endregion

		/// <summary>
		/// When player is holding RightMouse
		/// </summary>
		private void CaptureInput()
		{
			m_inputCaptured  = true;

			// Set the rotation of Camera
			var eulerAngles = transform.eulerAngles;
			m_yaw   = eulerAngles.y;
			m_pitch = eulerAngles.x;
		}

		/// <summary>
		/// This function is for if player has canceled holding MouserRight
		/// </summary>
		private void ReleaseInput() => m_inputCaptured  = false;

		/// <summary>
		/// called when the application loses or gains focus
		/// </summary>
		/// <param name="focus">if gameObjects have focus</param>
		private void OnApplicationFocus(bool focus)
		{
			if (m_inputCaptured && !focus)
				ReleaseInput();
		}
		

		/// <summary>
		/// This function checks if user is in Capture
		/// </summary>
		/// <returns></returns>
		private bool InCapture()
		{
			if (!m_inputCaptured)
			{
				// Check if player is rotating 
				if (holdRightMouseCapture && rotateInput > 0)
					CaptureInput();
			}

			// check if player is holdingMouse or rotating
			switch (m_inputCaptured)
			{
				case false:
				case true when !holdRightMouseCapture || !(rotateInput > 0): return true;
				case true: ReleaseInput(); break;
			}

			return false;
		}
		
		
		/// <summary>
		/// This function is used for updating the rotation of the camera
		/// </summary>
		private void RotationHandler()
		{
			// get the amount of rotation
			float rotStrafe = mouseX * Time.deltaTime;
			float rotFwd    = mouseY * Time.deltaTime;

			// Apply speed to rotation
			m_yaw   = (m_yaw + lookSpeed * rotStrafe) % 360f;
			m_pitch = (m_pitch - lookSpeed * rotFwd) % 360f;

			// Set the rotation in Quaternion
			transform.rotation = Quaternion.AngleAxis(m_yaw, Vector3.up) *
			                     Quaternion.AngleAxis(m_pitch, Vector3.right);
			
			// Set the "real" rotation of camera by eulerAngles
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
		}

		
		/// <summary>
		/// This function is used for updating the movement of the camera
		/// </summary>
		private void MovementHandler()
		{
			// check if sprinting
			float speed   = Time.deltaTime * (sprintInput > 0 ? sprintSpeed : moveSpeed);
			
			// get movementInput and speed
			float forward = speed * moveInput.y;
			float right   = speed * moveInput.x;
			float up      = speed * (flyInput.y - flyInput.x);
			
			// set movement
			transform.position += transform.forward * forward + transform.right * right + Vector3.up * up;
		}


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