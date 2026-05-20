using UnityEngine;
using UnityEngine.XR;

namespace VRAimLab
{
    public class XRCameraTracker : MonoBehaviour
    {
        public Vector3 defaultPosition = new Vector3(0, 1.6f, 0f);
        public Vector3 defaultEuler = Vector3.zero;

        void Update()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (device.isValid)
            {
                device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);
                transform.localPosition = pos;
                transform.localRotation = rot;
            }
            else
            {
                transform.localPosition = defaultPosition;
                transform.localRotation = Quaternion.Euler(defaultEuler);
            }
        }
    }
}
