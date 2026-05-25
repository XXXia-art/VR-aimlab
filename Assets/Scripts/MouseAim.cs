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
            // VR 设备上跳过鼠标瞄准
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
                return;

#if ENABLE_LEGACY_INPUT_MANAGER
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
                float sens = GameStateManager.Instance != null ? GameStateManager.Instance.mouseSensitivity : sensitivity;
                yaw += Input.GetAxis("Mouse X") * sens;
                pitch -= Input.GetAxis("Mouse Y") * sens;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
#endif
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
