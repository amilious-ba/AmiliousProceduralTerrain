using UnityEngine;

namespace Amilious.CameraControllers {
    public class SimpleEditorStyleCameraController : MonoBehaviour {

        public float panSpeed = 20f;
        public float rotSpeed = 10f;
        public float zoomSpeed = 50f;
        public float borderWidth = 10f;
        public bool edgeScrolling = true;
        public Camera cam;


        private float zoomMin = 11.0f;
        private float zoomMax = 49.0f;
        private float mouseX, mouseY;


        void Start() {
            cam = Camera.main;
        }


        void Update() {
            Movement();
            Rotation();
            Zoom();
        }


        void Movement() {
            Vector3 pos = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();
            if (Input.GetKey("w") || edgeScrolling == true && Input.mousePosition.y >= Screen.height - borderWidth) {
                pos += forward * panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("s") || edgeScrolling == true && Input.mousePosition.y <= borderWidth) {
                pos -= forward * panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("d") || edgeScrolling == true && Input.mousePosition.x >= Screen.width - borderWidth) {
                pos += right * panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("a") || edgeScrolling == true && Input.mousePosition.x <= borderWidth) {
                pos -= right * panSpeed * Time.deltaTime;
            }
            transform.position = pos;

        }

        void Rotation() {
            if (Input.GetMouseButton(1)) {
                mouseX += Input.GetAxis("Mouse X") * rotSpeed;
                mouseY -= Input.GetAxis("Mouse Y") * rotSpeed;
                mouseY = Mathf.Clamp(mouseY, -30, 45);
                transform.rotation = Quaternion.Euler(mouseY, mouseX, 0);
            }

        }


        void Zoom() {
            Vector3 camPos = cam.transform.position;
            float distance = Vector3.Distance(transform.position, cam.transform.position);
            if (Input.GetAxis("Mouse ScrollWheel") > 0f && distance > zoomMin) {
                camPos += cam.transform.forward * zoomSpeed * Time.deltaTime;
            }
            if (Input.GetAxis("Mouse ScrollWheel") < 0f && distance < zoomMax) {
                camPos -= cam.transform.forward * zoomSpeed * Time.deltaTime;
            }
            cam.transform.position = camPos;
        }

    }
}
