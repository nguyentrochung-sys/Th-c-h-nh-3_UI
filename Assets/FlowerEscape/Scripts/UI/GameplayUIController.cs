using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape
{
    public enum GameplayState
    {
        GearSetup, // Figma "Main - Buy gear": board to ở giữa, shop tím dưới đáy, nút +50, ẩn vùng combat
        Combat     // Figma "Main": board thu nhỏ xuống đáy, cột booster bên phải, quái + thuyền hiện ra
    }

    /// <summary>
    /// Điều phối màn gameplay chính ở 2 trạng thái.
    ///
    /// Hai trạng thái dùng CHUNG một bộ UI. Vị trí/scale của board ở mỗi trạng thái lấy đúng
    /// từ 2 frame Figma (board combat = board xếp gear x 0.827). Chuyển trạng thái = nội suy
    /// một tham số t (0 = GearSetup, 1 = Combat) cho vị trí board, shop, booster và độ mờ các khối.
    ///
    /// Responsive: CanvasScaler Expand (1080x2348 = frame Figma 402x874 x2.687) + các vùng neo
    /// trên/dưới/trái/phải (xem FlowerEscapeSceneBuilder), nên controller không phải tính lại layout.
    /// </summary>
    public class GameplayUIController : MonoBehaviour
    {
        [Header("Systems")]
        public GearBoard gearBoard;
        public ShopController shop;
        public CombatSimulator combat;
        public HudController hud;

        [Header("Board (vị trí trong vùng đáy, đơn vị canvas)")]
        public RectTransform boardRoot;
        public Vector2 gearBoardPos;
        public float gearBoardScale = 1f;
        public Vector2 combatBoardPos;
        public float combatBoardScale = 0.827f;

        [Header("Khối chỉ có ở trạng thái Xếp gear")]
        public RectTransform shopRoot;
        public CanvasGroup shopGroup;
        [Tooltip("Shop trượt xuống bao nhiêu khi sang Combat")]
        public float shopSlide = 560f;
        public CanvasGroup rewardGroup;

        [Header("Khối chỉ có ở trạng thái Combat")]
        public RectTransform boosterRoot;
        public CanvasGroup boosterGroup;
        public float boosterSlide = 220f;
        public CanvasGroup battleGroup;
        public CanvasGroup battleRightGroup;

        [Header("UI")]
        public Button startButton;
        public TMP_Text damageText;

        [Header("Chuyển trạng thái")]
        public float transitionDuration = 0.45f;
        public GameplayState startState = GameplayState.GearSetup;

        public GameplayState State { get; private set; }
        public event Action<GameplayState> StateChanged;

        float t;            // 0 = GearSetup, 1 = Combat
        Coroutine transition;

        void Start()
        {
            if (startButton) startButton.onClick.AddListener(OnStartPressed);
            if (combat) combat.Finished += OnCombatFinished;
            if (gearBoard) gearBoard.Changed += RefreshStats;

            SetState(startState, false);
            RefreshStats();
        }

        void OnDestroy()
        {
            if (combat) combat.Finished -= OnCombatFinished;
            if (gearBoard) gearBoard.Changed -= RefreshStats;
        }

        void OnStartPressed()
        {
            if (State != GameplayState.GearSetup) return;
            SetState(GameplayState.Combat);
            if (combat) combat.Begin();
        }

        void OnCombatFinished()
        {
            SetState(GameplayState.GearSetup);
            if (shop) shop.Roll();
        }

        public void SetState(GameplayState state, bool animate = true)
        {
            State = state;
            bool gear = state == GameplayState.GearSetup;

            if (gearBoard) gearBoard.Interactable = gear;
            SetInteractive(shopGroup, gear);
            SetInteractive(rewardGroup, gear);
            SetInteractive(boosterGroup, !gear);
            SetInteractive(battleGroup, !gear);
            SetInteractive(battleRightGroup, !gear);
            if (hud) hud.OnStateChanged(state);

            float target = gear ? 0f : 1f;
            if (transition != null) StopCoroutine(transition);
            transition = null;
            if (animate && isActiveAndEnabled && Application.isPlaying)
                transition = StartCoroutine(Animate(t, target));
            else
                Apply(target);

            StateChanged?.Invoke(state);
        }

        static void SetInteractive(CanvasGroup g, bool on)
        {
            if (!g) return;
            g.interactable = on;
            g.blocksRaycasts = on;
        }

        IEnumerator Animate(float from, float to)
        {
            float k = 0f;
            while (k < 1f)
            {
                k += Time.unscaledDeltaTime / Mathf.Max(0.01f, transitionDuration);
                Apply(Mathf.Lerp(from, to, UIFx.EaseInOut(Mathf.Clamp01(k))));
                yield return null;
            }
            Apply(to);
            transition = null;
        }

        /// <summary>Dàn UI theo t: 0 = đúng frame "Main - Buy gear", 1 = đúng frame "Main".</summary>
        public void Apply(float value)
        {
            t = value;
            if (boardRoot)
            {
                boardRoot.anchoredPosition = Vector2.Lerp(gearBoardPos, combatBoardPos, t);
                boardRoot.localScale = Vector3.one * Mathf.Lerp(gearBoardScale, combatBoardScale, t);
            }
            if (shopRoot) shopRoot.anchoredPosition = new Vector2(0f, -shopSlide * t);
            if (shopGroup) shopGroup.alpha = 1f - t;
            if (rewardGroup) rewardGroup.alpha = 1f - t;
            if (boosterRoot) boosterRoot.anchoredPosition = new Vector2(boosterSlide * (1f - t), 0f);
            if (boosterGroup) boosterGroup.alpha = t;
            if (battleGroup) battleGroup.alpha = t;
            if (battleRightGroup) battleRightGroup.alpha = t;
        }

        void RefreshStats()
        {
            if (damageText && gearBoard) damageText.text = (gearBoard.TotalPower * 3.187f).ToString("0.0") /* 23 x 3.187 = 73.3 như Figma */;
        }

        // ---------------- Xem trước trong Editor ----------------

        [ContextMenu("Preview: Gear Setup")]
        void PreviewGear() => PreviewState(GameplayState.GearSetup);

        [ContextMenu("Preview: Combat")]
        void PreviewCombat() => PreviewState(GameplayState.Combat);

        public void PreviewState(GameplayState state)
        {
            State = state;
            Apply(state == GameplayState.GearSetup ? 0f : 1f);
        }
    }
}
