using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Menu: Tools > Flower Escape > Add Game View Sizes
    /// Thêm sẵn các độ phân giải test vào dropdown của Game view.
    /// (Unity không có API public cho việc này nên dùng reflection; nếu lỗi hãy thêm tay bằng nút "+".)
    /// </summary>
    public static class GameViewSizeTool
    {
        static readonly (int w, int h, string label)[] Sizes =
        {
            (1080, 2520, "Flower 9:21 (1080x2520)"),
            (1536, 2048, "Flower 3:4 (1536x2048)"),
            (1080, 1920, "Flower 9:16 (1080x1920)"),
        };

        [MenuItem("Tools/Flower Escape/Add Game View Sizes (9-21, 3-4, 9-16)", false, 10)]
        public static void AddSizes()
        {
            try
            {
                Assembly asm = typeof(Editor).Assembly;
                Type sizesType = asm.GetType("UnityEditor.GameViewSizes");
                Type singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                object instance = singleton.GetProperty("instance").GetValue(null);

                Type sizeType = asm.GetType("UnityEditor.GameViewSize");
                Type sizeEnum = asm.GetType("UnityEditor.GameViewSizeType");
                ConstructorInfo ctor = sizeType.GetConstructor(new[] { sizeEnum, typeof(int), typeof(int), typeof(string) });
                MethodInfo getGroup = sizesType.GetMethod("GetGroup");

                GameViewSizeGroupType[] groups = { GameViewSizeGroupType.Standalone, GameViewSizeGroupType.Android, GameViewSizeGroupType.iOS };
                int added = 0;
                foreach (var g in groups)
                {
                    object group = getGroup.Invoke(instance, new object[] { g });
                    var texts = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
                    foreach (var (w, h, label) in Sizes)
                    {
                        if (Array.Exists(texts, t => t.StartsWith(label))) continue;
                        object size = ctor.Invoke(new object[] { Enum.ToObject(sizeEnum, 1), w, h, label }); // 1 = FixedResolution
                        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
                        added++;
                    }
                }
                sizesType.GetMethod("SaveToHDD")?.Invoke(instance, null);
                Debug.Log($"[Flower Escape] Đã thêm {added} kích thước Game view. Mở dropdown độ phân giải trong tab Game để chọn.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Flower Escape] Không thêm tự động được Game view size (" + e.Message +
                                 "). Hãy thêm tay: tab Game > dropdown độ phân giải > '+' > Fixed Resolution 1080x2520 và 1536x2048.");
            }
        }
    }
}
