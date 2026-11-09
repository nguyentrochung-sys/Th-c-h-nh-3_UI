using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Tự động hoá để chạy lệnh từ bên ngoài Unity (công cụ AI / script):
    /// ghi các lệnh (mỗi dòng 1 lệnh) vào  &lt;project&gt;/Temp/flower_cmd.txt
    ///   refresh  : AssetDatabase.Refresh
    ///   build    : Tools > Flower Escape > Build Gameplay Scene (không hiện hộp thoại)
    ///   capture  : vào Play mode, chụp 2 trạng thái ở 9:21 / 3:4 / 9:16 vào &lt;project&gt;/Captures, rồi thoát Play
    /// Kết quả ghi vào &lt;project&gt;/Temp/flower_result.txt
    /// Cũng có thể chụp bằng tay: Tools > Flower Escape > Capture Screenshots.
    /// </summary>
    [InitializeOnLoad]
    public static class FlowerEscapeAutomation
    {
        static readonly string ProjectDir = Directory.GetParent(Application.dataPath).FullName;
        static readonly string CmdFile = Path.Combine(ProjectDir, "Temp", "flower_cmd.txt");
        static readonly string ResultFile = Path.Combine(ProjectDir, "Temp", "flower_result.txt");
        static readonly string CaptureDir = Path.Combine(ProjectDir, "Captures");
        const string CaptureFlag = "FlowerEscape.CapturePending";
        const string GameplayScene = "Assets/FlowerEscape/Scenes/Gameplay.unity";

        static double nextPoll;
        static int playFrames;

        static readonly (int w, int h, string name)[] Sizes =
        {
            (1080, 2520, "9x21"),
            (1536, 2048, "3x4"),
            (1080, 1920, "9x16"),
        };

        static FlowerEscapeAutomation()
        {
            EditorApplication.update += OnUpdate;
        }

        static void Log(string msg)
        {
            Debug.Log("[Flower Escape Auto] " + msg);
            try { File.AppendAllText(ResultFile, DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n"); } catch { }
        }

        static void OnUpdate()
        {
            // Đang chờ chụp trong Play mode
            if (EditorApplication.isPlaying && SessionState.GetBool(CaptureFlag, false))
            {
                if (++playFrames < 45) return; // đợi Start() chạy xong, layout ổn định
                SessionState.SetBool(CaptureFlag, false);
                playFrames = 0;
                try { CaptureAll(); }
                catch (Exception e) { Log("ERROR capture: " + e); }
                EditorApplication.ExitPlaymode();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            if (!File.Exists(CmdFile)) return;

            string[] cmds;
            try
            {
                cmds = File.ReadAllLines(CmdFile);
                File.Delete(CmdFile);
            }
            catch { return; }

            foreach (var raw in cmds)
            {
                string cmd = raw.Trim().ToLowerInvariant();
                if (cmd.Length == 0) continue;
                try
                {
                    switch (cmd)
                    {
                        case "refresh":
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                            Log("refresh OK");
                            break;
                        case "build":
                            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                            Log("build:\n" + FlowerEscapeSceneBuilder.Build(false));
                            break;
                        case "capture":
                            StartCapture();
                            break;
                        case "selftest":
                            FlowerEscapeSelfTest.Run();
                            break;
                        default:
                            Log("Lệnh không hiểu: " + cmd);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log("ERROR " + cmd + ": " + e);
                }
            }
        }

        [MenuItem("Tools/Flower Escape/Capture Screenshots (9-21, 3-4, 9-16)", false, 30)]
        public static void StartCapture()
        {
            if (!File.Exists(GameplayScene))
            {
                Log("ERROR: chưa có scene Gameplay, hãy Build trước");
                return;
            }
            if (EditorSceneManager.GetActiveScene().path != GameplayScene)
                EditorSceneManager.OpenScene(GameplayScene);
            SessionState.SetBool(CaptureFlag, true);
            playFrames = 0;
            Log("capture: vào Play mode...");
            EditorApplication.EnterPlaymode();
        }

        static void CaptureAll()
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<GameplayUIController>();
            if (ui == null) { Log("ERROR: không thấy GameplayUIController"); return; }
            Directory.CreateDirectory(CaptureDir);

            var canvas = ui.GetComponent<Canvas>();
            var scaler = ui.GetComponent<CanvasScaler>();
            var responsive = ui.GetComponent<ResponsiveCanvas>();
            var canvasRt = (RectTransform)canvas.transform;
            GameplayState original = ui.State;

            var camGo = new GameObject("~CaptureCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.enabled = false;
            camGo.transform.position = new Vector3(0, 0, -100);

            RenderMode oldMode = canvas.renderMode;
            Camera oldCam = canvas.worldCamera;
            var oldScaleMode = scaler.uiScaleMode;

            try
            {
                foreach (var (w, h, name) in Sizes)
                {
                    var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                    cam.targetTexture = rt;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 10;
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    scaler.scaleFactor = Mathf.Min(w / 1080f, h / 2348f); // = công thức của Expand (ref 1080x2348)
                    if (responsive) responsive.EvaluateFor(w, h);

                    foreach (GameplayState state in new[] { GameplayState.GearSetup, GameplayState.Combat })
                    {
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRt);
                        ui.PreviewState(state);
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRt);
                        Canvas.ForceUpdateCanvases();

                        cam.Render();
                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                        tex.Apply();
                        RenderTexture.active = prev;
                        string file = Path.Combine(CaptureDir, $"{name}_{state}.png");
                        File.WriteAllBytes(file, tex.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(tex);
                        Log($"saved {file}  (canvas {canvasRt.rect.width:0}x{canvasRt.rect.height:0})");
                    }
                    cam.targetTexture = null;
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
            finally
            {
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCam;
                scaler.uiScaleMode = oldScaleMode;
                UnityEngine.Object.DestroyImmediate(camGo);
                ui.PreviewState(original);
            }
            Log("capture DONE");
        }
    }
}
