using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>
    /// Thanh HUD trên cùng: Level, thanh tiến trình (đầu lâu = wave, cờ = boss/đích),
    /// khoảng cách, Sun, độ khó, nút x2, nút cài đặt (pause), nút đóng, nút nhận thưởng.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        public GameSession session;

        [Header("Text")]
        public TMP_Text levelText;
        public TMP_Text sunText;
        public TMP_Text difficultyText;
        public TMP_Text distanceText;
        public TMP_Text x2Label;

        [Header("Progress")]
        public RectTransform progressFill;
        [Tooltip("Badge '123m' chạy theo thanh tiến trình")]
        public RectTransform distanceBadge;
        [Range(0, 1)] public float initialProgress = 0.375f; // Figma: fill 103/275

        [Header("Buttons")]
        public Button settingsButton;
        public Button closeButton;
        public Button x2Button;
        public Button rewardButton;
        public int rewardAmount = 50;

        [Header("Pause")]
        public GameObject pausePanel;
        public Button resumeButton;

        bool fast;
        bool paused;
        bool rewardUsed;

        void Start()
        {
            if (session != null)
            {
                session.SunChanged += OnSunChanged;
                session.LevelChanged += OnLevelChanged;
            }
            if (settingsButton) settingsButton.onClick.AddListener(Pause);
            if (resumeButton) resumeButton.onClick.AddListener(Resume);
            if (closeButton) closeButton.onClick.AddListener(Restart);
            if (x2Button) x2Button.onClick.AddListener(ToggleSpeed);
            if (rewardButton) rewardButton.onClick.AddListener(ClaimReward);
            if (pausePanel) pausePanel.SetActive(false);

            RefreshAll();
            SetProgress(initialProgress);
        }

        void OnDestroy()
        {
            if (session != null)
            {
                session.SunChanged -= OnSunChanged;
                session.LevelChanged -= OnLevelChanged;
            }
        }

        void RefreshAll()
        {
            if (session == null) return;
            if (sunText) sunText.text = session.Sun.ToString();
            if (levelText) levelText.text = "Level " + session.Level;
            if (difficultyText) difficultyText.text = session.difficulty;
            if (x2Label) x2Label.text = "x2";
        }

        void OnSunChanged(int value)
        {
            if (sunText == null) return;
            sunText.text = value.ToString();
            StartCoroutine(UIFx.Punch(sunText.transform, 0.2f, 0.2f));
        }

        void OnLevelChanged(int value)
        {
            if (levelText) levelText.text = "Level " + value;
        }

        public void SetProgress(float p)
        {
            p = Mathf.Clamp01(p);
            if (progressFill) progressFill.anchorMax = new Vector2(p, 1f);
            if (distanceBadge)
            {
                distanceBadge.anchorMin = new Vector2(p, distanceBadge.anchorMin.y);
                distanceBadge.anchorMax = new Vector2(p, distanceBadge.anchorMax.y);
            }
        }

        public void SetDistance(int meters)
        {
            if (distanceText) distanceText.text = meters + "m";
        }

        /// <summary>Được GameplayUIController gọi khi đổi trạng thái.</summary>
        public void OnStateChanged(GameplayState state)
        {
            if (state == GameplayState.GearSetup) rewardUsed = false;
            if (rewardButton) rewardButton.gameObject.SetActive(state == GameplayState.GearSetup && !rewardUsed);
        }

        void ClaimReward()
        {
            // Game thật: hiện quảng cáo video, xem xong mới cộng thưởng
            if (rewardUsed || session == null) return;
            rewardUsed = true;
            session.AddSun(rewardAmount);
            rewardButton.gameObject.SetActive(false);
        }

        void ToggleSpeed()
        {
            fast = !fast;
            if (!paused) Time.timeScale = fast ? 2f : 1f;
            if (x2Label) x2Label.text = fast ? "x1" : "x2";
        }

        void Pause()
        {
            paused = true;
            Time.timeScale = 0f;
            if (pausePanel) pausePanel.SetActive(true);
        }

        void Resume()
        {
            paused = false;
            Time.timeScale = fast ? 2f : 1f;
            if (pausePanel) pausePanel.SetActive(false);
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
