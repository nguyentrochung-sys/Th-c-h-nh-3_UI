using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>
    /// Một gear nằm trong ô: gear số (có chữ số) hoặc gear đặc biệt (ảnh nền + ảnh phủ).
    /// Kéo thả để di chuyển / gộp / đổi chỗ.
    /// Cấu trúc prefab: Root (Image trong suốt nhận raycast) > Icon, Overlay, Label.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class GearItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image icon;
        public Image overlay;
        public TMP_Text label;
        public CanvasGroup canvasGroup;
        public float dragScale = 1.15f;
        [Tooltip("Khoảng lề giữa gear và mép ô")]
        public float padding = 0f;

        public int Value { get; private set; }
        public string SpecialId { get; private set; }
        public bool IsSpecial => !string.IsNullOrEmpty(SpecialId);
        public GearCell Cell { get; private set; }

        GearBoard board;
        RectTransform rt;
        bool dragging;
        bool dropHandled;

        void Awake()
        {
            rt = (RectTransform)transform;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Setup(GearBoard owner, int value)
        {
            board = owner;
            SetValue(value);
        }

        public void SetupSpecial(GearBoard owner, GearConfig.Special special)
        {
            board = owner;
            SpecialId = special.id;
            Value = 0;
            if (icon)
            {
                icon.sprite = special.baseSprite;
                icon.color = Color.white;
                icon.preserveAspect = true;
                icon.rectTransform.localScale = Vector3.one * special.baseScale;
            }
            if (overlay)
            {
                bool has = special.overlaySprite != null;
                overlay.gameObject.SetActive(has);
                if (has)
                {
                    overlay.sprite = special.overlaySprite;
                    overlay.preserveAspect = true;
                    overlay.rectTransform.localScale = Vector3.one * special.overlayScale;
                    overlay.rectTransform.anchoredPosition = special.overlayOffset;
                }
            }
            if (label) label.gameObject.SetActive(false);
        }

        public void SetValue(int value)
        {
            Value = value;
            SpecialId = null;
            if (board != null && board.config != null) board.config.Apply(icon, value);
            if (icon) icon.rectTransform.localScale = Vector3.one;
            if (overlay) overlay.gameObject.SetActive(false);
            if (label)
            {
                label.gameObject.SetActive(true);
                label.text = value.ToString();
            }
        }

        /// <summary>Đặt gear vào ô (ô trở thành cha, gear lấp đầy ô).</summary>
        public void PlaceInto(GearCell cell)
        {
            Cell = cell;
            cell.Item = this;
            if (rt == null) rt = (RectTransform)transform;
            rt.SetParent(cell.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            rt.localScale = Vector3.one;
            rt.SetAsLastSibling();
        }

        public void Pop() => StartCoroutine(UIFx.Punch(transform, 0.3f, 0.25f));

        public void MarkDropHandled() => dropHandled = true;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (board == null || !board.Interactable || Cell == null)
            {
                eventData.pointerDrag = null; // hủy kéo
                return;
            }

            dragging = true;
            dropHandled = false;

            // Giữ nguyên kích thước hiển thị khi chuyển sang DragLayer (nằm trên cùng)
            Vector2 size = rt.rect.size;
            Vector3 worldScale = rt.lossyScale;
            RectTransform layer = board.dragLayer;
            rt.SetParent(layer, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            Vector3 layerScale = layer.lossyScale;
            rt.localScale = new Vector3(worldScale.x / layerScale.x, worldScale.y / layerScale.y, 1f) * dragScale;

            canvasGroup.blocksRaycasts = false; // để raycast xuyên xuống ô bên dưới
            FollowPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging) FollowPointer(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            // OnDrop (trên GearCell) chạy TRƯỚC OnEndDrag. Nếu không ô nào nhận -> về chỗ cũ.
            if (!dropHandled && this != null && Cell != null) PlaceInto(Cell);
        }

        void FollowPointer(PointerEventData eventData)
        {
            var layer = board.dragLayer;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, eventData.position, eventData.pressEventCamera, out var local))
                rt.localPosition = local;
        }
    }
}
