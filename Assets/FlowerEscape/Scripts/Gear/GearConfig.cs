using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>
    /// Bảng hình ảnh cho gear:
    /// - Gear số (1, 2, 4, 8, ...): hai gear cùng số gộp lại thành số x2.
    /// - Gear đặc biệt (vũ khí, chỉ số, earning, rotor...): kéo thả / đổi chỗ được nhưng không gộp.
    /// Sprite lấy từ Figma (Assets/FlowerEscape/Art/Sprites).
    /// </summary>
    [CreateAssetMenu(menuName = "Flower Escape/Gear Config", fileName = "GearConfig")]
    public class GearConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public int value = 1;
            public Sprite sprite;
            [Tooltip("Nhân màu cho sprite (dùng để tạo cấp cao hơn từ sprite có sẵn)")]
            public Color tint = Color.white;
            [Tooltip("Màu tô placeholder khi chưa có sprite")]
            public Color placeholderColor = Color.white;
        }

        [Serializable]
        public class Special
        {
            [Tooltip("Mã dùng trong initialLayout của GearBoard, vd: 'a', 'r'")]
            public string id;
            public Sprite baseSprite;
            public float baseScale = 1f;
            public Sprite overlaySprite;
            public float overlayScale = 0.6f;
            public Vector2 overlayOffset;
        }

        public List<Entry> entries = new List<Entry>();
        public List<Special> specials = new List<Special>();
        public Sprite placeholderSprite;
        public int maxValue = 64;

        public Entry Get(int value)
        {
            Entry best = null;
            foreach (var e in entries)
            {
                if (e.value == value) return e;
                if (e.value < value && (best == null || e.value > best.value)) best = e;
            }
            return best;
        }

        public Special GetSpecial(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return specials.Find(s => s.id == id);
        }

        /// <summary>Gán sprite/màu cho một Image theo giá trị gear.</summary>
        public void Apply(Image img, int value)
        {
            if (img == null) return;
            var e = Get(value);
            bool hasSprite = e != null && e.sprite != null;
            img.sprite = hasSprite ? e.sprite : placeholderSprite;
            img.color = hasSprite ? e.tint : (e != null ? e.placeholderColor : Color.white);
            img.preserveAspect = true;
        }
    }
}
