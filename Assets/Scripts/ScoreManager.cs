using UnityEngine;
using TMPro;
using System.Collections;

namespace VRAimLab
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance;

        [Header("UI References")]
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI hitsText;
        public TextMeshProUGUI shotsText;
        public TextMeshProUGUI accuracyText;
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI comboText;

        [Header("Result Panel")]
        public Transform resultPanelParent;
        public bool showResultPanel = true;

        private int score = 0;
        private int hits = 0;
        private int shots = 0;
        private int combo = 0;
        private int maxCombo = 0;
        private int lastCombo = 0;
        private float comboAnimTimer = 0f;
        private float startTime;
        private GameObject resultPanelObj;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            startTime = Time.time;
            UpdateUI();
        }

        void Update()
        {
            UpdateTime();
            UpdateComboAnim();
        }

        void UpdateComboAnim()
        {
            if (comboText == null) return;
            if (comboAnimTimer > 0)
            {
                comboAnimTimer -= Time.deltaTime;
                float t = 1f - Mathf.Clamp01(comboAnimTimer / 0.35f);
                // 弹性缩放：1.0 -> 1.45 -> 1.0
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.45f;
                comboText.transform.localScale = Vector3.one * scale;
            }
            else
            {
                comboText.transform.localScale = Vector3.one;
            }
        }

        public void ResetScore()
        {
            score = 0;
            hits = 0;
            shots = 0;
            combo = 0;
            maxCombo = 0;
            lastCombo = 0;
            comboAnimTimer = 0f;
            startTime = Time.time;
            HideResultPanel();
            UpdateUI();
        }

        public void RecordAndShowResult()
        {
            ShowResultPanel();
            if (RecordManager.Instance != null && GameStateManager.Instance != null)
            {
                RecordManager.Instance.AddRecord(
                    GameStateManager.Instance.SelectedGameMode.ToString(),
                    GameStateManager.Instance.SelectedDifficulty.ToString(),
                    score, hits, shots, maxCombo
                );
            }
            // 通知成绩墙刷新
            var chart = FindObjectOfType<ScoreChart>();
            chart?.RefreshChart();
        }

        public void AddHit()
        {
            combo++;
            if (combo > maxCombo) maxCombo = combo;
            hits++;
            score += combo * 100;
            UpdateUI();
        }

        public void AddShot(bool isHit)
        {
            shots++;
            if (!isHit && combo > 0)
            {
                combo = 0;
            }
            UpdateUI();
        }

        // 兼容旧调用（默认未命中不会断combo，因为旧逻辑在AddHit之前调用）
        public void AddShot()
        {
            shots++;
            UpdateUI();
        }

        void UpdateTime()
        {
            float elapsed = Time.time - startTime;
            int seconds = Mathf.FloorToInt(elapsed % 60f);

            if (timerText != null)
            {
                float remaining = Mathf.Max(0, 30f - elapsed);
                int remSec = Mathf.FloorToInt(remaining);
                timerText.text = $"{remSec}";
                timerText.color = remaining <= 5f ? Color.red : Color.white;
            }

            if (timeText != null)
            {
                int minutes = Mathf.FloorToInt(elapsed / 60f);
                timeText.text = $"<color=#555555>TIME</color>\n{minutes:00}:{seconds:00}";
            }
        }

        void UpdateUI()
        {
            float accuracy = shots > 0 ? (hits / (float)shots) * 100f : 0f;

            if (scoreText != null) scoreText.text = $"SCORE\n<size=140%>{score}</size>";
            if (hitsText != null) hitsText.text = $"<color=#555555>HIT</color>\n{hits}";
            if (shotsText != null) shotsText.text = $"<color=#555555>SHOT</color>\n{shots}";
            if (accuracyText != null) accuracyText.text = $"<color=#555555>ACC</color>\n{accuracy:F1}%";

            if (comboText != null)
            {
                if (combo >= 2)
                {
                    comboText.gameObject.SetActive(true);
                    string fire = combo >= 10 ? "*" : "";
                    string colorHex = combo >= 10 ? "FF2200" : (combo >= 5 ? "FF6600" : "FFAA00");
                    comboText.text = $"<color=#{colorHex}>{fire} COMBO {fire}</color>\n<size=160%>{combo}</size>";

                    // Combo 变化时触发放缩动画
                    if (combo != lastCombo)
                    {
                        comboAnimTimer = 0.35f;
                        lastCombo = combo;
                    }
                }
                else
                {
                    comboText.gameObject.SetActive(false);
                    lastCombo = 0;
                }
            }
        }

        public void ShowResultPanel()
        {
            if (!showResultPanel) return;
            HideResultPanel();

            // 查找正面的 ResultCanvas，没有则回退到原 parent
            GameObject resultCanvas = GameObject.Find("ResultCanvas");
            Transform parent = resultCanvas != null ? resultCanvas.transform : (resultPanelParent != null ? resultPanelParent : transform);
            resultPanelObj = new GameObject("ResultPanel");
            resultPanelObj.transform.SetParent(parent, false);

            // 背景
            UnityEngine.UI.Image bg = resultPanelObj.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
            RectTransform bgRT = resultPanelObj.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0.05f, 0.1f);
            bgRT.anchorMax = new Vector2(0.95f, 0.9f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // 标题
            CreateResultText("ResultTitle", "TIME'S UP", new Vector2(0, 0.72f), new Vector2(1, 0.95f), 40, new Color(1f, 0.6f, 0.2f));
            CreateResultText("ResultScore", $"Score: {score}", new Vector2(0, 0.48f), new Vector2(1, 0.68f), 32, Color.white);
            CreateResultText("ResultHits", $"Hits: {hits}", new Vector2(0, 0.30f), new Vector2(1, 0.46f), 26, Color.white);
            CreateResultText("ResultCombo", $"Max Combo: {maxCombo}", new Vector2(0, 0.14f), new Vector2(1, 0.28f), 26, new Color(1f, 0.4f, 0f));
            CreateResultText("ResultAcc", $"Accuracy: {(shots > 0 ? (hits / (float)shots * 100f) : 0f):F1}%", new Vector2(0, -0.02f), new Vector2(1, 0.12f), 24, new Color(0.8f, 0.8f, 0.8f));

            // 自动隐藏
            StartCoroutine(AutoHideResultPanel(5f));
        }

        void CreateResultText(string name, string text, Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(resultPanelObj.transform, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.font = scoreText != null ? scoreText.font : null;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        IEnumerator AutoHideResultPanel(float delay)
        {
            yield return new WaitForSeconds(delay);
            HideResultPanel();
        }

        public void HideResultPanel()
        {
            if (resultPanelObj != null)
            {
                Destroy(resultPanelObj);
                resultPanelObj = null;
            }
        }
    }
}
