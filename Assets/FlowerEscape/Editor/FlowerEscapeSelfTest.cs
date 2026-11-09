using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Menu: Tools > Flower Escape > Run Self Test (Play mode)
    /// Tự chơi thử luồng chính và ghi kết quả + ảnh vào &lt;project&gt;/Captures:
    ///  1) gộp 2 gear "1" -> "2"   2) di chuyển gear vào ô trống   3) mua gear ở shop
    ///  4) bấm START -> Combat     5) đợi wave xong -> tự về Xếp gear, được cộng Sun
    /// </summary>
    [InitializeOnLoad]
    public static class FlowerEscapeSelfTest
    {
        const string Flag = "FlowerEscape.SelfTestStep";
        const string GameplayScene = "Assets/FlowerEscape/Scenes/Gameplay.unity";
        static readonly string ProjectDir = Directory.GetParent(Application.dataPath).FullName;
        static readonly string ResultFile = Path.Combine(ProjectDir, "Temp", "flower_result.txt");
        static readonly string CaptureDir = Path.Combine(ProjectDir, "Captures");

        static int frames;
        static double stepTime;
        static int passed, failed;

        static FlowerEscapeSelfTest()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/Flower Escape/Run Self Test (Play mode)", false, 31)]
        public static void Run()
        {
            if (EditorSceneManager.GetActiveScene().path != GameplayScene)
                EditorSceneManager.OpenScene(GameplayScene);
            SessionState.SetInt(Flag, 1);
            frames = 0;
            Log("selftest: vào Play mode...");
            EditorApplication.EnterPlaymode();
        }

        static void Log(string msg)
        {
            Debug.Log("[Flower Escape Test] " + msg);
            try { File.AppendAllText(ResultFile, DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n"); } catch { }
        }

        static void Check(bool ok, string what)
        {
            if (ok) passed++; else failed++;
            Log((ok ? "PASS  " : "FAIL  ") + what);
        }

        static void Tick()
        {
            int step = SessionState.GetInt(Flag, 0);
            if (step == 0 || !EditorApplication.isPlaying) return;
            var ui = UnityEngine.Object.FindFirstObjectByType<GameplayUIController>();
            if (ui == null) return;
            var board = ui.gearBoard;
            var session = UnityEngine.Object.FindFirstObjectByType<GameSession>();

            try
            {
                switch (step)
                {
                    case 1: // đợi Start()
                        if (++frames < 45) return;
                        passed = failed = 0;
                        Check(ui.State == GameplayState.GearSetup, "Bắt đầu ở trạng thái Xếp gear");
                        Check(board.Cells.Count == 24, $"Board có 24 ô (6x4): {board.Cells.Count}");
                        Check(board.TotalPower == 23, $"Bố cục Figma: tổng giá trị gear = 23 ({board.TotalPower})");

                        // 1) Gộp: ô (2,1)=1 kéo vào ô (4,1)=1
                        var a = board.Cells[1 * 6 + 2];
                        var b = board.Cells[1 * 6 + 4];
                        bool merged = a.Item != null && b.Item != null && a.Item.Value == 1 && b.Item.Value == 1
                                      && board.HandleDrop(a.Item, b);
                        Check(merged && a.Item == null && b.Item != null && b.Item.Value == 2, "Gộp 1 + 1 = 2");

                        // 2) Di chuyển gear (3,2)=2 vào ô trống (2,2)
                        var c = board.Cells[2 * 6 + 3];
                        var d = board.Cells[2 * 6 + 2];
                        bool moved = c.Item != null && d.Item == null && board.HandleDrop(c.Item, d);
                        Check(moved && c.Item == null && d.Item != null && d.Item.Value == 2, "Di chuyển gear vào ô trống");

                        // 3) Đổi chỗ gear thường với gear đặc biệt (không gộp)
                        var e = board.Cells[0 * 6 + 0]; // 2
                        var f = board.Cells[0 * 6 + 4]; // đặc biệt 'a'
                        bool swapped = e.Item != null && f.Item != null && f.Item.IsSpecial && board.HandleDrop(e.Item, f);
                        Check(swapped && e.Item != null && e.Item.IsSpecial && f.Item.Value == 2, "Đổi chỗ với gear đặc biệt");

                        // 4) Mua thẻ 1 (gear 1, giá 5)
                        int sun0 = session.Sun;
                        int free0 = CountFree(board);
                        ui.shop.cards[0].button.onClick.Invoke();
                        Check(session.Sun == sun0 - 5 && CountFree(board) == free0 - 1, $"Mua gear ở shop: Sun {sun0} -> {session.Sun}");

                        Capture(ui, "test_1_gear_after_actions.png");
                        stepTime = EditorApplication.timeSinceStartup;
                        ui.startButton.onClick.Invoke();
                        SessionState.SetInt(Flag, 2);
                        break;

                    case 2: // giữa combat: đợi animation chuyển cảnh xong
                        bool settled = ui.boosterGroup == null || ui.boosterGroup.alpha > 0.99f;
                        if (!settled && EditorApplication.timeSinceStartup - stepTime < 15) return;
                        Check(ui.State == GameplayState.Combat && ui.combat.IsRunning, "Bấm START -> Combat đang chạy");
                        Check(!board.Interactable, "Board bị khoá kéo thả khi Combat");
                        Capture(ui, "test_2_combat_running.png");
                        Time.timeScale = 4f; // tăng tốc cho nhanh (Editor chạy chậm khi không focus)
                        stepTime = EditorApplication.timeSinceStartup;
                        SessionState.SetInt(Flag, 3);
                        break;

                    case 3: // đợi wave xong
                        bool done = ui.State == GameplayState.GearSetup && !ui.combat.IsRunning
                                    && (ui.shopGroup == null || ui.shopGroup.alpha > 0.99f);
                        if (!done && EditorApplication.timeSinceStartup - stepTime < 90) return;
                        Time.timeScale = 1f;
                        Check(session.Sun == 160 + ui.combat.rewardPerWave, $"Nhận thưởng wave: Sun = {session.Sun}");
                        Check(done, "Hết wave -> tự quay về Xếp gear");
                        Check(board.Interactable, "Board mở khoá kéo thả lại");
                        Capture(ui, "test_3_back_to_gear.png");
                        Log($"selftest DONE: {passed} PASS, {failed} FAIL");
                        SessionState.SetInt(Flag, 0);
                        EditorApplication.ExitPlaymode();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("ERROR selftest: " + ex);
                SessionState.SetInt(Flag, 0);
                EditorApplication.ExitPlaymode();
            }
        }

        static int CountFree(GearBoard board)
        {
            int n = 0;
            foreach (var c in board.Cells) if (c.IsFree) n++;
            return n;
        }

        /// <summary>Chụp trạng thái hiện tại ở 1080x2520 (9:21).</summary>
        static void Capture(GameplayUIController ui, string fileName)
        {
            const int w = 1080, h = 2520;
            Directory.CreateDirectory(CaptureDir);
            var canvas = ui.GetComponent<Canvas>();
            var scaler = ui.GetComponent<CanvasScaler>();
            var camGo = new GameObject("~TestCaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.enabled = false;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var oldMode = canvas.renderMode;
            var oldScale = scaler.uiScaleMode;
            try
            {
                cam.targetTexture = rt;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = Mathf.Min(w / 1080f, h / 2348f);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)canvas.transform);
                Canvas.ForceUpdateCanvases();
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                File.WriteAllBytes(Path.Combine(CaptureDir, fileName), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Log("saved " + fileName);
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = null;
                scaler.uiScaleMode = oldScale;
                cam.targetTexture = null;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
        }
    }
}
