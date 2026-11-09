using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>
    /// Một ô trên bàn xếp gear. Ô có thể bị khóa (mờ) hoặc mở (lá sen).
    /// Nhận sự kiện thả (OnDrop) khi người chơi kéo gear vào.
    /// </summary>
    public class GearCell : MonoBehaviour, IDropHandler
    {
        public Image background;

        [Header("Hình ô mở / khóa")]
        public Sprite unlockedSprite;
        public Color unlockedColor = Color.white;
        public Sprite lockedSprite;
        public Color lockedColor = new Color(1f, 1f, 1f, 0.35f);

        public GearBoard Board { get; private set; }
        public int Index { get; private set; }
        public bool Locked { get; private set; }
        public GearItem Item { get; set; }
        public bool IsFree => !Locked && Item == null;

        public void Init(GearBoard board, int index)
        {
            Board = board;
            Index = index;
        }

        public void SetLocked(bool locked)
        {
            Locked = locked;
            if (background == null) return;
            if (locked)
            {
                if (lockedSprite != null) background.sprite = lockedSprite;
                background.color = lockedColor;
            }
            else
            {
                if (unlockedSprite != null) background.sprite = unlockedSprite;
                background.color = unlockedColor;
            }
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag == null || Board == null) return;
            var item = eventData.pointerDrag.GetComponent<GearItem>();
            if (item != null) Board.HandleDrop(item, this);
        }
    }
}
