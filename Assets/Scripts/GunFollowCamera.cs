using UnityEngine;

namespace VRAimLab
{
    public class GunFollowCamera : MonoBehaviour
    {
        public Camera targetCamera;
        public Vector3 offsetPosition = new Vector3(0.12f, -0.1f, 0.55f);
        public float swayAmount = 0.02f;
        public float swaySpeed = 3f;

        void LateUpdate()
        {
            // VR 设备上让 XRControllerTracker 控制枪械位置，不跟随相机
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
                return;

            if (targetCamera == null) return;

            Vector3 basePos = targetCamera.transform.position + targetCamera.transform.rotation * offsetPosition;

            // Add subtle breathing sway
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmount;
            float swayY = Mathf.Cos(Time.time * swaySpeed * 0.7f) * swayAmount;
            basePos += targetCamera.transform.right * swayX;
            basePos += targetCamera.transform.up * swayY;

            transform.position = basePos;
            transform.rotation = targetCamera.transform.rotation;
        }
    }        
}



