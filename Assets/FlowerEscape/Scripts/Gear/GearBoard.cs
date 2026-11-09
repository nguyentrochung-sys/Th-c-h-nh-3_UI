using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowerEscape
{
    /// <summary>
    /// Bàn xếp gear 6x4 (theo Figma).
    /// Luật: kéo gear vào ô trống = di chuyển; vào gear số cùng giá trị = gộp (x2); còn lại = đổi chỗ.
    /// Gear đặc biệt (vũ khí, chỉ số...) chỉ di chuyển / đổi chỗ, không gộp.
    /// Chỉ cho kéo khi đang ở trạng thái xếp gear (Interactable = true).
    /// </summary>
    public class GearBoard : MonoBehaviour
    {
        public GearConfig config;
        public GearItem gearPrefab;
        [Tooltip("Lớp nằm trên cùng canvas để gear đang kéo không bị UI khác che")]
        public RectTransform dragLayer;
        public Transform cellsRoot;
        public int columns = 6;

        [Tooltip("Bố cục ban đầu (theo Figma 'Main - Buy gear'). Mỗi dòng 1 hàng, cách nhau bằng dấu cách.\n" +
                 "'#' = ô khóa, '.' = ô trống, số = gear số, chữ = gear đặc biệt (id trong GearConfig.specials)")]
        [TextArea(4, 8)]
        public string initialLayout =
            "2 8 . . a .\n" +
            "4 r 1 4 1 .\n" +
            ". . . 2 1 .\n" +
            "e . . s . l";

        public bool Interactable { get; set; } = true;

        /// <summary>Bắn ra mỗi khi bàn thay đổi (di chuyển, gộp, thêm gear).</summary>
        public event Action Changed;

        readonly List<GearCell> cells = new List<GearCell>();

        public IReadOnlyList<GearCell> Cells => cells;

        void Awake()
        {
            cells.Clear();
            cells.AddRange(cellsRoot.GetComponentsInChildren<GearCell>(true));
            for (int i = 0; i < cells.Count; i++) cells[i].Init(this, i);
        }

        void Start()
        {
            LoadLayout(initialLayout);
        }

        public void LoadLayout(string layout)
        {
            foreach (var c in cells)
            {
                if (c.Item != null) Destroy(c.Item.gameObject);
                c.Item = null;
                c.SetLocked(false);
            }

            if (string.IsNullOrEmpty(layout)) return;
            string[] rows = layout.Replace("\r", "").Split('\n');
            int r = 0;
            foreach (string row in rows)
            {
                if (string.IsNullOrWhiteSpace(row)) continue;
                string[] tokens = row.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (int c = 0; c < tokens.Length && c < columns; c++)
                {
                    int index = r * columns + c;
                    if (index >= cells.Count) break;
                    string t = tokens[c];
                    if (t == "#") cells[index].SetLocked(true);
                    else if (int.TryParse(t, out int v) && v > 0) CreateGear(v, cells[index]);
                    else if (config != null && config.GetSpecial(t) != null) CreateSpecial(config.GetSpecial(t), cells[index]);
                }
                r++;
            }
            Changed?.Invoke();
        }

        public GearItem CreateGear(int value, GearCell cell)
        {
            var item = Instantiate(gearPrefab);
            item.Setup(this, value);
            item.PlaceInto(cell);
            return item;
        }

        public GearItem CreateSpecial(GearConfig.Special special, GearCell cell)
        {
            var item = Instantiate(gearPrefab);
            item.SetupSpecial(this, special);
            item.PlaceInto(cell);
            return item;
        }

        public bool HasFreeCell
        {
            get
            {
                foreach (var c in cells) if (c.IsFree) return true;
                return false;
            }
        }

        /// <summary>Thêm gear mới vào một ô trống ngẫu nhiên (khi mua ở shop).</summary>
        public bool SpawnGear(int value)
        {
            var free = new List<GearCell>();
            foreach (var c in cells) if (c.IsFree) free.Add(c);
            if (free.Count == 0) return false;
            var cell = free[UnityEngine.Random.Range(0, free.Count)];
            CreateGear(value, cell).Pop();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Tổng sức mạnh của bàn = tổng giá trị gear số (dùng tính damage).</summary>
        public int TotalPower
        {
            get
            {
                int sum = 0;
                foreach (var c in cells) if (c.Item != null) sum += c.Item.Value;
                return sum;
            }
        }

        /// <summary>Được GearCell gọi khi người chơi thả gear vào ô.</summary>
        public bool HandleDrop(GearItem item, GearCell target)
        {
            if (!Interactable || target == null || target.Locked) return false;
            GearCell origin = item.Cell;
            if (origin == null || target == origin) return false;

            item.MarkDropHandled();

            if (target.Item == null)
            {
                // Di chuyển
                origin.Item = null;
                item.PlaceInto(target);
            }
            else if (!item.IsSpecial && !target.Item.IsSpecial &&
                     target.Item.Value == item.Value && item.Value < config.maxValue)
            {
                // Gộp: 2 + 2 = 4
                origin.Item = null;
                target.Item.SetValue(item.Value * 2);
                target.Item.Pop();
                Destroy(item.gameObject);
            }
            else
            {
                // Đổi chỗ
                GearItem other = target.Item;
                other.PlaceInto(origin);
                item.PlaceInto(target);
            }

            Changed?.Invoke();
            return true;
        }
    }
}
