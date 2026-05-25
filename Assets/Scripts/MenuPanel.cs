using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace VRAimLab
{
    public class MenuPanel : MonoBehaviour
    {
        [Header("References")]
        public Canvas menuCanvas;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI modeText;
        public TextMeshProUGUI gunText;
        public TextMeshProUGUI sensText;
        public Button startButton;
        public Button stopButton;
        public Button modeLeftButton;
        public Button modeRightButton;
        public Button gunLeftButton;
        public Button gunRightButton;
        public Button sensLeftButton;
        public Button sensRightButton;
        public TextMeshProUGUI diffText;
        public Button diffLeftButton;
        public Button diffRightButton;

        private readonly float sensMin = 0.5f;
        private readonly float sensMax = 3.0f;
        private readonly float sensStep = 0.1f;

        private readonly string[] modeNames = { "5x5 Grid", "Moving Target", "Reaction Shot", "Tracking Shot" };
        private readonly string[] gunNames = { "Pistol", "AK47", "M4" };
        private readonly string[] diffNames = { "Standard", "Hard" };

        void Start()
        {
            SetupUI();
            UpdateDisplay();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStart += OnGameStarted;
                GameStateManager.Instance.OnGameStop += OnGameStopped;
            }
        }

        void SetupUI()
        {
            if (modeLeftButton != null)
                modeLeftButton.onClick.AddListener(() => ChangeMode(-1));
            if (modeRightButton != null)
                modeRightButton.onClick.AddListener(() => ChangeMode(1));
            if (gunLeftButton != null)
                gunLeftButton.onClick.AddListener(() => ChangeGun(-1));
            if (gunRightButton != null)
                gunRightButton.onClick.AddListener(() => ChangeGun(1));
            if (sensLeftButton != null)
                sensLeftButton.onClick.AddListener(() => ChangeSensitivity(-1));
            if (sensRightButton != null)
                sensRightButton.onClick.AddListener(() => ChangeSensitivity(1));
            if (diffLeftButton != null)
                diffLeftButton.onClick.AddListener(() => ChangeDifficulty(-1));
            if (diffRightButton != null)
                diffRightButton.onClick.AddListener(() => ChangeDifficulty(1));
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopClicked);
        }

        void ChangeMode(int dir)
        {
            int current = (int)GameStateManager.Instance.SelectedGameMode;
            int count = System.Enum.GetValues(typeof(GameModeType)).Length;
            int next = (current + dir + count) % count;
            GameStateManager.Instance.SelectGameMode((GameModeType)next);
            UpdateDisplay();
        }

        void ChangeGun(int dir)
        {
            int current = (int)GameStateManager.Instance.SelectedGun;
            int count = System.Enum.GetValues(typeof(GunType)).Length;
            int next = (current + dir + count) % count;
            GameStateManager.Instance.SelectGun((GunType)next);
            UpdateDisplay();
        }

        void ChangeSensitivity(int dir)
        {
            float current = GameStateManager.Instance.mouseSensitivity;
            current += dir * sensStep;
            current = Mathf.Round(current * 10f) / 10f;
            current = Mathf.Clamp(current, sensMin, sensMax);
            GameStateManager.Instance.mouseSensitivity = current;
            UpdateDisplay();
        }

        void ChangeDifficulty(int dir)
        {
            int current = (int)GameStateManager.Instance.SelectedDifficulty;
            int count = System.Enum.GetValues(typeof(Difficulty)).Length;
            int next = (current + dir + count) % count;
            GameStateManager.Instance.SelectDifficulty((Difficulty)next);
            UpdateDisplay();
        }

        void OnStartClicked()
        {
            GameStateManager.Instance.StartGame();
        }

        void OnStopClicked()
        {
            GameStateManager.Instance.ReturnToMenu();
        }

        void OnGameStarted()
        {
            // 面板保持可见，只更新按钮状态
            UpdateDisplay();
        }

        void OnGameStopped()
        {
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (modeText != null)
                modeText.text = $"Mode: {modeNames[(int)GameStateManager.Instance.SelectedGameMode]}";
            if (gunText != null)
                gunText.text = $"Gun: {gunNames[(int)GameStateManager.Instance.SelectedGun]}";

            if (startButton != null)
                startButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (stopButton != null)
                stopButton.gameObject.SetActive(GameStateManager.Instance.IsPlaying);
            if (modeLeftButton != null)
                modeLeftButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (modeRightButton != null)
                modeRightButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (gunLeftButton != null)
                gunLeftButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (gunRightButton != null)
                gunRightButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (sensLeftButton != null)
                sensLeftButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (sensRightButton != null)
                sensRightButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (diffLeftButton != null)
                diffLeftButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);
            if (diffRightButton != null)
                diffRightButton.gameObject.SetActive(!GameStateManager.Instance.IsPlaying);

            if (sensText != null)
                sensText.text = $"Sens: {GameStateManager.Instance.mouseSensitivity:F1}x";
            if (diffText != null)
                diffText.text = $"Diff: {diffNames[(int)GameStateManager.Instance.SelectedDifficulty]}";
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.OnGameStart -= OnGameStarted;
                GameStateManager.Instance.OnGameStop -= OnGameStopped;
            }
        }
    }
}
