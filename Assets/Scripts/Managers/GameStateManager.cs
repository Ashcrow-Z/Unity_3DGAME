using System;
using UnityEngine;

namespace SipLab
{
    public enum GameState
    {
        Start,
        Playing,
        Paused,
        Victory
    }

    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        [Header("Runtime State")]
        public GameState State = GameState.Start;
        public bool IsPowerOn;
        public bool HasEnergyCore;
        public bool HasKeycard;

        [Header("Timer")]
        public float RunTime;

        public event Action<GameState> OnStateChanged;
        public event Action OnPowerRestored;
        public event Action OnEnergyCoreObtained;
        public event Action OnKeycardObtained;
        public event Action OnVictory;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            if (State == GameState.Playing)
                RunTime += Time.unscaledDeltaTime;
        }

        public void SetState(GameState newState)
        {
            if (State == newState) return;
            State = newState;
            switch (newState)
            {
                case GameState.Start:
                case GameState.Paused:
                case GameState.Victory:
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    break;
                case GameState.Playing:
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    break;
            }
            OnStateChanged?.Invoke(newState);
        }

        public void StartGame()
        {
            RunTime = 0f;
            IsPowerOn = false;
            HasEnergyCore = false;
            HasKeycard = false;
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void PickupEnergyCore()
        {
            HasEnergyCore = true;
            OnEnergyCoreObtained?.Invoke();
        }

        public void RestorePower()
        {
            if (IsPowerOn) return;
            IsPowerOn = true;
            HasEnergyCore = false;
            OnPowerRestored?.Invoke();
        }

        public void PickupKeycard()
        {
            HasKeycard = true;
            OnKeycardObtained?.Invoke();
        }

        public void TriggerVictory()
        {
            if (State == GameState.Victory) return;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayVictory();
            SetState(GameState.Victory);
            OnVictory?.Invoke();
        }

        public string FormattedTime
        {
            get
            {
                int total = Mathf.FloorToInt(RunTime);
                int m = total / 60;
                int s = total % 60;
                int ms = Mathf.FloorToInt((RunTime - total) * 100);
                return string.Format("{0:00}:{1:00}.{2:00}", m, s, ms);
            }
        }
    }
}
