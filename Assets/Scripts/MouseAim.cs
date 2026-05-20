using UnityEngine;

namespace VRAimLab
{
    public class MouseAim : MonoBehaviour
    {
        public float sensitivity = 1.5f;
        public float maxPitch = 40f;
        public float minPitch = -40f;

        private float yaw;
        private float pitch;
        private bool cursorLocked = false;

        void Start()
        {
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;
        }

        void Update()
        {
            // Only process mouse look when cursor is locked or not in editor
            if (Application.isEditor)
            {
                if (Input.GetMouseButtonDown(0) && !cursorLocked)
                {
                    LockCursor();
                }
                if (Input.GetKeyDown(KeyCode.Escape) && cursorLocked)
                {
                    UnlockCursor();
                }
            }
            else
            {
                if (!cursorLocked) LockCursor();
                if (Input.GetKeyDown(KeyCode.Escape)) UnlockCursor();
            }

            if (cursorLocked || !Application.isEditor)
            {
                yaw += Input.GetAxis("Mouse X") * sensitivity;
                pitch -= Input.GetAxis("Mouse Y") * sensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLocked = true;
        }

        void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorLocked = false;
        }

        void OnDisable()
        {
            UnlockCursor();
        }
    }
}
