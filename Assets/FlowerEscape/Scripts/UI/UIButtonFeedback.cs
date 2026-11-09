using UnityEngine;
using UnityEngine.EventSystems;

namespace FlowerEscape
{
    /// <summary>Phản hồi khi bấm nút: thu nhỏ nhẹ lúc nhấn.</summary>
    public class UIButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float pressedScale = 0.92f;
        bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            transform.localScale = Vector3.one * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            transform.localScale = Vector3.one;
        }

        void OnDisable() => transform.localScale = Vector3.one;
    }
}
