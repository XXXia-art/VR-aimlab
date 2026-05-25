using System;
using UnityEngine;

namespace VRAimLab
{
    public enum GameModeType { Grid5x5, MovingTarget, ReactionTarget, TrackingShot }
    public enum GunType { Pistol, AK47, M4 }
    public enum Difficulty { Standard, Hard }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance;

        [Header("Current Selection")]
        public GameModeType SelectedGameMode = GameModeType.Grid5x5;
        public GunType SelectedGun = GunType.Pistol;
        public Difficulty SelectedDifficulty = Difficulty.Standard;

        [Header("Settings")]
        public float mouseSensitivity = 1.5f;

        [Header("State")]
        public bool IsInMenu = true;
        public bool IsPlaying = false;

        public event Action OnGameStart;
        public event Action OnGameStop;
        public event Action<GameModeType> OnGameModeChanged;
        public event Action<GunType> OnGunChanged;
        public event Action<float> OnSensitivityChanged;
        public event Action<Difficulty> OnDifficultyChanged;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void SelectGameMode(GameModeType mode)
        {
            SelectedGameMode = mode;
            OnGameModeChanged?.Invoke(mode);
            Debug.Log($"[GameState] 选择游戏模式: {mode}");
        }

        public void SelectGun(GunType gun)
        {
            SelectedGun = gun;
            OnGunChanged?.Invoke(gun);
            Debug.Log($"[GameState] 选择枪械: {gun}");
        }

        public void SelectDifficulty(Difficulty diff)
        {
            SelectedDifficulty = diff;
            OnDifficultyChanged?.Invoke(diff);
            Debug.Log($"[GameState] 选择难度: {diff}");
        }

        public void StartGame()
        {
            IsInMenu = false;
            IsPlaying = true;
            OnGameStart?.Invoke();
            Debug.Log("[GameState] 游戏开始");
        }

        public void StopGame()
        {
            IsPlaying = false;
            IsInMenu = true;
            OnGameStop?.Invoke();
            Debug.Log("[GameState] 游戏停止");
        }

        public void ReturnToMenu()
        {
            StopGame();
        }
    }
}
