using UnityEngine;
using UnityEngine.XR;

namespace VRAimLab
{
    public class XRControllerTracker : MonoBehaviour
    {
        public XRNode node = XRNode.RightHand;
        public Vector3 defaultPosition = new Vector3(0.2f, 1.3f, 0.2f);
        public Vector3 defaultEuler = new Vector3(0, 0, 0);

        void Update()
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
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
