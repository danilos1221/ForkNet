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
public class MenuManager : MonoBehaviour, INavigableScreen
{
    [Header("Панели верхнего уровня")]
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject savePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Кнопки главного меню")]
    [SerializeField] private Button loadButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Кнопки закрытия панелей верхнего уровня")]
    [SerializeField] private Button closeLoadButton;
    [SerializeField] private Button closeSaveButton;
    [SerializeField] private Button closeSettingsButton;

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

    public bool CheatsEnabled { get; private set; }

    private void OnEnable()
    {
        // Каждый раз, когда экран меню становится активным
        // (например, PhoneNavigationManager.OpenGameMenu), начинаем с чистого листа.
        ResetToRoot();
    }

    private void Start()
    {
        loadButton.onClick.AddListener(() => OpenPanel(loadPanel));
        saveButton.onClick.AddListener(() => OpenPanel(savePanel));
        settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
        exitButton.onClick.AddListener(ExitGame);

        closeLoadButton.onClick.AddListener(ClosePanel);
        closeSaveButton.onClick.AddListener(ClosePanel);
        closeSettingsButton.onClick.AddListener(ClosePanel);

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

    /// <summary>Жёстко закрыть все панели и сбросить историю (используется при открытии/закрытии меню).</summary>
    public void CloseAllPanels()
    {
        loadPanel.SetActive(false);
        savePanel.SetActive(false);
        settingsPanel.SetActive(false);
        changeDesktopPanel.SetActive(false);
        changeNamesPanel.SetActive(false);

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