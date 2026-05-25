using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

namespace VRAimLab
{
    /// <summary>
    /// 从屏幕中心发射射线与 WorldSpace Canvas 上的按钮交互。
    /// 解决编辑器中鼠标锁定后无法点击 WorldSpace UI 的问题。
    /// 用法：挂载到 Main Camera 上，按 E 键或鼠标左键触发面前的按钮。
    /// </summary>
    public class WorldSpaceUIInteractor : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("射线最大检测距离")]
        public float maxDistance = 8f;
        [Tooltip("是否显示中心准星")]
        public bool showCenterDot = true;

        Camera mainCam;
        Canvas menuCanvas;
        Button hoveredButton;
        Dictionary<Button, Color> defaultColors = new Dictionary<Button, Color>();
        private bool wasTriggerPressed = false;

        void Start()
        {
            mainCam = Camera.main;
        }

        Transform GetRayOrigin()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
            {
                GameObject rightHand = GameObject.Find("RightHand");
                if (rightHand != null) return rightHand.transform;
            }
            return mainCam != null ? mainCam.transform : null;
        }

        bool GetTriggerDown()
        {
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
            {
                var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (device.isValid)
                {
                    device.TryGetFeatureValue(CommonUsages.triggerButton, out bool pressed);
                    bool justPressed = pressed && !wasTriggerPressed;
                    wasTriggerPressed = pressed;
                    return justPressed;
                }
                wasTriggerPressed = false;
                return false;
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        void Update()
        {
            if (mainCam == null) return;

            // 寻找 MenuCanvas
            if (menuCanvas == null)
            {
                GameObject menuObj = GameObject.Find("MenuCanvas");
                if (menuObj != null) menuCanvas = menuObj.GetComponent<Canvas>();
            }

            // 确定射线来源（VR 下用手柄，非 VR 用相机）
            Transform rayOrigin = GetRayOrigin();
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            Button hitButton = null;

            if (menuCanvas != null && menuCanvas.renderMode == RenderMode.WorldSpace)
            {
                // 计算射线与 Canvas 平面的交点
                if (RayPlaneIntersect(ray, menuCanvas.transform.position, menuCanvas.transform.forward, out Vector3 worldHit))
                {
                    // 检查射线是否从 Canvas 正面射来（避免从背面穿过）
                    float faceDot = Vector3.Dot(ray.direction, menuCanvas.transform.forward);
                    if (faceDot > 0.01f) // 射线方向与面板法线同向，即从正面射来
                    {
                        // 转换为 Canvas 本地坐标
                        Vector3 localPoint = menuCanvas.transform.InverseTransformPoint(worldHit);
                        // Canvas 本地坐标中，RectTransform 的原点在中心，范围是 sizeDelta/2
                        Vector2 canvasSize = menuCanvas.GetComponent<RectTransform>().sizeDelta;
                        // 本地坐标转换为以左下角为原点的 UI 坐标（anchor 坐标系）
                        Vector2 anchorPos = new Vector2(
                            (localPoint.x / canvasSize.x) + 0.5f,
                            (localPoint.y / canvasSize.y) + 0.5f
                        );

                        // 检测哪个按钮包含该坐标
                        hitButton = FindButtonAtAnchor(anchorPos);
                    }
                }
            }

            UpdateHover(hitButton);

            // 触发点击
            if (GetTriggerDown() && hoveredButton != null)
            {
                hoveredButton.onClick.Invoke();
            }
        }

        /// <summary>
        /// 计算射线与平面的交点
        /// </summary>
        bool RayPlaneIntersect(Ray ray, Vector3 planePos, Vector3 planeNormal, out Vector3 hitPoint)
        {
            hitPoint = Vector3.zero;
            float denom = Vector3.Dot(planeNormal, ray.direction);
            if (Mathf.Abs(denom) < 0.0001f) return false;
            float t = Vector3.Dot(planePos - ray.origin, planeNormal) / denom;
            if (t < 0 || t > maxDistance) return false;
            hitPoint = ray.GetPoint(t);
            return true;
        }

        /// <summary>
        /// 查找包含指定 anchor 坐标的按钮
        /// </summary>
        Button FindButtonAtAnchor(Vector2 anchorPos)
        {
            // 遍历 MenuCanvas 下的所有 Button
            Button[] buttons = menuCanvas.GetComponentsInChildren<Button>();
            foreach (var btn in buttons)
            {
                if (!btn.gameObject.activeInHierarchy) continue;

                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt == null) continue;

                if (anchorPos.x >= rt.anchorMin.x && anchorPos.x <= rt.anchorMax.x &&
                    anchorPos.y >= rt.anchorMin.y && anchorPos.y <= rt.anchorMax.y)
                {
                    return btn;
                }
            }
            return null;
        }

        void UpdateHover(Button btn)
        {
            if (hoveredButton == btn) return;

            // 恢复上一个按钮的颜色
            if (hoveredButton != null && defaultColors.TryGetValue(hoveredButton, out Color defaultColor))
            {
                Image img = hoveredButton.GetComponent<Image>();
                if (img != null) img.color = defaultColor;
            }

            hoveredButton = btn;

            // 高亮新按钮
            if (hoveredButton != null)
            {
                Image img = hoveredButton.GetComponent<Image>();
                if (img != null)
                {
                    if (!defaultColors.ContainsKey(hoveredButton))
                        defaultColors[hoveredButton] = img.color;
                    img.color = Color.Lerp(img.color, Color.white, 0.4f);
                }
            }
        }

        void OnGUI()
        {
            if (!showCenterDot) return;

            // VR 模式下不绘制屏幕中心点
            if (!Application.isEditor && Application.platform == RuntimePlatform.Android)
                return;

            // 屏幕中心小点
            float size = hoveredButton != null ? 10f : 6f;
            Color dotColor = hoveredButton != null ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 1f, 1f, 0.5f);
            GUI.color = dotColor;
            GUI.DrawTexture(new Rect(Screen.width / 2 - size / 2, Screen.height / 2 - size / 2, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // 悬停提示
            if (hoveredButton != null)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 14;
                style.normal.textColor = Color.green;
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 20, 200, 24), "[Click or E] " + hoveredButton.name, style);
            }
        }
    }
}
