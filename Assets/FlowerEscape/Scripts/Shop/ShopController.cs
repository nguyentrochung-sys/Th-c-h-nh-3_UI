using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>
    /// Shop ở trạng thái xếp gear: mua gear bằng Sun, Refresh để đổi hàng.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        public GameSession session;
        public GearBoard board;
        public ShopCard[] cards;
        public Button refreshButton;
        public TMP_Text refreshCostText;

        [Header("Luật shop")]
        public int refreshCost = 5;
        [Tooltip("Số thẻ đang mở bán (thẻ cuối trong thiết kế là ô trống)")]
        public int activeCardCount = 3;
        public int[] offerValues = { 1, 1, 2, 2, 4 };
        public int pricePerValue = 5;
        [Range(0, 1)] public float freeChance = 0.2f;

        void Start()
        {
            foreach (var card in cards)
            {
                var c = card;
                if (c.button) c.button.onClick.AddListener(() => Buy(c));
            }
            if (refreshButton) refreshButton.onClick.AddListener(Refresh);
            if (refreshCostText) refreshCostText.text = refreshCost.ToString();
            SetInitialOffers();
        }

        /// <summary>Hàng ban đầu giống thiết kế Figma: 1 (5 sun), 2 (10 sun), 4 (Free), ô trống.</summary>
        void SetInitialOffers()
        {
            int[] values = { 1, 2, 4 };
            int[] prices = { 5, 10, 0 };
            for (int i = 0; i < cards.Length; i++)
            {
                if (i < activeCardCount && i < values.Length) cards[i].SetOffer(values[i], prices[i], board.config);
                else cards[i].SetEmpty();
            }
        }

        public void Roll()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (i >= activeCardCount) { cards[i].SetEmpty(); continue; }
                int value = offerValues[Random.Range(0, offerValues.Length)];
                int price = Random.value < freeChance ? 0 : value * pricePerValue;
                cards[i].SetOffer(value, price, board.config);
            }
        }

        void Buy(ShopCard card)
        {
            if (card.IsEmpty) return;
            if (!board.HasFreeCell || !session.CanAfford(card.Price))
            {
                StartCoroutine(UIFx.Shake((RectTransform)card.transform));
                return;
            }
            // Price = 0: thẻ "Free" (trong game thật sẽ gọi quảng cáo video ở đây)
            session.TrySpendSun(card.Price);
            board.SpawnGear(card.Value);
            card.SetEmpty();
        }

        void Refresh()
        {
            if (!session.TrySpendSun(refreshCost))
            {
                StartCoroutine(UIFx.Shake((RectTransform)refreshButton.transform));
                return;
            }
            Roll();
        }
    }
}
