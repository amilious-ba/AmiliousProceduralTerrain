using System;
using Packages.Rider.Editor.UnitTesting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Amilious.ValueAdds
{
    public class FlyCamera : MonoBehaviour, ICameraActions
    {
        public bool enableInputCapture = true;
        public bool lockAndHideCursor;
        public bool holdRightMouseCapture = true;

        public float lookSpeed   = 5f;
        public float moveSpeed   = 5f;
        public float sprintSpeed = 50f;
        public float m_cameraRoll;

        private bool m_inputCaptured;
        private float m_yaw;
        private float m_pitch;

        private Vector2 moveInput;
        private Vector2 flyInput;
        private Vector2 mousePosition;
        
        private float   sprintInput;
        private float   rotateInput;

        private float mouseX;
        private float mouseY;
        
        private bool  hasReachedLimit;
        private void Awake() => enabled = enableInputCapture;

        private void OnEnable()
        {
            if (enableInputCapture && !holdRightMouseCapture)
                CaptureInput();
        }

        private void OnDisable() => ReleaseInput();

        private void OnValidate()
        {
            if (Application.isPlaying)
                enabled = enableInputCapture;
        }

        private void CaptureInput()
        {
            if (lockAndHideCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            m_inputCaptured = true;

            var eulerAngles = transform.eulerAngles;
            m_yaw = eulerAngles.y;
            m_pitch = eulerAngles.x;
        }

        private void ReleaseInput()
        {
            if (lockAndHideCursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            m_inputCaptured = false;
        }

        private void OnApplicationFocus(bool focus)
        {
            if (m_inputCaptured && !focus)
                ReleaseInput();
        }

        private void Update()
        {
            if (!m_inputCaptured)
            {
                if (holdRightMouseCapture && rotateInput > 0)
                    CaptureInput();
            }
            
            switch (m_inputCaptured)
            {
                case false:
                case true when !holdRightMouseCapture || !(rotateInput > 0): return;
                case true: ReleaseInput(); break;
            }

            float rotStrafe = mouseX * Time.deltaTime;
            float rotFwd    = mouseY * Time.deltaTime;

            m_yaw = (m_yaw + lookSpeed * rotStrafe) % 360f;
            m_pitch = (m_pitch - lookSpeed * rotFwd) % 360f;

            float speed   = Time.deltaTime * (sprintInput > 0 ? sprintSpeed : moveSpeed); 
            float forward = speed * moveInput.y;
            float right   = speed * moveInput.x;
            float up      = speed * (flyInput.y - flyInput.x);
            
            transform.position += transform.forward * forward + transform.right * right + Vector3.up * up;
            transform.rotation = Quaternion.AngleAxis(m_yaw, Vector3.up) * Quaternion.AngleAxis(m_pitch, Vector3.right);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, m_cameraRoll);
        }

        public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

        public void OnFly(InputValue value) => flyInput = value.Get<Vector2>();

        public void OnSprint(InputValue value) => sprintInput = value.Get<float>();

        public void OnRotate(InputValue value) => rotateInput = value.Get<float>();

        public void OnMouseX(InputValue value) => mouseX = value.Get<float>();

        public void OnMouseY(InputValue value) => mouseY = value.Get<float>();
    }
}