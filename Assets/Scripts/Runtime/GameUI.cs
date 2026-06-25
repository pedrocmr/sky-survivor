using UnityEngine;
using UnityEngine.UI;

namespace NeonSkySurvivor
{
    public sealed class GameUI : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject mainMenuPanel;
        public GameObject hudPanel;
        public GameObject pausePanel;
        public GameObject gameOverPanel;

        [Header("Main Menu")]
        public Text mainMenuHighScoreText;
        public Button startButton;
        public Button quitButton;

        [Header("HUD")]
        public Text scoreText;
        public Text highScoreText;
        public Text comboText;
        public Text timeText;
        public Slider healthSlider;
        public Text healthText;
        public Slider energySlider;
        public Text energyText;

        [Header("Pause")]
        public Button resumeButton;
        public Button pauseRestartButton;
        public Button pauseMenuButton;

        [Header("Game Over")]
        public Text finalScoreText;
        public Text finalHighScoreText;
        public Text finalTimeText;
        public Button restartButton;
        public Button gameOverMenuButton;

        private void Awake()
        {
            startButton.onClick.AddListener(() => GameManager.Instance.StartGame());
            quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());
            resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());
            pauseRestartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            pauseMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            gameOverMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());
        }

        public void ShowMainMenu(int highScore)
        {
            SetOnly(mainMenuPanel);
            mainMenuHighScoreText.text = $"RECORDE: {highScore:000000}";
        }

        public void ShowGameplay()
        {
            mainMenuPanel.SetActive(false);
            hudPanel.SetActive(true);
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }

        public void ShowPause()
        {
            mainMenuPanel.SetActive(false);
            hudPanel.SetActive(true);
            pausePanel.SetActive(true);
            gameOverPanel.SetActive(false);
        }

        public void ShowGameOver(int score, int highScore, float elapsedTime)
        {
            mainMenuPanel.SetActive(false);
            hudPanel.SetActive(false);
            pausePanel.SetActive(false);
            gameOverPanel.SetActive(true);

            finalScoreText.text = $"PONTUAÇÃO: {score:000000}";
            finalHighScoreText.text = $"RECORDE: {highScore:000000}";
            finalTimeText.text = $"TEMPO: {FormatTime(elapsedTime)}";
        }

        public void RefreshHUD(int score, int highScore, float energy, float maxEnergy, int currentLives, int maxLives, int combo, float elapsedTime)
        {
            scoreText.text = $"SCORE {score:000000}";
            highScoreText.text = $"BEST {highScore:000000}";
            comboText.text = combo > 1 ? $"COMBO x{combo}" : string.Empty;
            timeText.text = FormatTime(elapsedTime);
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxLives;
                healthSlider.value = currentLives;
            }

            if (healthText != null)
            {
                healthText.text = $"VIDA {currentLives}/{maxLives}";
            }
            energySlider.maxValue = maxEnergy;
            energySlider.value = energy;
            energyText.text = $"ENERGIA {Mathf.CeilToInt(energy)}/{Mathf.CeilToInt(maxEnergy)}";
        }

        private void SetOnly(GameObject activePanel)
        {
            mainMenuPanel.SetActive(activePanel == mainMenuPanel);
            hudPanel.SetActive(activePanel == hudPanel);
            pausePanel.SetActive(activePanel == pausePanel);
            gameOverPanel.SetActive(activePanel == gameOverPanel);
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainingSeconds:00}";
        }
    }
}
