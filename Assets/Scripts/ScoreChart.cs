using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

namespace VRAimLab
{
    public class ScoreChart : MonoBehaviour
    {
        [Header("Chart Settings")]
        public float pointRadius = 5f;
        public Color lineColor = new Color(0f, 0.85f, 0.95f);
        public Color pointColor = Color.white;
        public Color axisColor = new Color(0.7f, 0.7f, 0.7f, 0.9f);

        private Canvas chartCanvas;
        private UILineRenderer lineRenderer;
        private GameObject pointRoot;
        private GameObject axisRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI statText;

        void Start()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameModeChanged += OnModeChanged;
                GameStateManager.Instance.OnDifficultyChanged += OnDiffChanged;
            }
            RefreshChart();
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameModeChanged -= OnModeChanged;
                GameStateManager.Instance.OnDifficultyChanged -= OnDiffChanged;
            }
        }

        void OnModeChanged(GameModeType mode) => RefreshChart();
        void OnDiffChanged(Difficulty diff) => RefreshChart();

        public void RefreshChart()
        {
            if (RecordManager.Instance == null) return;

            string mode = GameStateManager.Instance.SelectedGameMode.ToString();
            string diff = GameStateManager.Instance.SelectedDifficulty.ToString();
            var records = RecordManager.Instance.GetRecords(mode, diff, 12);

            if (titleText != null)
            {
                string modeName = GetModeDisplayName(mode);
                titleText.text = $"{modeName}  [{diff}]";
            }

            if (records.Count == 0)
            {
                if (statText != null) statText.text = "No records yet - Play a round!";
                if (lineRenderer != null) lineRenderer.SetPoints(new Vector2[0]);
                ClearChildren(pointRoot);
                ClearChildren(axisRoot);
                return;
            }

            // 统计
            int maxScore = records.Max(r => r.score);
            float avgScore = (float)records.Average(r => r.score);
            int bestCombo = records.Max(r => r.maxCombo);
            if (statText != null)
                statText.text = $"Best: {maxScore}    Avg: {avgScore:F0}    Combo: {bestCombo}";

            // 获取折线区域的实际本地大小
            Vector2 drawSize = lineRenderer != null ? lineRenderer.GetComponent<RectTransform>().rect.size : new Vector2(300, 120);
            float padLeft = 40f;
            float padBottom = 30f;
            float padTop = 10f;
            float graphW = Mathf.Max(drawSize.x - padLeft, 50f);
            float graphH = Mathf.Max(drawSize.y - padBottom - padTop, 30f);

            // 构建折线点（在 lineRenderer 的本地空间）
            int count = records.Count;
            Vector2[] points = new Vector2[count];
            float maxVal = Mathf.Max(maxScore, 100f);
            float xStep = graphW / Mathf.Max(count - 1, 1);

            for (int i = 0; i < count; i++)
            {
                float x = padLeft + (count == 1 ? graphW * 0.5f : i * xStep);
                float y = padBottom + (records[i].score / maxVal) * graphH;
                points[i] = new Vector2(x, y);
            }

            if (lineRenderer != null)
                lineRenderer.SetPoints(points);

            ClearChildren(pointRoot);
            ClearChildren(axisRoot);
            DrawAxes(drawSize, padLeft, padBottom);
            DrawPoints(points, records, maxVal, padBottom);
        }

        string GetModeDisplayName(string mode)
        {
            switch (mode)
            {
                case "Grid5x5": return "5x5 Grid";
                case "MovingTarget": return "Moving Target";
                case "ReactionTarget": return "Reaction Shot";
                case "TrackingShot": return "Tracking Shot";
                default: return mode;
            }
        }

        void ClearChildren(GameObject root)
        {
            if (root == null) return;
            foreach (Transform c in root.transform)
                Destroy(c.gameObject);
        }

        void DrawAxes(Vector2 drawSize, float padLeft, float padBottom)
        {
            if (axisRoot == null) return;

            float axisThickness = 6f;
            float tickLen = 8f;
            float tickThick = 3f;

            // 底部横轴（延伸到最右边缘）
            CreateAxisLine(new Vector2(padLeft, padBottom), new Vector2(drawSize.x, padBottom), axisColor, axisThickness);
            // 左侧纵轴（延伸到最上边缘）
            CreateAxisLine(new Vector2(padLeft, padBottom), new Vector2(padLeft, drawSize.y), axisColor, axisThickness);

            // 横轴末端箭头（小三角示意）
            CreateAxisLine(new Vector2(drawSize.x, padBottom), new Vector2(drawSize.x - 10f, padBottom + 4f), axisColor, axisThickness);
            CreateAxisLine(new Vector2(drawSize.x, padBottom), new Vector2(drawSize.x - 10f, padBottom - 4f), axisColor, axisThickness);

            // 纵轴末端箭头
            CreateAxisLine(new Vector2(padLeft, drawSize.y), new Vector2(padLeft + 4f, drawSize.y - 10f), axisColor, axisThickness);
            CreateAxisLine(new Vector2(padLeft, drawSize.y), new Vector2(padLeft - 4f, drawSize.y - 10f), axisColor, axisThickness);

            // 刻度线（4个均匀分布）
            for (int i = 1; i <= 4; i++)
            {
                float tx = padLeft + (drawSize.x - padLeft) * i / 5f;
                CreateAxisLine(new Vector2(tx, padBottom), new Vector2(tx, padBottom + tickLen), axisColor, tickThick);

                float ty = padBottom + (drawSize.y - padBottom) * i / 5f;
                CreateAxisLine(new Vector2(padLeft, ty), new Vector2(padLeft + tickLen, ty), axisColor, tickThick);
            }
        }

        void CreateAxisLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            GameObject line = new GameObject("Axis");
            line.transform.SetParent(axisRoot.transform, false);
            RectTransform rt = line.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.anchoredPosition = (start + end) * 0.5f;
            float dx = end.x - start.x;
            float dy = end.y - start.y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            rt.sizeDelta = new Vector2(len, thickness);
            rt.localRotation = Quaternion.Euler(0, 0, angle);

            Image img = line.AddComponent<Image>();
            img.color = color;
        }

        void DrawPoints(Vector2[] points, List<GameRecord> records, float maxVal, float padBottom)
        {
            if (pointRoot == null) return;

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 pt = points[i];

                // 圆点
                GameObject dot = new GameObject("Dot");
                dot.transform.SetParent(pointRoot.transform, false);
                RectTransform rt = dot.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.anchoredPosition = pt;
                rt.sizeDelta = Vector2.one * pointRadius * 2f;

                Image img = dot.AddComponent<Image>();
                img.color = pointColor;
                img.sprite = GetCircleSprite();

                // 分数标签（点上方）
                GameObject label = new GameObject("Label");
                label.transform.SetParent(pointRoot.transform, false);
                RectTransform lrt = label.AddComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 0);
                lrt.anchorMax = new Vector2(0, 0);
                lrt.anchoredPosition = pt + new Vector2(0, 14f);
                lrt.sizeDelta = new Vector2(60f, 20f);

                TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
                tmp.text = records[i].score.ToString();
                tmp.fontSize = 12;
                tmp.color = new Color(0.85f, 0.85f, 0.85f);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enableWordWrapping = false;
            }
        }

        Sprite GetCircleSprite()
        {
            Texture2D tex = new Texture2D(16, 16);
            Color[] pixels = new Color[16 * 16];
            Vector2 center = new Vector2(7.5f, 7.5f);
            for (int py = 0; py < 16; py++)
            {
                for (int px = 0; px < 16; px++)
                {
                    float dist = Vector2.Distance(new Vector2(px, py), center);
                    pixels[py * 16 + px] = dist <= 7.5f ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        }

        public void Setup(Transform parentCanvas)
        {
            chartCanvas = parentCanvas.GetComponent<Canvas>();

            GameObject titleObj = GameObject.Find("ChartTitle");
            if (titleObj != null) titleText = titleObj.GetComponent<TextMeshProUGUI>();

            GameObject statObj = GameObject.Find("ChartStat");
            if (statObj != null) statText = statObj.GetComponent<TextMeshProUGUI>();

            GameObject lineObj = GameObject.Find("ChartLine");
            if (lineObj != null) lineRenderer = lineObj.GetComponent<UILineRenderer>();

            pointRoot = GameObject.Find("ChartPoints");
            axisRoot = GameObject.Find("ChartPoints"); // 坐标轴也画在点层

            RefreshChart();
        }
    }
}
