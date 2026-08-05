using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI slotNumberText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image slotImage;
    [SerializeField] private Color emptySlotColor = Color.gray;
    [SerializeField] private Color filledSlotColor = Color.white;

    private int slotIndex;
    private ISlotActionHost slotActionHost;

    public void Initialize(int index, SaveSlotInfo info, ISlotActionHost host)
    {
        slotIndex = index;
        slotActionHost = host;

        slotButton?.onClick.AddListener(OnSlotClicked);
        Refresh(info);
    }

    public void Refresh(SaveSlotInfo info)
    {
        if (slotNumberText != null)
            slotNumberText.text = $"Слот {slotIndex + 1}";

        if (dateText != null)
            dateText.text = info.isEmpty ? "Пусто" : info.saveDate;

        if (slotImage != null)
            slotImage.color = info.isEmpty ? emptySlotColor : filledSlotColor;
    }

    private void OnSlotClicked()
    {
        slotActionHost?.OpenSlotActionPanel(slotIndex);
    }
}
