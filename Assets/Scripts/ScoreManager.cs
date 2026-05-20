using UnityEngine;
using TMPro;

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

        private int score = 0;
        private int hits = 0;
        private int shots = 0;
        private float startTime;

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
        }

        public void AddHit()
        {
            hits++;
            score += 100;
            UpdateUI();
        }

        public void AddShot()
        {
            shots++;
            UpdateUI();
        }

        void UpdateTime()
        {
            if (timeText == null) return;
            float elapsed = Time.time - startTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        void UpdateUI()
        {
            float accuracy = shots > 0 ? (hits / (float)shots) * 100f : 0f;

            if (scoreText != null) scoreText.text = $"Score: {score}";
            if (hitsText != null) hitsText.text = $"Hits: {hits}";
            if (shotsText != null) shotsText.text = $"Shots: {shots}";
            if (accuracyText != null) accuracyText.text = $"Accuracy: {accuracy:F1}%";
        }
    }
}
