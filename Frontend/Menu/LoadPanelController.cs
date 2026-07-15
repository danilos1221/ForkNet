using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class LoadPanelController : MonoBehaviour
{
    [SerializeField] private Transform slotsContainer;  // Grid или другой контейнер для слотов
    [SerializeField] private GameObject saveSlotPrefab;  // Префаб для слота сохранения
    [SerializeField] private TextMeshProUGUI statusText;  // Для отображения статуса загрузки
    
    private SaveSystem saveSystem;
    private SavePanelController savePanelController;
    private int maxSlots = 5;
    
    private void Start()
    {
        saveSystem = FindAnyObjectByType<SaveSystem>();
        savePanelController = FindAnyObjectByType<SavePanelController>();
        
        CreateLoadSlots();
    }
    
    private void OnEnable()
    {
        // Обновляем слоты при открытии панели
        RefreshSlots();
    }
    
    private void CreateLoadSlots()
    {
        if (saveSystem == null)
        {
            Debug.LogError("LoadPanelController: SaveSystem не найден на сцене!");
            return;
        }
        
        if (slotsContainer == null || saveSlotPrefab == null)
        {
            Debug.LogError("LoadPanelController: slotsContainer или saveSlotPrefab не назначены!");
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
                slotUI.Initialize(i, info, savePanelController, this);
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
    /// Загрузить игру из выбранного слота
    /// </summary>
    public void LoadFromSlot(int slotIndex)
    {
        if (saveSystem != null)
        {
            saveSystem.LoadGameFromSlot(slotIndex);
            
            if (statusText != null)
            {
                statusText.text = $"Загружено из слота {slotIndex + 1}!";
            }
            
            Debug.Log($"Загружено из слота {slotIndex}");
            
            // Закрываем меню после загрузки
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
