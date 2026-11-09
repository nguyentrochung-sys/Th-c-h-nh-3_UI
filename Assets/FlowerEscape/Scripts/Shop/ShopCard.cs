using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FlowerEscape
{
    /// <summary>Một thẻ bán gear trong shop (ảnh gear + giá Sun hoặc Free xem video).</summary>
    public class ShopCard : MonoBehaviour
    {
        public Button button;
        public GameObject content;
        public Image gearIcon;
        public TMP_Text valueText;
        public TMP_Text priceText;
        public GameObject sunIcon;
        public GameObject videoIcon;

        public int Value { get; private set; }
        public int Price { get; private set; }
        public bool IsEmpty { get; private set; } = true;
        public bool IsFree => Price <= 0;

        public void SetOffer(int value, int price, GearConfig config)
        {
            IsEmpty = false;
            Value = value;
            Price = price;
            if (content) content.SetActive(true);
            if (config) config.Apply(gearIcon, value);
            if (valueText) valueText.text = value.ToString();
            if (priceText) priceText.text = IsFree ? "Free" : price.ToString();
            if (sunIcon) sunIcon.SetActive(!IsFree);
            if (videoIcon) videoIcon.SetActive(IsFree);
            if (button) button.interactable = true;
        }

        public void SetEmpty()
        {
            IsEmpty = true;
            if (content) content.SetActive(false);
            if (button) button.interactable = false;
        }
    }
}
