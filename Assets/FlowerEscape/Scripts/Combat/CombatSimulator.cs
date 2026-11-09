using System;
using System.Collections;
using UnityEngine;

namespace FlowerEscape
{
    /// <summary>
    /// Giả lập một wave combat để demo UI: thanh tiến trình chạy, quái mất máu rồi biến mất,
    /// hết wave thì cộng Sun và quay lại trạng thái xếp gear.
    /// (Bài thực hành tập trung vào UI nên combat chỉ ở mức minh họa.)
    /// </summary>
    public class CombatSimulator : MonoBehaviour
    {
        public GameSession session;
        public HudController hud;
        public GearBoard board;

        [Header("Quái (cùng thứ tự với thanh máu)")]
        public RectTransform[] enemies;
        public RectTransform[] enemyHpFills;

        [Header("Luật")]
        public float waveDuration = 6f;
        public int wavesPerLevel = 4;
        public int rewardPerWave = 25;
        public int startDistance = 123;
        public float metersPerSecond = 12f;

        public bool IsRunning { get; private set; }
        public event Action Finished;

        float progress;
        float distance;

        void Start()
        {
            progress = hud ? hud.initialProgress : 0f;
            distance = startDistance;
            if (hud) hud.SetDistance(startDistance);
        }

        public void Begin()
        {
            if (IsRunning) return;
            StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            IsRunning = true;
            ResetEnemies();

            float p0 = progress;
            float p1 = Mathf.Min(1f, progress + 1f / wavesPerLevel);
            // Bàn càng mạnh, wave càng nhanh xong
            float power = board ? Mathf.Max(1, board.TotalPower) : 1;
            float duration = Mathf.Clamp(waveDuration * 40f / (power + 20f), 2.5f, waveDuration);

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                if (hud)
                {
                    hud.SetProgress(Mathf.Lerp(p0, p1, k));
                    distance += metersPerSecond * Time.deltaTime;
                    hud.SetDistance(Mathf.RoundToInt(distance));
                }
                UpdateEnemies(k);
                yield return null;
            }

            progress = p1;
            if (session) session.AddSun(rewardPerWave);
            if (progress >= 0.999f)
            {
                progress = 0f;
                if (session) session.NextLevel();
                if (hud) hud.SetProgress(0f);
            }

            IsRunning = false;
            Finished?.Invoke();
        }

        void ResetEnemies()
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i]) enemies[i].gameObject.SetActive(true);
                SetHp(i, 1f);
            }
        }

        void UpdateEnemies(float k)
        {
            int n = enemies.Length;
            for (int i = 0; i < n; i++)
            {
                // Quái nhỏ chết trước, boss (index 0) chết cuối
                int order = i == 0 ? n - 1 : i - 1;
                float start = order / (float)n * 0.8f;
                float hp = 1f - Mathf.Clamp01((k - start) / 0.3f);
                if (i == 0) hp = 1f - k;
                SetHp(i, hp);
                if (enemies[i] && hp <= 0.001f && enemies[i].gameObject.activeSelf)
                    enemies[i].gameObject.SetActive(false);
            }
        }

        void SetHp(int i, float hp)
        {
            if (enemyHpFills == null || i >= enemyHpFills.Length || enemyHpFills[i] == null) return;
            var f = enemyHpFills[i];
            f.anchorMax = new Vector2(Mathf.Clamp01(hp), f.anchorMax.y);
        }
    }
}
