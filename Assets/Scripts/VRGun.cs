using UnityEngine;
using UnityEngine.XR;

namespace VRAimLab
{
    public class VRGun : MonoBehaviour
    {
        [Header("Ray Settings")]
        public Transform muzzleTransform;
        public float maxRayDistance = 100f;
        public LayerMask targetLayer;

        [Header("Visual")]
        public LineRenderer laserLine;
        public Color aimColor = Color.green;
        public Color targetLockedColor = Color.red;
        public float laserWidth = 0.04f;
        public Material laserMaterial;

        [Header("Input")]
        public XRNode controllerNode = XRNode.RightHand;
        public bool useMouseDebug = true;

        private bool wasPressed = false;
        private InputDevice device;
        private Camera mainCam;

        void Start()
        {
            if (muzzleTransform == null)
                muzzleTransform = transform;

            mainCam = Camera.main;
            SetupLaserLine();
            device = InputDevices.GetDeviceAtXRNode(controllerNode);
        }

        void SetupLaserLine()
        {
            if (laserLine == null)
            {
                GameObject lineObj = new GameObject("LaserLine");
                lineObj.transform.SetParent(transform);
                laserLine = lineObj.AddComponent<LineRenderer>();
            }

            laserLine.positionCount = 2;
            laserLine.useWorldSpace = true;
            laserLine.sortingOrder = 100;
            laserLine.numCapVertices = 4;

            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, laserWidth);
            curve.AddKey(1f, laserWidth * 0.3f);
            laserLine.widthCurve = curve;
            laserLine.widthMultiplier = 1f;

            if (laserMaterial == null)
            {
                laserMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            laserMaterial.color = aimColor;
            laserLine.material = laserMaterial;
        }

        void Update()
        {
            device = InputDevices.GetDeviceAtXRNode(controllerNode);
            UpdateLaser();
            HandleShooting();
        }

        void UpdateLaser()
        {
            Vector3 origin;
            Vector3 direction;

            if (useMouseDebug && mainCam != null)
            {
                origin = mainCam.transform.position;
                direction = mainCam.transform.forward;
            }
            else
            {
                origin = muzzleTransform.position;
                direction = muzzleTransform.forward;
            }

            RaycastHit hit;
            bool hitTarget = Physics.Raycast(origin, direction, out hit, maxRayDistance, targetLayer);

            Vector3 endPoint = hitTarget ? hit.point : origin + direction * maxRayDistance;

            laserLine.SetPosition(0, origin);
            laserLine.SetPosition(1, endPoint);

            Color currentColor = hitTarget ? targetLockedColor : aimColor;
            laserMaterial.color = currentColor;
        }

        void HandleShooting()
        {
            bool isPressed = false;

            if (useMouseDebug)
            {
                isPressed = Input.GetMouseButtonDown(0);
            }
            else if (device.isValid)
            {
                device.TryGetFeatureValue(CommonUsages.triggerButton, out isPressed);
            }

            if (isPressed && !wasPressed)
            {
                Shoot();
            }
            wasPressed = isPressed;
        }

        void Shoot()
        {
            Vector3 origin;
            Vector3 direction;

            if (useMouseDebug && mainCam != null)
            {
                origin = mainCam.transform.position;
                direction = mainCam.transform.forward;
            }
            else
            {
                origin = muzzleTransform.position;
                direction = muzzleTransform.forward;
            }

            ScoreManager.Instance?.AddShot();

            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, maxRayDistance, targetLayer))
            {
                Target target = hit.collider.GetComponent<Target>();
                if (target != null)
                {
                    target.Hit();
                }
                else
                {
                    target = hit.collider.GetComponentInParent<Target>();
                    if (target != null)
                        target.Hit();
                }
            }
        }

        void OnDestroy()
        {
            if (laserMaterial != null)
                Destroy(laserMaterial);
        }
    }
}
