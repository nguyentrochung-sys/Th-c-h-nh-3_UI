using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Menu: Tools > Flower Escape > Build Gameplay Scene
    ///
    /// Dựng màn gameplay từ file Figma "Thực hành 4" (frame "Main" = Combat, "Main - Buy gear" = Xếp gear).
    /// - Mọi phần tử đặt theo đúng X, Y, W, H của layer trong Figma (đơn vị Figma x K, K = 1080/402).
    /// - Sprite: Assets/FlowerEscape/Art/Sprites/&lt;key&gt;.png (export từ Figma, đã tách nền).
    /// - Font: Lalezar (đúng font trong Figma) -> TMP Font Asset tự tạo.
    /// - Responsive: CanvasScaler Expand 1080x2348 + 5 vùng neo (trên-trái / trên-giữa / trên-phải / đáy / world).
    /// </summary>
    public static class FlowerEscapeSceneBuilder
    {
        public const string Root = "Assets/FlowerEscape";
        public const string SpriteDir = Root + "/Art/Sprites";
        const string FontDir = Root + "/Fonts";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/Gameplay.unity";

        /// <summary>1 đơn vị Figma = K đơn vị canvas (frame 402 rộng -> 1080).</summary>
        public const float K = 1080f / 402f;
        public const float FrameW = 402f, FrameH = 874f;

        // key -> (scale lúc export, kích thước node nguồn trong Figma) để bù phần đổ bóng/viền ngoài bounds
        static readonly Dictionary<string, (float s, float w, float h)> Src = new Dictionary<string, (float, float, float)>
        {
            { "bg_environment", (3, 874, 874) },
            { "hud_setting", (4, 25.30f, 28.15f) }, { "hud_close", (4, 26.15f, 26.15f) },
            { "slider_bg", (3, 36, 8) }, { "slider_fill_cyan", (3, 36, 8) }, { "slider_fill_red", (3, 36, 8) }, { "slider_fill_green", (3, 36, 8) },
            { "icon_skull", (4, 11, 13) }, { "marker_triangle", (4, 5, 7) }, { "icon_flag", (4, 14, 19.07f) },
            { "distance_pointer", (4, 44, 20) }, { "panel_sun", (3, 76, 28) }, { "icon_sun", (4, 24, 24) },
            { "badge_hard", (3, 54, 16) }, { "btn_xspeed", (4, 32, 32) }, { "btn_big_yellow", (3, 120, 54) },
            { "icon_video", (4, 32, 24) }, { "bar_bottom_buy", (3, 402, 201) }, { "bar_bottom_buy_wide", (3, 874, 201) }, { "white_rec", (3, 370, 73) },
            { "btn_buy_gear", (3, 82, 82) }, { "price_tag", (3, 55, 22) }, { "dot_cyan", (4, 24, 24) },
            { "btn_big_cyan", (3, 172, 54) }, { "btn_big_green", (3, 102, 54) },
            { "panel_board", (3, 376.08f, 497) }, { "panel_stats", (3, 356.73f, 120.92f) }, { "board_pads", (3, 355.23f, 233.95f) },
            { "stat_bar", (3, 165, 26) }, { "icon_move_speed", (4, 21.34f, 18.14f) }, { "icon_fire_rate", (4, 25.15f, 19.35f) },
            { "slot_active", (3, 50.79f, 72.55f) }, { "slot_disabled", (3, 50.79f, 72.55f) }, { "slot_empty", (3, 50.79f, 72.55f) },
            { "equip_boat_wheel", (3, 48.37f, 48.37f) }, { "equip_boat_sail", (3, 43.53f, 52.0f) },
            { "equip_gun_pea", (3, 50.79f, 50.79f) }, { "equip_gun_berry", (3, 50.79f, 50.79f) },
            { "gear_connected", (3, 38.70f, 38.70f) }, { "gear_connected_hl", (3, 38.70f, 38.70f) },
            { "boat", (3, 193.98f, 156) }, { "boss1", (3.5f, 124, 94) },
            { "mob_flying", (4, 36, 25) }, { "mob_melee", (4, 29, 32) }, { "mob_range", (4, 43, 35) }, { "mob_fast", (4, 46, 44) },
            { "char_nerd", (4, 27, 39) }, { "char_emo", (4, 23, 35) }, { "char_tomboy", (4, 29, 42) }, { "char_sexy", (4, 34, 36) },
            { "icon_status_poison", (4, 15, 15) }, { "icon_status_shield", (4, 16, 16) }, { "res_slot", (3, 53, 22) },
            { "bullet_pea", (1, 44, 44) }, { "bullet_berry", (1, 44, 44) }, { "bullet_chili", (1, 44, 44) }, { "bullet_pumpkin", (1, 44, 44) },
            { "btn_booster", (3, 58, 58) }, { "icon_booster_heart", (4, 36, 36) }, { "dot_green", (4, 24, 24) },
            { "banner_ad", (3, 402, 96) },
        };

        // Sprite 9-slice: border (L, B, R, T) theo pixel
        static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
        {
            { "slider_bg", new Vector4(15, 0, 15, 0) },
            { "slider_fill_cyan", new Vector4(12, 0, 12, 0) },
            { "slider_fill_red", new Vector4(12, 0, 12, 0) },
            { "slider_fill_green", new Vector4(12, 0, 12, 0) },
        };

        static TMP_FontAsset font;
        static Material outlineMat;
        static readonly SortedSet<string> missing = new SortedSet<string>();
        static readonly Color White = Color.white;
        static readonly Color SunYellow = new Color32(0xFF, 0xE5, 0x61, 0xFF);

        // =====================================================================
        [MenuItem("Tools/Flower Escape/Build Gameplay Scene", false, 1)]
        public static void Build() => Build(true);

        /// <summary>Dựng scene. interactive = false: không hiện hộp thoại (dùng cho tự động hoá).</summary>
        public static string Build(bool interactive)
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                if (!interactive) return "ERROR: thiếu TMP Essential Resources";
                EditorUtility.DisplayDialog("Flower Escape",
                    "Project chưa có TextMesh Pro Essential Resources.\nBấm 'Import TMP Essentials' rồi chạy lại menu.", "OK");
                EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
                return "ERROR: thiếu TMP Essential Resources";
            }

            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return "CANCELLED";
            }
            else
            {
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var sc = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (sc.isDirty && !string.IsNullOrEmpty(sc.path)) EditorSceneManager.SaveScene(sc);
                }
            }

            EnsureFolders();
            AssetDatabase.Refresh();
            EnsureSpriteImports();
            missing.Clear();

            font = GetOrCreateFont();
            outlineMat = CreateOutlineMaterial(font);

            GearConfig config = CreateOrUpdateGearConfig();
            GearItem gearPrefab = CreateGearPrefab();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene(config, gearPrefab);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            var sb = new StringBuilder();
            sb.AppendLine("[Flower Escape] Đã dựng xong scene từ Figma: " + ScenePath);
            sb.AppendLine("Font: " + (font ? font.name : "(none)"));
            if (missing.Count > 0)
            {
                sb.AppendLine($"Thiếu {missing.Count} sprite trong {SpriteDir}:");
                foreach (var m in missing) sb.AppendLine("  - " + m + ".png");
            }
            else sb.AppendLine("Đủ toàn bộ sprite.");
            Debug.Log(sb.ToString());

            if (interactive)
                EditorUtility.DisplayDialog("Flower Escape", "Đã dựng xong màn Gameplay theo Figma.\n\nBấm Play, chọn Game view 9:21 hoặc 3:4 để test.", "OK");
            return sb.ToString();
        }

        // =====================================================================
        // Asset
        // =====================================================================
        static void EnsureFolders()
        {
            foreach (var dir in new[] { SpriteDir, FontDir, DataDir, PrefabDir, SceneDir })
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }

        /// <summary>Đặt import settings cho mọi PNG: Sprite, không mipmap, alpha trong suốt, 9-slice nếu cần.</summary>
        static void EnsureSpriteImports()
        {
            foreach (var file in Directory.GetFiles(SpriteDir, "*.png"))
            {
                string path = file.Replace('\\', '/');
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                string key = Path.GetFileNameWithoutExtension(path);
                bool changed = false;
                void Set<T>(T cur, T val, System.Action<T> apply) { if (!EqualityComparer<T>.Default.Equals(cur, val)) { apply(val); changed = true; } }

                Set(ti.textureType, TextureImporterType.Sprite, v => ti.textureType = v);
                Set(ti.spriteImportMode, SpriteImportMode.Single, v => ti.spriteImportMode = v);
                Set(ti.mipmapEnabled, false, v => ti.mipmapEnabled = v);
                Set(ti.alphaIsTransparency, true, v => ti.alphaIsTransparency = v);
                Set(ti.wrapMode, TextureWrapMode.Clamp, v => ti.wrapMode = v);
                // 4096: giữ nguyên độ phân giải export (ảnh > 2048 px sẽ bị Unity thu nhỏ nếu để 2048)
                Set(ti.maxTextureSize, 4096, v => ti.maxTextureSize = v);
                Set(ti.textureCompression, TextureImporterCompression.CompressedHQ, v => ti.textureCompression = v);
                Vector4 border = Borders.TryGetValue(key, out var b) ? b : Vector4.zero;
                Set(ti.spriteBorder, border, v => ti.spriteBorder = v);
                if (changed) ti.SaveAndReimport();
            }
        }

        static TMP_FontAsset GetOrCreateFont()
        {
            string path = FontDir + "/Lalezar SDF.asset";
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa) return fa;
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/Lalezar-Regular.ttf");
            if (ttf == null) return TMP_Settings.defaultFontAsset;

            fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = "Lalezar SDF";
            AssetDatabase.CreateAsset(fa, path);
            if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
            {
                fa.atlasTextures[0].name = "Lalezar SDF Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            }
            fa.material.name = "Lalezar SDF Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            return fa;
        }

        static Material CreateOutlineMaterial(TMP_FontAsset f)
        {
            if (f == null || f.material == null) return null;
            ShaderUtilities.GetShaderPropertyIDs();
            string path = $"{FontDir}/{f.name} Outline.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(f.material);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = f.material.shader;
            mat.SetTexture(ShaderUtilities.ID_MainTex, f.material.GetTexture(ShaderUtilities.ID_MainTex));
            mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
            mat.SetFloat(ShaderUtilities.ID_FaceDilate, 0.2f);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.28f);
            mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(0x23, 0x21, 0x4A, 0xFF));
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Sprite LoadSprite(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            string path = SpriteDir + "/" + key + ".png";
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is Sprite s) return s;
            return null;
        }

        static GearConfig CreateOrUpdateGearConfig()
        {
            string path = DataDir + "/GearConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<GearConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GearConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            config.placeholderSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            config.entries = new List<GearConfig.Entry>
            {
                new GearConfig.Entry { value = 1, sprite = Need("gear_1") },
                new GearConfig.Entry { value = 2, sprite = Need("gear_2") },
                new GearConfig.Entry { value = 4, sprite = Need("gear_4") },
                new GearConfig.Entry { value = 8, sprite = Need("gear_8") },
                // Figma chỉ vẽ đến 8: cấp cao hơn dùng lại sprite + nhân màu
                new GearConfig.Entry { value = 16, sprite = Need("gear_1"), tint = new Color(1f, 0.55f, 0.85f) },
                new GearConfig.Entry { value = 32, sprite = Need("gear_2"), tint = new Color(0.55f, 1f, 0.55f) },
                new GearConfig.Entry { value = 64, sprite = Need("gear_4"), tint = new Color(1f, 0.85f, 0.3f) },
            };
            config.specials = new List<GearConfig.Special>
            {
                new GearConfig.Special { id = "a", baseSprite = Need("gear_weapon"), overlaySprite = Need("flower_heartboom"), overlayScale = 0.582f, overlayOffset = new Vector2(-0.8f, 3.2f) * K },
                new GearConfig.Special { id = "r", baseSprite = Need("gear_rotor"), baseScale = 1.05f },
                new GearConfig.Special { id = "e", baseSprite = Need("gear_earning"), overlaySprite = Need("flower_regen"), overlayScale = 0.62f },
                new GearConfig.Special { id = "s", baseSprite = Need("gear_weapon"), overlaySprite = Need("flower_sunbloom"), overlayScale = 0.582f },
                new GearConfig.Special { id = "l", baseSprite = Need("gear_stats"), overlaySprite = Need("flower_luck"), overlayScale = 0.62f },
            };
            config.maxValue = 64;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        static Sprite Need(string key)
        {
            var s = LoadSprite(key);
            if (s == null) missing.Add(key);
            return s;
        }

        static GearItem CreateGearPrefab()
        {
            string path = PrefabDir + "/GearItem.prefab";
            var rt = NewRect("GearItem", null);
            rt.sizeDelta = new Vector2(58.04f, 58.04f) * K;
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = new Color(1, 1, 1, 0); // trong suốt, chỉ để nhận kéo thả
            hit.raycastTarget = true;
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            var item = rt.gameObject.AddComponent<GearItem>();

            var icon = Stretch("Icon", rt).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var overlay = Stretch("Overlay", rt).gameObject.AddComponent<Image>();
            overlay.preserveAspect = true;
            overlay.raycastTarget = false;
            overlay.gameObject.SetActive(false);
            var label = SetupText(Stretch("Label", rt), "1", 33.86f, White, TextAlignmentOptions.Center);

            item.icon = icon;
            item.overlay = overlay;
            item.label = label;
            item.canvasGroup = cg;
            item.padding = 0f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(rt.gameObject, path);
            Object.DestroyImmediate(rt.gameObject);
            return prefab.GetComponent<GearItem>();
        }

        static void AddSceneToBuildSettings()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // =====================================================================
        // Helpers tạo UI (toạ độ Figma)
        // =====================================================================
        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            if (parent) rt.SetParent(parent, false);
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>
        /// Vùng neo kích thước bằng cả frame Figma (1080 x 2348), neo vào 1 điểm của cha.
        /// Con của vùng dùng toạ độ Figma tính từ góc trái-trên frame.
        /// </summary>
        static RectTransform Region(string name, Transform parent, Vector2 anchor)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(FrameW, FrameH) * K;
            return rt;
        }

        /// <summary>Đặt rect theo X, Y, W, H của Figma, tính từ góc trái-trên của cha.</summary>
        static RectTransform D(string name, Transform parent, float x, float y, float w, float h)
        {
            var rt = NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2((x + w * 0.5f) * K, -(y + h * 0.5f) * K);
            rt.sizeDelta = new Vector2(w, h) * K;
            return rt;
        }

        /// <summary>Gắn sprite vào rect. Ảnh nằm ở con "Art", nở ra theo phần đổ bóng của bản export.</summary>
        static Image Spr(RectTransform rt, string key, bool raycast = false)
        {
            var art = Stretch("Art", rt);
            var img = art.gameObject.AddComponent<Image>();
            img.raycastTarget = raycast;
            var sp = LoadSprite(key);
            if (sp == null)
            {
                missing.Add(key);
                img.color = new Color(1f, 0f, 1f, 0.5f);
                return img;
            }
            img.sprite = sp;
            bool sliced = sp.border.sqrMagnitude > 0f;
            if (Src.TryGetValue(key, out var m))
            {
                float padX = (sp.rect.width / m.s - m.w) * 0.5f;
                float padY = (sp.rect.height / m.s - m.h) * 0.5f;
                Vector2 node = rt.sizeDelta / K;
                float fx = sliced ? 1f : node.x / m.w;
                float fy = sliced ? 1f : node.y / m.h;
                art.offsetMin = -new Vector2(padX * fx, padY * fy) * K;
                art.offsetMax = new Vector2(padX * fx, padY * fy) * K;
                if (sliced)
                {
                    img.type = Image.Type.Sliced;
                    img.pixelsPerUnitMultiplier = m.s / K;
                }
            }
            return img;
        }

        static RectTransform Pic(string name, Transform parent, string key, float x, float y, float w, float h)
        {
            var rt = D(name, parent, x, y, w, h);
            Spr(rt, key);
            return rt;
        }

        static TextMeshProUGUI SetupText(RectTransform rt, string text, float figmaSize, Color color, TextAlignmentOptions align)
        {
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            if (outlineMat) t.fontSharedMaterial = outlineMat;
            t.text = text;
            t.fontSize = figmaSize * K;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Text theo X, Y, W, H + cỡ chữ Figma (font Lalezar, căn giữa).</summary>
        static TextMeshProUGUI Txt(Transform parent, string name, string text, float figmaSize, float x, float y, float w, float h, Color? color = null)
        {
            var rt = D(name, parent, x, y, w, h);
            return SetupText(rt, text, figmaSize, color ?? White, TextAlignmentOptions.Center);
        }

        static Button Btn(RectTransform rt, Image target)
        {
            target.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = target;
            var cb = b.colors;
            cb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);
            b.colors = cb;
            rt.gameObject.AddComponent<UIButtonFeedback>();
            return b;
        }

        static CanvasGroup Group(RectTransform rt) => rt.gameObject.AddComponent<CanvasGroup>();

        static void Bob(RectTransform rt, float amp, float speed, float phase)
        {
            var b = rt.gameObject.AddComponent<UIBob>();
            b.amplitude = amp;
            b.speed = speed;
            b.phase = phase;
        }

        /// <summary>Thanh trượt (nền + fill co giãn theo anchor), theo slider gameplay trong Figma.</summary>
        static RectTransform Bar(string name, Transform parent, float x, float y, float w, float h, string fillKey, float ratio, out RectTransform fillHolder)
        {
            var bar = D(name, parent, x, y, w, h);
            Spr(bar, "slider_bg");
            fillHolder = NewRect("Fill", bar);
            fillHolder.anchorMin = Vector2.zero;
            fillHolder.anchorMax = new Vector2(ratio, 1f);
            fillHolder.offsetMin = fillHolder.offsetMax = Vector2.zero;
            Spr(fillHolder, fillKey);
            return bar;
        }

        // =====================================================================
        // Dựng scene
        // =====================================================================
        static void BuildScene(GearConfig config, GearItem gearPrefab)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x1E, 0x5C, 0x8A, 0xFF);
            camGo.transform.position = new Vector3(0, 0, -10);

            var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            esGo.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var systems = new GameObject("GameSystems");
            var session = systems.AddComponent<GameSession>();
            var combat = systems.AddComponent<CombatSimulator>();

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(FrameW, FrameH) * K; // 1080 x 2348
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var responsive = canvasGo.AddComponent<ResponsiveCanvas>();
            responsive.referenceResolution = scaler.referenceResolution;
            var canvasRt = (RectTransform)canvasGo.transform;

            // ---- Nền (ngoài SafeArea). Ảnh 874x874 rộng hơn frame -> màn 3:4 vẫn phủ kín 2 bên ----
            var bgRegion = Region("BackgroundRegion", canvasRt, new Vector2(0.5f, 1f));
            Pic("Background", bgRegion, "bg_environment", -236, 0, 874, 874);

            var safe = Stretch("SafeArea", canvasRt);
            safe.gameObject.AddComponent<SafeArea>();

            // ---- World (combat): neo trên-giữa cùng nền ----
            var world = Region("WorldRegion", safe, new Vector2(0.5f, 1f));
            var battle = Stretch("BattleRoot", world);
            var battleGroup = Group(battle);
            BuildBattle(battle, out var enemies, out var hpFills);

            // ---- Vùng đáy: board, booster, shop, banner ----
            // Nền đen sau banner quảng cáo, kéo hết chiều ngang (màn 3:4 rộng hơn banner)
            var bannerBack = NewRect("BannerBack", safe);
            bannerBack.anchorMin = new Vector2(0, 0);
            bannerBack.anchorMax = new Vector2(1, 0);
            bannerBack.pivot = new Vector2(0.5f, 0);
            bannerBack.anchoredPosition = Vector2.zero;
            bannerBack.sizeDelta = new Vector2(0, 96 * K);
            bannerBack.gameObject.AddComponent<Image>().color = Color.black;

            var bottom = Region("BottomRegion", safe, new Vector2(0.5f, 0f));
            var boardRoot = BuildBoard(bottom, config, gearPrefab, out var gearBoard, out var damageText);
            var boosters = Stretch("BoostersRoot", bottom);
            var boosterGroup = Group(boosters);
            BuildBoosters(boosters);
            var shopRoot = Stretch("ShopRoot", bottom);
            var shopGroup = Group(shopRoot);
            var shop = BuildShop(shopRoot, session, gearBoard, out var startButton);
            Pic("BannerAd", bottom, "banner_ad", 0, 778, 402, 96).GetComponentInChildren<Image>().raycastTarget = true;

            // ---- HUD: 3 vùng neo trên ----
            var hud = BuildHud(safe, session, out var rewardGroup, out var battleRightGroup);

            var dragLayer = Stretch("DragLayer", safe);
            gearBoard.dragLayer = dragLayer;

            var pause = BuildPausePanel(canvasRt, out var resumeButton);
            hud.pausePanel = pause;
            hud.resumeButton = resumeButton;

            combat.session = session;
            combat.hud = hud;
            combat.board = gearBoard;
            combat.enemies = enemies.ToArray();
            combat.enemyHpFills = hpFills.ToArray();

            var ui = canvasGo.AddComponent<GameplayUIController>();
            ui.gearBoard = gearBoard;
            ui.shop = shop;
            ui.combat = combat;
            ui.hud = hud;
            ui.boardRoot = boardRoot;
            ui.gearBoardPos = new Vector2(13f, -199f) * K;       // Figma "Main - Buy gear": panel (13, 199) 376x497
            ui.gearBoardScale = 1f;
            ui.combatBoardPos = new Vector2(12f, -463f) * K;     // Figma "Main": panel (12, 463) 311x411
            ui.combatBoardScale = 311f / 376.08f;
            ui.shopRoot = shopRoot;
            ui.shopGroup = shopGroup;
            ui.shopSlide = 210f * K;
            ui.rewardGroup = rewardGroup;
            ui.boosterRoot = boosters;
            ui.boosterGroup = boosterGroup;
            ui.boosterSlide = 80f * K;
            ui.battleGroup = battleGroup;
            ui.battleRightGroup = battleRightGroup;
            ui.startButton = startButton;
            ui.damageText = damageText;

            ui.PreviewState(GameplayState.GearSetup);
            Selection.activeGameObject = canvasGo;
        }

        // ---------------------------------------------------------------------
        // Combat (frame "Main")
        // ---------------------------------------------------------------------
        static void BuildBattle(RectTransform battle, out List<RectTransform> enemies, out List<RectTransform> fills)
        {
            enemies = new List<RectTransform>();
            fills = new List<RectTransform>();

            var boat = Pic("Boat", battle, "boat", 197, 195, 193.98f, 156);
            Bob(boat, 6, 1.1f, 0.4f);
            Bar("HpBar", boat, 303 - 197, 169 - 195, 37, 8, "slider_fill_green", 29f / 37f, out _);
            Txt(boat, "Hp", "120", 12, 309 - 197, 165 - 195, 24, 12);
            Pic("Shield", boat, "icon_status_shield", 313 - 197, 142 - 195, 16, 16);
            Txt(boat, "ShieldValue", "89", 12, 309 - 197, 153 - 195, 24, 12);
            Pic("Poison", boat, "icon_status_poison", 314 - 197, 180 - 195, 15, 15);
            Txt(boat, "PoisonValue", "2", 9, 322 - 197, 189 - 195, 11, 9);

            // Quái: (tên, sprite, x, y, w, h, có thanh máu?)
            var mobs = new (string key, float x, float y, float w, float h, float bx, float by, float bw, float ratio)[]
            {
                ("mob_flying", 109, 132, 36, 25, 0, 0, 0, 0),
                ("mob_flying", 188, 147, 36, 25, 157, 135, 27, 23f / 27f),
                ("mob_melee", 149, 353, 29, 32, 0, 0, 0, 0),
                ("mob_melee", 48, 352, 29, 32, 0, 0, 0, 0),
                ("mob_melee", 199, 258, 29, 32, 190, 263, 27, 25f / 27f),
                ("mob_melee", 101, 344, 29, 32, 0, 0, 0, 0),
                ("mob_melee", 180, 309, 29, 32, 0, 0, 0, 0),
                ("mob_range", 75, 392, 43, 35, 0, 0, 0, 0),
                ("mob_range", 152, 229, 43, 35, 165, 245, 27, 13f / 27f),
                ("mob_range", 216, 276, 43, 35, 0, 0, 0, 0),
                ("mob_fast", 216, 339, 46, 44, 0, 0, 0, 0),
            };
            var mobRects = new List<RectTransform>();
            var mobFills = new List<RectTransform>();
            for (int i = 0; i < mobs.Length; i++)
            {
                var m = mobs[i];
                var rt = Pic("Mob_" + (i + 1), battle, m.key, m.x, m.y, m.w, m.h);
                Bob(rt, 4, 2.2f, i * 0.9f);
                RectTransform fill = null;
                if (m.bw > 0) Bar("HpBar", rt, m.bx - m.x, m.by - m.y, m.bw, 8, "slider_fill_red", m.ratio, out fill);
                mobRects.Add(rt);
                mobFills.Add(fill);
            }

            Pic("Char_Nerd", battle, "char_nerd", 281, 267, 27, 39);
            Pic("Char_Emo", battle, "char_emo", 308, 226, 23, 35);
            Pic("Char_Tomboy", battle, "char_tomboy", 338, 232, 29, 42);
            Pic("Char_Sexy", battle, "char_sexy", 312, 273, 34, 36);

            var boss = Pic("Boss", battle, "boss1", -11, 215, 159, 121);
            Bob(boss, 5, 1.3f, 0f);
            Bar("HpBar", boss, 16 - (-11), 196 - 215, 97, 8, "slider_fill_red", 75f / 97f, out var bossFill);

            // index 0 = boss (CombatSimulator cho boss chết cuối)
            enemies.Add(boss);
            fills.Add(bossFill);
            enemies.AddRange(mobRects);
            fills.AddRange(mobFills);
        }

        static void BuildBoosters(RectTransform root)
        {
            float[] ys = { 491, 564, 637, 710 };
            for (int i = 0; i < ys.Length; i++)
            {
                var b = D("Booster_" + (i + 1), root, 334, ys[i], 58, 58);
                Btn(b, Spr(b, "btn_booster"));
                if (i == 0) Pic("Icon", b, "icon_booster_heart", 12, 12, 36, 36);
                var dot = Pic("Count", b, "dot_green", 38, 38, 24, 24);
                Txt(dot, "Text", "3", 18, 0, 1, 24, 24);
            }
        }

        // ---------------------------------------------------------------------
        // Board (frame "Main - Buy gear", gốc toạ độ = góc panel (13, 199))
        // ---------------------------------------------------------------------
        static RectTransform BuildBoard(RectTransform bottom, GearConfig config, GearItem gearPrefab, out GearBoard gearBoard, out TMP_Text damageText)
        {
            const float ox = 13f, oy = 199f;
            var board = NewRect("BoardRoot", bottom);
            board.anchorMin = board.anchorMax = new Vector2(0, 1);
            board.pivot = new Vector2(0, 1);
            board.sizeDelta = new Vector2(376.08f, 497f) * K;
            board.anchoredPosition = new Vector2(ox, -oy) * K;

            Pic("Panel", board, "panel_board", 0, 0, 376.08f, 497);
            Pic("StatsPanel", board, "panel_stats", 22.67f - ox, 208.67f - oy, 356.73f, 120.92f);
            Pic("Pads", board, "board_pads", 23.77f - ox, 334.03f - oy, 355.23f, 233.95f);

            Pic("SpeedBar", board, "stat_bar", 34 - ox, 215 - oy, 165, 26);
            Pic("DamageBar", board, "stat_bar", 205 - ox, 215 - oy, 166, 26);
            Pic("SpeedIcon", board, "icon_move_speed", 44 - ox, 218.35f - oy, 21.34f, 18.14f);
            Pic("DamageIcon", board, "icon_fire_rate", 214 - ox, 218.35f - oy, 25.15f, 19.35f);
            Txt(board, "SpeedText", "52.3mph", 14.51f, 80 - ox, 218.35f - oy, 74.97f, 22.98f);
            damageText = Txt(board, "DamageText", "73.3", 14.51f, 250 - ox, 218.35f - oy, 74.97f, 22.98f);

            // Hàng trang bị
            Pic("Slot_1", board, "slot_active", 34 - ox, 250 - oy, 50.79f, 72.55f);
            Pic("Slot_2", board, "slot_active", 92.81f - ox, 247.37f - oy, 50.79f, 72.55f);
            Pic("Slot_3", board, "slot_empty", 148.44f - ox, 247.37f - oy, 50.79f, 72.55f);
            Pic("Slot_4", board, "slot_disabled", 205.27f - ox, 247.37f - oy, 50.79f, 72.55f);
            Pic("Slot_5", board, "slot_disabled", 262.10f - ox, 247.37f - oy, 50.79f, 72.55f);
            Pic("Slot_6", board, "slot_empty", 318.94f - ox, 247.37f - oy, 50.79f, 72.55f);
            Pic("Equip_Wheel", board, "equip_boat_wheel", 35 - ox, 261 - oy, 48.37f, 48.37f);
            Pic("Equip_GunPea", board, "equip_gun_pea", 92.81f - ox, 257.04f - oy, 50.79f, 50.79f);
            Pic("Equip_GunBerry", board, "equip_gun_berry", 148.44f - ox, 257.04f - oy, 50.79f, 50.79f);
            Pic("Equip_Sail", board, "equip_boat_sail", 323.78f - ox, 257.04f - oy, 43.53f, 52f);
            Pic("Connect_1", board, "gear_connected_hl", 40.81f - ox, 303f - oy, 38.70f, 38.70f);
            Pic("Connect_2", board, "gear_connected_hl", 98.86f - ox, 303f - oy, 38.70f, 38.70f);
            Pic("Connect_3", board, "gear_connected", 153.27f - ox, 303f - oy, 38.70f, 38.70f);
            Pic("Connect_6", board, "gear_connected", 324.99f - ox, 303f - oy, 38.70f, 38.70f);
            Txt(board, "Rate_1", "0.24/s", 12.09f, 21.46f - ox, 289.69f - oy, 78.6f, 29.02f);
            Txt(board, "Rate_2", "1.17/s", 12.09f, 78.30f - ox, 289.69f - oy, 78.6f, 29.02f);

            // Lưới 6x4 ô kéo thả (vị trí gear trong Figma: x 28.7 + 57.8c, y 333.2 + 56c, cỡ 58)
            var grid = Stretch("Grid", board);
            for (int r = 0; r < 4; r++)
            for (int c = 0; c < 6; c++)
            {
                var cellRt = D($"Cell_{r}_{c}", grid, 28.72f + c * 57.8f - ox, 333.23f + r * 56.03f - oy, 58.04f, 58.04f);
                var img = cellRt.gameObject.AddComponent<Image>();
                img.color = new Color(1, 1, 1, 0);
                img.raycastTarget = true;
                var cell = cellRt.gameObject.AddComponent<GearCell>();
                cell.background = img;
                cell.unlockedColor = new Color(1, 1, 1, 0);
                cell.lockedColor = new Color(0, 0, 0, 0.25f);
            }

            gearBoard = board.gameObject.AddComponent<GearBoard>();
            gearBoard.config = config;
            gearBoard.gearPrefab = gearPrefab;
            gearBoard.cellsRoot = grid;
            gearBoard.columns = 6;
            return board;
        }

        // ---------------------------------------------------------------------
        // Shop (frame "Main - Buy gear")
        // ---------------------------------------------------------------------
        static ShopController BuildShop(RectTransform root, GameSession session, GearBoard gearBoard, out Button startButton)
        {
            // Bản symbol rộng 874 (thay vì instance 402) để màn 3:4 không lộ nền 2 bên
            var bar = Pic("Bar", root, "bar_bottom_buy_wide", -236, 577, 874, 201);
            bar.GetComponentInChildren<Image>().raycastTarget = true;

            var shop = root.gameObject.AddComponent<ShopController>();
            shop.session = session;
            shop.board = gearBoard;

            var cards = new List<ShopCard>();
            for (int i = 0; i < 4; i++)
            {
                float x = 16 + 96 * i;
                var c = D("Card_" + (i + 1), root, x, 602, 82, 82);
                var card = c.gameObject.AddComponent<ShopCard>();
                card.button = Btn(c, Spr(c, "btn_buy_gear"));
                var cc = card.button.colors;
                cc.disabledColor = Color.white; // thẻ trống vẫn giữ màu be như Figma
                card.button.colors = cc;

                var content = Stretch("Content", c);
                card.content = content.gameObject;
                var gear = D("Gear", content, 12, 12, 58.04f, 58.04f);
                card.gearIcon = Stretch("Icon", gear).gameObject.AddComponent<Image>();
                card.gearIcon.raycastTarget = false;
                card.gearIcon.sprite = LoadSprite("gear_1");
                card.gearIcon.preserveAspect = true;
                card.valueText = SetupText(Stretch("Value", gear), "1", 33.86f, White, TextAlignmentOptions.Center);

                Pic("PriceTag", content, "price_tag", 14, 56, 55, 22);
                Pic("Sun", content, "icon_sun", 16, 58, 18, 18.23f);
                card.priceText = Txt(content, "Price", "5", 16, 34, 63, 31, 12);
                card.videoIcon = Pic("Video", content, "icon_video", 54, -3, 32, 24).gameObject;
                if (i == 2)
                {
                    var info = Pic("Info", content, "dot_cyan", -5, -3, 24, 24);
                    Txt(info, "i", "i", 18, 4, 6, 16, 17);
                }
                cards.Add(card);
            }
            shop.cards = cards.ToArray();
            shop.activeCardCount = 3;

            Pic("ButtonsPanel", root, "white_rec", 16, 696, 370, 73);

            var refresh = D("Btn_Refresh", root, 63, 706, 102, 54);
            shop.refreshButton = Btn(refresh, Spr(refresh, "btn_big_green"));
            Txt(refresh, "Label", "Refresh", 16, 76 - 63, 711 - 706, 76, 28);
            Pic("Sun", refresh, "icon_sun", 97 - 63, 733 - 706, 18, 18.23f);
            shop.refreshCostText = Txt(refresh, "Cost", "5", 18, 115 - 63, 727 - 706, 16.93f, 29.02f);

            var start = D("Btn_Start", root, 173, 706, 172, 54);
            startButton = Btn(start, Spr(start, "btn_big_cyan"));
            Txt(start, "Label", "START!", 24, 188 - 173, 720 - 706, 146, 29);
            return shop;
        }

        // ---------------------------------------------------------------------
        // HUD (giống nhau ở 2 frame)
        // ---------------------------------------------------------------------
        static HudController BuildHud(RectTransform safe, GameSession session, out CanvasGroup rewardGroup, out CanvasGroup battleRightGroup)
        {
            var left = Region("HudLeft", safe, new Vector2(0f, 1f));
            var center = Region("HudCenter", safe, new Vector2(0.5f, 1f));
            var right = Region("HudRight", safe, new Vector2(1f, 1f));
            var hud = center.gameObject.AddComponent<HudController>();
            hud.session = session;
            hud.initialProgress = 103f / 275f;

            // Trái
            var set = D("Btn_Settings", left, 14, 71, 25.30f, 28.15f);
            hud.settingsButton = Btn(set, Spr(set, "hud_setting"));
            Pic("SunPanel", left, "panel_sun", 6, 111.05f, 76, 28);
            Pic("SunIcon", left, "icon_sun", 10, 109, 32, 32.42f);
            hud.sunText = Txt(left, "SunText", "165", 18, 42, 114.05f, 38, 24, SunYellow);

            // Giữa
            hud.levelText = Txt(center, "LevelText", "Level 12", 18, 130, 43, 142, 24);
            var prog = Bar("Progress", center, 63, 83, 275, 8, "slider_fill_cyan", hud.initialProgress, out var fill);
            hud.progressFill = fill;
            float[] skullX = { 114, 170, 288 };
            for (int i = 0; i < skullX.Length; i++)
            {
                Pic("Skull_" + (i + 1), center, "icon_skull", skullX[i], 66, 11, 13);
                Pic("Marker_" + (i + 1), center, "marker_triangle", skullX[i] + 8, 87.3f, 5, 7);
            }
            Pic("Flag", center, "icon_flag", 335, 65, 14, 19.07f);
            var badge = NewRect("DistanceBadge", prog);
            badge.anchorMin = badge.anchorMax = new Vector2(hud.initialProgress, 1f);
            badge.pivot = new Vector2(0.5f, 1f);
            badge.anchoredPosition = new Vector2(0, -3f * K);
            badge.sizeDelta = new Vector2(44, 20) * K;
            Spr(badge, "distance_pointer");
            hud.distanceText = Txt(badge, "Text", "123m", 12, 5, 7, 34, 12);
            hud.distanceBadge = badge;
            Pic("HardBadge", center, "badge_hard", 174, 117, 54, 16);
            hud.difficultyText = Txt(center, "HardText", "Hard", 14, 182, 118, 38, 16);

            // Phải
            var close = D("Btn_Close", right, 368, 72, 26.15f, 26.15f);
            hud.closeButton = Btn(close, Spr(close, "hud_close"));
            var x2 = D("Btn_x2", right, 366, 109, 32, 32);
            hud.x2Button = Btn(x2, Spr(x2, "btn_xspeed"));
            hud.x2Label = Txt(x2, "Label", "x2", 16, 3, 5, 26, 24);

            // Nút +50 Sun (chỉ ở trạng thái xếp gear)
            var reward = Stretch("RewardRoot", right);
            rewardGroup = Group(reward);
            var rb = D("Btn_Reward", reward, 274, 153, 120, 54);
            hud.rewardButton = Btn(rb, Spr(rb, "btn_big_yellow"));
            Pic("Sun", rb, "icon_sun", 302 - 274, 164 - 153, 29.02f, 29.4f);
            Txt(rb, "Value", "50", 24, 331 - 274, 167 - 153, 33, 29);
            Pic("Video", rb, "icon_video", 362 - 274, 146 - 153, 32, 24);
            hud.rewardAmount = 50;

            // Cột tài nguyên bên phải vùng combat (chỉ ở combat)
            var res = Stretch("BattleRightRoot", right);
            battleRightGroup = Group(res);
            var rows = new (float y, string bullet, float by, string value, float ty)[]
            {
                (306, "bullet_pumpkin", 311, "2", 314),
                (334, "bullet_chili", 338, "0", 342),
                (362, "bullet_berry", 367, "5", 369),
                (390, "bullet_pea", 395, "13", 398),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                Pic("ResSlot_" + (i + 1), res, "res_slot", 360, rows[i].y, 53, 22);
                Pic("ResIcon_" + (i + 1), res, rows[i].bullet, 366, rows[i].by, 12, 12);
                Txt(res, "ResText_" + (i + 1), rows[i].value, 14, 383, rows[i].ty - 3, 16, 15);
            }
            return hud;
        }

        static GameObject BuildPausePanel(RectTransform canvasRt, out Button resume)
        {
            var p = Stretch("PausePanel", canvasRt);
            var img = p.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.65f);
            img.raycastTarget = true;
            var box = Region("Center", p, new Vector2(0.5f, 0.5f));
            Txt(box, "Title", "PAUSED", 40, 101, 330, 200, 50);
            var btn = D("Btn_Resume", box, 115, 410, 172, 54);
            resume = Btn(btn, Spr(btn, "btn_big_cyan"));
            Txt(btn, "Label", "RESUME", 24, 13, 14, 146, 29);
            p.gameObject.SetActive(false);
            return p.gameObject;
        }
    }
}
