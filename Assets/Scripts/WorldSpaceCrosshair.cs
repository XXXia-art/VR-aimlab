using UnityEngine;
using UnityEngine.UI;

namespace VRAimLab
{
    public class WorldSpaceCrosshair : MonoBehaviour
    {
        [Header("Aim")]
        public Transform muzzleTransform;
        public LayerMask targetLayer;
        public float defaultDistance = 5f;
        public float maxRayDistance = 100f;

        [Header("Visual")]
        public Color aimColor = new Color(0.2f, 1f, 0.2f, 0.9f);
        public Color lockColor = new Color(1f, 0.3f, 0.3f, 0.95f);

        private Image crosshairImage;

        void Start()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;

            RectTransform canvasRT = GetComponent<RectTransform>();
            if (canvasRT == null)
                canvasRT = gameObject.AddComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(100, 100);

            GameObject dotObj = new GameObject("CrosshairDot");
            dotObj.transform.SetParent(transform, false);

            RectTransform dotRect = dotObj.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(24, 24);

            crosshairImage = dotObj.AddComponent<Image>();
            crosshairImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob");
            crosshairImage.color = aimColor;
            crosshairImage.enabled = false; // 关闭视觉准星，仅保留射线
        }

        void Update()
        {
            if (muzzleTransform == null)
            {
                GameObject rightHand = GameObject.Find("RightHand");
                if (rightHand != null)
                {
                    VRGun gun = rightHand.GetComponent<VRGun>();
                    if (gun != null) muzzleTransform = gun.muzzleTransform;
                }
                if (muzzleTransform == null) return;
            }

            Vector3 origin = muzzleTransform.position;
            Vector3 direction = muzzleTransform.forward;

            RaycastHit hit;
            float distance = defaultDistance;
            bool hitTarget = Physics.Raycast(origin, direction, out hit, maxRayDistance, targetLayer, QueryTriggerInteraction.Collide);
            if (hitTarget)
            {
                distance = hit.distance;
                crosshairImage.color = lockColor;
            }
            else
            {
                crosshairImage.color = aimColor;
            }

            transform.position = origin + direction * distance;
            transform.rotation = Quaternion.LookRotation(direction);

            float scale = distance * 0.003f;
            transform.localScale = Vector3.one * Mathf.Max(scale, 0.001f);
        }
    }
}
