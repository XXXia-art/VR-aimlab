using UnityEngine;
using UnityEngine.UI;

namespace VRAimLab
{
    public class ScreenCrosshair : MonoBehaviour
    {
        public Color aimColor = new Color(0.2f, 1f, 0.2f, 0.9f);
        public Color lockColor = new Color(1f, 0.3f, 0.3f, 0.95f);
        public float size = 10f;
        public float thickness = 3f;
        public float gap = 5f;

        private Image top, bottom, left, right;

        void Start()
        {
            CreateCrosshair();
        }

        void CreateCrosshair()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
            }

            top = CreateLine("CrosshairTop", new Vector2(thickness, size));
            bottom = CreateLine("CrosshairBottom", new Vector2(thickness, size));
            left = CreateLine("CrosshairLeft", new Vector2(size, thickness));
            right = CreateLine("CrosshairRight", new Vector2(size, thickness));

            UpdatePositions();
        }

        Image CreateLine(string name, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = aimColor;
            return img;
        }

        void UpdatePositions()
        {
            if (top == null) return;
            top.rectTransform.anchoredPosition = new Vector2(0, gap + size * 0.5f);
            bottom.rectTransform.anchoredPosition = new Vector2(0, -(gap + size * 0.5f));
            left.rectTransform.anchoredPosition = new Vector2(-(gap + size * 0.5f), 0);
            right.rectTransform.anchoredPosition = new Vector2(gap + size * 0.5f, 0);
        }

        public void SetColor(Color color)
        {
            if (top != null) top.color = color;
            if (bottom != null) bottom.color = color;
            if (left != null) left.color = color;
            if (right != null) right.color = color;
        }

        void Update()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 origin = cam.transform.position;
            Vector3 direction = cam.transform.forward;
            int layer = targetLayerMask();
            bool hitTarget = Physics.Raycast(origin, direction, out _, 100f, layer);
            SetColor(hitTarget ? lockColor : aimColor);
        }

        int targetLayerMask()
        {
            return 1 << 0; // Default layer
        }
    }
}
