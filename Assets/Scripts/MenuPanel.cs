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
        public Button startButton;
        public Button stopButton;
        public Button modeLeftButton;
        public Button modeRightButton;
        public Button gunLeftButton;
        public Button gunRightButton;

        private readonly string[] modeNames = { "5x5 网格射击", "移动靶射击" };
        private readonly string[] gunNames = { "手枪", "AK47" };

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
            if (menuCanvas != null) menuCanvas.enabled = false;
        }

        void OnGameStopped()
        {
            if (menuCanvas != null) menuCanvas.enabled = true;
            UpdateDisplay();
        }

        void UpdateDisplay()
        {
            if (modeText != null)
                modeText.text = $"模式: {modeNames[(int)GameStateManager.Instance.SelectedGameMode]}";
            if (gunText != null)
                gunText.text = $"枪械: {gunNames[(int)GameStateManager.Instance.SelectedGun]}";

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
