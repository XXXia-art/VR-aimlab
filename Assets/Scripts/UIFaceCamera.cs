using UnityEngine;

namespace VRAimLab
{
    public class UIFaceCamera : MonoBehaviour
    {
        public Transform cameraTransform;
        public float distance = 2.5f;
        public float height = 1.8f;
        public float horizontalOffset = 0f;
        public bool lockYRotation = false;

        void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main?.transform;
        }

        void Update()
        {
            if (cameraTransform == null) return;

            Vector3 targetPos = cameraTransform.position + cameraTransform.forward * distance + cameraTransform.right * horizontalOffset;
            targetPos.y = height;
            transform.position = targetPos;

            if (lockYRotation)
            {
                Vector3 dir = cameraTransform.position - transform.position;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(-dir);
            }
            else
            {
                transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
            }
        }
    }
}
