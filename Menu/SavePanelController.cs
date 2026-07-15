using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SavePanelController : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;  // Grid или другой контейнер для слотов
    [SerializeField] private GameObject saveSlotPrefab;  // Префаб для слота сохранения
    [SerializeField] private TextMeshProUGUI statusText;  // Статус операции
    
    private SaveSystem saveSystem;
    private LoadPanelController loadPanelController;
    private int maxSlots = 5;
    
    /*private void Awake()
    {

    }*/
    private void Start()
    {
        saveSystem = FindAnyObjectByType<SaveSystem>();
        loadPanelController = FindAnyObjectByType<LoadPanelController>();
        
        CreateSaveSlots();
    }
    
    private void OnEnable()
    {
        // Обновляем слоты при открытии панели
        RefreshSlots();
    }
    
    private void CreateSaveSlots()
    {
        if (saveSystem == null)
        {
            Debug.LogError("SavePanelController: SaveSystem не найден на сцене!");
            return;
        }
        
        if (slotsContainer == null || saveSlotPrefab == null)
        {
            Debug.LogError("SavePanelController: slotsContainer или saveSlotPrefab не назначены!");
            return;
        }
        
        // Очищаем контейнер
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Создаем слоты
        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotGO = Instantiate(saveSlotPrefab, slotsContainer);
            SaveSlotUI slotUI = slotGO.GetComponent<SaveSlotUI>();
            
            if (slotUI != null)
            {
                SaveSlotInfo info = saveSystem.GetSlotInfo(i);
                slotUI.Initialize(i, info, this, loadPanelController);
            }
        }
    }
    
    private void RefreshSlots()
    {
        if (slotsContainer == null)
            return;
        
        int slotIndex = 0;
        foreach (Transform child in slotsContainer)
        {
            SaveSlotUI slotUI = child.GetComponent<SaveSlotUI>();
            if (slotUI != null && saveSystem != null)
            {
                SaveSlotInfo info = saveSystem.GetSlotInfo(slotIndex);
                slotUI.Refresh(info);
            }
            slotIndex++;
        }
    }
    
    /// <summary>
    /// Сохранить игру в выбранный слот
    /// </summary>
    public void SaveToSlot(int slotIndex)
    {
        if (saveSystem != null)
        {
            saveSystem.SaveGameToSlot(slotIndex);
            
            if (statusText != null)
            {
                statusText.text = $"Сохранено в слот {slotIndex + 1}!";
            }
            
            Debug.Log($"Сохранено в слот {slotIndex}");
            
            // Обновляем отображение слотов
            RefreshSlots();
            
            // Закрываем меню через 1 секунду
            Invoke(nameof(CloseMenu), 1f);
        }
    }
    
    private void CloseMenu()
    {
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        if (menuManager != null)
        {
            menuManager.CloseAllPanels();
            //menuManager.ToggleMenuFromCode();
        }
    }
}
