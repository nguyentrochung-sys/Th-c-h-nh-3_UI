using System;
using UnityEngine;

namespace FlowerEscape
{
    /// <summary>
    /// Dữ liệu phiên chơi: tiền (Sun), level, độ khó.
    /// Các UI khác lắng nghe event để tự cập nhật.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [Header("Giá trị khởi đầu (theo thiết kế Figma)")]
        public int sun = 165;
        public int level = 12;
        public string difficulty = "Hard";

        public event Action<int> SunChanged;
        public event Action<int> LevelChanged;

        public int Sun => sun;
        public int Level => level;

        void Awake()
        {
            Instance = this;
        }

        public bool CanAfford(int amount) => amount <= 0 || sun >= amount;

        public bool TrySpendSun(int amount)
        {
            if (amount <= 0) return true;
            if (sun < amount) return false;
            sun -= amount;
            SunChanged?.Invoke(sun);
            return true;
        }

        public void AddSun(int amount)
        {
            sun += amount;
            SunChanged?.Invoke(sun);
        }

        public void NextLevel()
        {
            level++;
            LevelChanged?.Invoke(level);
        }
    }
}
