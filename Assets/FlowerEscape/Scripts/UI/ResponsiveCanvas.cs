using System;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>
    /// Thiết lập CanvasScaler cho responsive và phân loại tỉ lệ màn hình.
    ///
    /// - Reference resolution 1080x2348 (= frame Figma 402x874 phóng theo chiều ngang 1080).
    /// - ScreenMatchMode.Expand: canvas luôn >= 1080x2348 ở CẢ 2 chiều.
    ///     + Màn 9:21 (1080x2520) -> canvas 1080 x 2520 : dư 172 chiều cao -> giãn khoảng giữa HUD và board.
    ///     + Màn 3:4  (1536x2048) -> canvas 1761 x 2348 : dư chiều ngang -> UI nằm giữa, nền phủ 2 bên.
    ///   => Nội dung trong frame Figma không bao giờ bị cắt.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveCanvas : MonoBehaviour
    {
        [Tooltip("= frame Figma 402x874 nhân 1080/402")]
        public Vector2 referenceResolution = new Vector2(1080, 2348);

        [Tooltip("width/height >= ngưỡng này thì coi là màn 'rộng' (tablet 3:4 = 0.75). 9:16 = 0.5625, 9:21 = 0.43")]
        public float wideAspectThreshold = 0.62f;

        public float Aspect { get; private set; }
        public bool IsWide { get; private set; }

        /// <summary>Bắn ra khi kích thước màn hình thay đổi (xoay máy, đổi Game view...).</summary>
        public event Action Changed;

        Vector2Int lastSize;

        void Awake()
        {
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            Evaluate(true);
        }

        void Update()
        {
            Evaluate(false);
        }

        /// <summary>Ép tính theo kích thước cho trước (dùng khi chụp ảnh kiểm tra ở nhiều tỉ lệ).</summary>
        public void EvaluateFor(int width, int height)
        {
            Aspect = height > 0 ? (float)width / height : 0.5625f;
            IsWide = Aspect >= wideAspectThreshold;
        }

        void Evaluate(bool force)
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (!force && size == lastSize) return;
            lastSize = size;
            Aspect = size.y > 0 ? (float)size.x / size.y : 0.5625f;
            IsWide = Aspect >= wideAspectThreshold;
            Changed?.Invoke();
        }
    }
}
