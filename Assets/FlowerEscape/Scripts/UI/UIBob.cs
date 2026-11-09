using UnityEngine;

namespace FlowerEscape
{
    /// <summary>Nhấp nhô lên xuống (quái, thuyền trên mặt nước).</summary>
    public class UIBob : MonoBehaviour
    {
        public float amplitude = 10f;
        public float speed = 2f;
        public float phase;

        RectTransform rt;
        Vector2 basePos;

        void Awake()
        {
            rt = (RectTransform)transform;
            basePos = rt.anchoredPosition;
        }

        void Update()
        {
            rt.anchoredPosition = basePos + Vector2.up * (Mathf.Sin(Time.time * speed + phase) * amplitude);
        }
    }
}
