using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonSkySurvivor
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private const string HighScoreKey = "SkySurvivorHighScore";
        private const string LegacyHighScoreKey = "NeonSkyHighScore";

        [Header("Scene References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private GameUI gameUI;

        [Header("Resources")]
        [SerializeField, Min(1f)] private float maxEnergy = 100f;
        [SerializeField, Range(0f, 1f)] private float startingEnergyPercent = 0.5f;
        [SerializeField, Min(0f)] private float comboDuration = 3f;

        private static bool startImmediatelyAfterReload;
        private float elapsedTime;
        private float energy;
        private float comboTimer;
        private int score;
        private int combo = 1;
        private int highScore;

        public GameState State { get; private set; } = GameState.Menu;
        public bool IsPlaying => State == GameState.Playing;
        public float ElapsedTime => elapsedTime;
        public float Energy => energy;
        public float MaxEnergy => maxEnergy;
        public int Score => score;
        public int Combo => combo;
        public float DifficultyMultiplier => 1f + (elapsedTime / 70f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Time.timeScale = 1f;
            highScore = PlayerPrefs.GetInt(HighScoreKey, PlayerPrefs.GetInt(LegacyHighScoreKey, 0));
        }

        private void Start()
        {
            ShowMenu();

            if (startImmediatelyAfterReload)
            {
                startImmediatelyAfterReload = false;
                StartCoroutine(StartOnNextFrame());
            }
        }

        private IEnumerator StartOnNextFrame()
        {
            yield return null;
            StartGame();
        }

        private void Update()
        {
            if (State == GameState.Playing)
            {
                elapsedTime += Time.deltaTime;

                if (combo > 1)
                {
                    comboTimer -= Time.deltaTime;
                    if (comboTimer <= 0f)
                    {
                        combo = 1;
                    }
                }

                gameUI.RefreshHUD(score, highScore, energy, maxEnergy, player.CurrentLives, player.MaxLives, combo, elapsedTime);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (State == GameState.Playing)
                {
                    PauseGame();
                }
                else if (State == GameState.Paused)
                {
                    ResumeGame();
                }
            }
        }

        public void StartGame()
        {
            ClearRuntimeObjects();

            score = 0;
            elapsedTime = 0f;
            combo = 1;
            comboTimer = 0f;
            energy = maxEnergy * startingEnergyPercent;
            State = GameState.Playing;
            Time.timeScale = 1f;

            player.gameObject.SetActive(true);
            player.ResetPlayer();
            enemySpawner.ResetSpawner();
            gameUI.ShowGameplay();
            gameUI.RefreshHUD(score, highScore, energy, maxEnergy, player.CurrentLives, player.MaxLives, combo, elapsedTime);
        }

        public void PauseGame()
        {
            if (State != GameState.Playing)
            {
                return;
            }

            State = GameState.Paused;
            Time.timeScale = 0f;
            gameUI.ShowPause();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused)
            {
                return;
            }

            State = GameState.Playing;
            Time.timeScale = 1f;
            gameUI.ShowGameplay();
        }

        public void GameOver()
        {
            if (State == GameState.GameOver)
            {
                return;
            }

            State = GameState.GameOver;
            highScore = Mathf.Max(highScore, score);
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();

            Time.timeScale = 0f;
            gameUI.ShowGameOver(score, highScore, elapsedTime);
        }

        public void RestartGame()
        {
            startImmediatelyAfterReload = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMenu()
        {
            startImmediatelyAfterReload = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void RegisterEnemyDefeated(int basePoints, float energyReward)
        {
            if (!IsPlaying)
            {
                return;
            }

            score += Mathf.Max(0, basePoints) * combo;
            AddEnergy(energyReward);
            combo = Mathf.Clamp(combo + 1, 1, 8);
            comboTimer = comboDuration;
        }

        public void RegisterPickup(int points, float energyReward)
        {
            if (!IsPlaying)
            {
                return;
            }

            score += Mathf.Max(0, points);
            AddEnergy(energyReward);
            comboTimer = comboDuration;
        }

        public void RegisterEnemyEscaped(float energyPenalty)
        {
            if (!IsPlaying)
            {
                return;
            }

            combo = 1;
            comboTimer = 0f;
        }

        public bool TrySpendEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            if (!IsPlaying || energy < amount)
            {
                return false;
            }

            energy -= amount;
            return true;
        }

        public void AddEnergy(float amount)
        {
            energy = Mathf.Clamp(energy + Mathf.Max(0f, amount), 0f, maxEnergy);
        }

        private void ShowMenu()
        {
            State = GameState.Menu;
            Time.timeScale = 1f;

            if (player != null)
            {
                player.gameObject.SetActive(false);
            }

            gameUI.ShowMainMenu(highScore);
        }

        private static void ClearRuntimeObjects()
        {
            DestroyAll<PlayerProjectile>();
            DestroyAll<EnemyProjectile>();
            DestroyAll<EnemyController>();
            DestroyAll<EnergyPickup>();
        }

        private static void DestroyAll<T>() where T : Component
        {
            T[] objects = FindObjectsOfType<T>();
            foreach (T item in objects)
            {
                Destroy(item.gameObject);
            }
        }
    }
}
