using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FlowerEscape
{
    /// <summary>Hiệu ứng UI nhỏ gọn, không cần DOTween.</summary>
    public static class UIFx
    {
        public static float EaseInOut(float t) => t * t * (3f - 2f * t);

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        /// <summary>Nảy scale: to lên rồi về 1.</summary>
        public static IEnumerator Punch(Transform tr, float strength = 0.25f, float duration = 0.25f)
        {
            float t = 0f;
            while (t < 1f && tr != null)
            {
                t += Time.unscaledDeltaTime / duration;
                float s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * strength;
                tr.localScale = Vector3.one * s;
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }

        /// <summary>Rung ngang: báo lỗi (thiếu tiền, board đầy...).</summary>
        public static IEnumerator Shake(RectTransform rt, float strength = 18f, float duration = 0.3f)
        {
            if (rt == null) yield break;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f;
            while (t < 1f && rt != null)
            {
                t += Time.unscaledDeltaTime / duration;
                float damper = 1f - Mathf.Clamp01(t);
                rt.anchoredPosition = basePos + new Vector2(Mathf.Sin(t * 40f) * strength * damper, 0f);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = basePos;
        }
    }
}
