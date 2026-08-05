using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Управляет панелью игрового меню: Load / Save / Settings и
/// вложенными под-панелями настроек (смена рабочего стола, смена имён, читы).
///
/// Реализует INavigableScreen, чтобы кнопка "назад" на телефоне
/// (PhoneNavigationManager) сначала закрывала верхнюю открытую панель,
/// и только если панелей нет — уходила на предыдущий экран телефона.
///
/// ВАЖНО: этот скрипт должен висеть на том же GameObject, который в
/// PhoneNavigationManager указан как gameMenuScreen — иначе
/// currentScreen.TryGetComponent&lt;INavigableScreen&gt;() его не найдёт.
/// </summary>
public class MenuManager : MonoBehaviour, INavigableScreen, ISlotActionHost
{
    [Header("Панели верхнего уровня")]
    [SerializeField] private GameObject savePanel;   // панель со слотами сохранений
    [SerializeField] private GameObject settingsPanel;

    [Header("Кнопки главного меню")]
    [Tooltip("Кнопка, открывающая панель со слотами сохранений")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Кнопки закрытия панелей верхнего уровня")]
    [SerializeField] private Button closeSaveButton;
    [SerializeField] private Button closeSettingsButton;

    [Header("Панель действий над выбранным слотом")]
    [SerializeField] private GameObject slotActionPanel;
    [SerializeField] private Button saveSlotActionButton;
    [SerializeField] private Button loadSlotActionButton;
    [SerializeField] private Button deleteSlotActionButton;
    [SerializeField] private Button closeSlotActionButton;
    [Tooltip("Необязательно: заголовок в панели действий (например: 'Слот 3')")]
    [SerializeField] private TextMeshProUGUI slotActionTitleText;

    [Header("Под-панели настроек (открываются поверх settingsPanel)")]
    [SerializeField] private GameObject changeDesktopPanel;
    [SerializeField] private GameObject changeNamesPanel;

    [Header("Кнопки внутри панели настроек")]
    [SerializeField] private Button changeDesktopButton;
    [SerializeField] private Button changeNamesButton;
    [SerializeField] private Toggle cheatsToggle;

    [Header("Настройки Toggle")]
    //[SerializeField] private Image toggleImage;          // ссылка на Image, который будет менять спрайт
    [SerializeField] private Sprite toggleOnSprite;     // спрайт для состояния "включено"
    [SerializeField] private Sprite toggleOffSprite;    // спрайт для состояния "выключено"

    [Header("Кнопки закрытия под-панелей настроек")]
    //[SerializeField] private Button closeChangeDesktopButton;
    //[SerializeField] private Button closeChangeNamesButton;

    // Стек открытых панелей. Нужен, чтобы кнопка "назад" могла закрывать
    // их последовательно: ChangeDesktop -> Settings -> (базовое меню).
    private readonly Stack<GameObject> panelHistory = new();
    private GameObject currentPanel; // null = ни одна панель не открыта

    private int _selectedSlotIndex = -1;
    private SaveSystem _saveSystem;
    private SavePanelController _savePanelController;

    public bool CheatsEnabled { get; private set; }

    private void OnEnable()
    {
        // Каждый раз, когда экран меню становится активным
        // (например, PhoneNavigationManager.OpenGameMenu), начинаем с чистого листа.
        ResetToRoot();
    }

    private void Start()
    {
        _saveSystem = FindAnyObjectByType<SaveSystem>();
        _savePanelController = FindAnyObjectByType<SavePanelController>();

        if (saveButton != null) saveButton.onClick.AddListener(() => OpenPanel(savePanel));
        if (settingsButton != null) settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        if (closeSaveButton != null) closeSaveButton.onClick.AddListener(ClosePanel);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(ClosePanel);

        if (saveSlotActionButton != null) saveSlotActionButton.onClick.AddListener(SaveSelectedSlot);
        if (loadSlotActionButton != null) loadSlotActionButton.onClick.AddListener(LoadSelectedSlot);
        if (deleteSlotActionButton != null) deleteSlotActionButton.onClick.AddListener(DeleteSelectedSlot);
        if (closeSlotActionButton != null) closeSlotActionButton.onClick.AddListener(ClosePanel);

        changeDesktopButton.onClick.AddListener(() => OpenPanel(changeDesktopPanel));
        changeNamesButton.onClick.AddListener(() => OpenPanel(changeNamesPanel));
        //closeChangeDesktopButton.onClick.AddListener(ClosePanel);
        //closeChangeNamesButton.onClick.AddListener(ClosePanel);

        if (cheatsToggle != null)
        {
            cheatsToggle.isOn = CheatsEnabled;
            cheatsToggle.onValueChanged.AddListener(OnCheatsToggled);
            // Устанавливаем начальный спрайт
            UpdateToggleSprite(CheatsEnabled);
        }
    }

    /// <summary>
    /// Открыть панель поверх текущей. Текущая (если есть) уходит в историю,
    /// чтобы вернуться к ней можно было кнопкой "назад" или крестиком закрытия.
    /// </summary>
    public void OpenPanel(GameObject panel)
    {
        if (panel == null || panel == currentPanel) return;

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
            panelHistory.Push(currentPanel);
        }

        panel.SetActive(true);
        currentPanel = panel;
    }

    private void UpdateToggleSprite(bool isOn)
    {
        if (cheatsToggle.GetComponent<Image>() != null)
        {
            cheatsToggle.GetComponent<Image>().sprite = isOn ? toggleOnSprite : toggleOffSprite;
        }
    }

    private void OnCheatsToggled(bool value)
    {
        CheatsEnabled = value;
        UpdateToggleSprite(value);
        // TODO: применить/отключить читы в игровой логике здесь
    }
    /// <summary>
    /// Закрыть текущую панель и вернуться на предыдущую из истории
    /// (или на базовое меню, если истории больше нет).
    /// Вешается на все кнопки закрытия панелей (X).
    /// </summary>
    public void ClosePanel()
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        currentPanel = panelHistory.Count > 0 ? panelHistory.Pop() : null;

        if (currentPanel != null)
            currentPanel.SetActive(true);
    }

    /// <summary>
    /// Открыть панель действий для конкретного слота (Сохранить / Загрузить).
    /// Вызывается из SaveSlotUI при нажатии на ячейку слота.
    /// </summary>
    public void OpenSlotActionPanel(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;

        // Показываем номер слота в заголовке панели действий
        if (slotActionTitleText != null)
            slotActionTitleText.text = $"Слот {slotIndex + 1}";

        // Кнопка загрузки недоступна, если слот пустой
        if (loadSlotActionButton != null && _saveSystem != null)
        {
            SaveSlotInfo info = _saveSystem.GetSlotInfo(slotIndex);
            loadSlotActionButton.interactable = info != null && !info.isEmpty;
        }

        // Кнопка удаления недоступна, если слот пустой
        if (deleteSlotActionButton != null && _saveSystem != null)
        {
            SaveSlotInfo info = _saveSystem.GetSlotInfo(slotIndex);
            deleteSlotActionButton.interactable = info != null && !info.isEmpty;
        }

        OpenPanel(slotActionPanel);
    }

    private void SaveSelectedSlot()
    {
        if (_selectedSlotIndex < 0 || _saveSystem == null) return;
        _saveSystem.SaveGameToSlot(_selectedSlotIndex);
        ClosePanel(); // закрыть панель действий, вернуться к слотам
    }

    private void LoadSelectedSlot()
    {
        if (_selectedSlotIndex < 0 || _saveSystem == null) return;
        _saveSystem.LoadGameFromSlot(_selectedSlotIndex);
        ClosePanel();
    }

    private void DeleteSelectedSlot()
    {
        if (_selectedSlotIndex < 0 || _saveSystem == null) return;
        _saveSystem.DeleteSlot(_selectedSlotIndex);
        _savePanelController?.RefreshSlots(); // ячейка сразу отобразится пустой
        ClosePanel();
    }

    /// <summary>Жёстко закрыть все панели и сбросить историю (используется при открытии/закрытии меню).</summary>
    public void CloseAllPanels()
    {
        if (savePanel != null) savePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (changeDesktopPanel != null) changeDesktopPanel.SetActive(false);
        if (changeNamesPanel != null) changeNamesPanel.SetActive(false);
        if (slotActionPanel != null) slotActionPanel.SetActive(false);

        panelHistory.Clear();
        currentPanel = null;
    }

    private void ResetToRoot() => CloseAllPanels();

    // ──────────────────────────────────────────────
    // INavigableScreen
    // Вызывается из PhoneNavigationManager.GoBack(), если этот компонент
    // найден на текущем активном экране (gameMenuScreen).
    // ──────────────────────────────────────────────
    public bool TryHandleBack()
    {
        if (currentPanel == null)
            return false; // открытых панелей нет — пусть телефон сам решает, что делать с "назад"

        ClosePanel();
        return true; // обработали сами: закрыли верхнюю панель, сам экран меню остался открыт
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}