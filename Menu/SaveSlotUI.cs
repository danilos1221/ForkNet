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
    private SaveSlotInfo slotInfo;
    private SavePanelController savePanelController;
    private LoadPanelController loadPanelController;
    
    public void Initialize(int index, SaveSlotInfo info, SavePanelController savePanel, LoadPanelController loadPanel)
    {
        slotIndex = index;
        slotInfo = info;
        savePanelController = savePanel;
        loadPanelController = loadPanel;
        
        UpdateDisplay();
        
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }
    
    private void UpdateDisplay()
    {
        if (slotNumberText != null)
        {
            slotNumberText.text = $"Слот {slotIndex + 1}";
        }
        
        if (dateText != null)
        {
            dateText.text = slotInfo.isEmpty ? "Пусто" : slotInfo.saveDate;
        }
        
        // Изменяем цвет в зависимости от того, пусто ли сохранение
        if (slotImage != null)
        {
            slotImage.color = slotInfo.isEmpty ? emptySlotColor : filledSlotColor;
        }
    }
    
    private void OnSlotClicked()
    {
        // Определяем, находимся ли мы в панели сохранения или загрузки
        // На основе того, какой контроллер не null, выполняем действие
        if (savePanelController != null && savePanelController.gameObject.activeInHierarchy)
        {
            savePanelController.SaveToSlot(slotIndex);
        }
        else if (loadPanelController != null && loadPanelController.gameObject.activeInHierarchy)
        {
            if (!slotInfo.isEmpty)
            {
                loadPanelController.LoadFromSlot(slotIndex);
            }
            else
            {
                Debug.LogWarning("Нельзя загрузить пустой слот!");
            }
        }
    }
    
    public void Refresh(SaveSlotInfo info)
    {
        slotInfo = info;
        UpdateDisplay();
    }
}
