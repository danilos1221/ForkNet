using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour, ISlotActionHost, INavigableScreen
{
    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject settingsPanel;

    [SerializeField] private Button loadButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button patreonButton;

    [Header("Панель действий над выбранным слотом (Сохранить/Загрузить/Удалить)")]
    [SerializeField] private GameObject slotActionPanel;
    [SerializeField] private Button saveSlotActionButton;
    [SerializeField] private Button loadSlotActionButton;
    [SerializeField] private Button deleteSlotActionButton;
    [SerializeField] private Button closeSlotActionButton;
    [SerializeField] private TMP_Text slotActionTitleText;

    [SerializeField] private Button startGameButton;

    [SerializeField] private string gameSceneName = "Game";
    [SerializeField] private string patreonUrl;

    // Стек открытых панелей (Load / Settings), как в MenuManager —
    // чтобы поведение открытия/закрытия было единообразным по проекту.
    private readonly Stack<GameObject> panelHistory = new();
    private GameObject currentPanel;

    private int _selectedSlotIndex = -1;
    private SaveSystem _saveSystem;
    private SavePanelController _savePanelController;

    private void Start()
    {
        CloseAllPanels();

        _saveSystem = FindAnyObjectByType<SaveSystem>();
        _savePanelController = FindAnyObjectByType<SavePanelController>();

        // Кнопки меню
        loadButton.onClick.AddListener(() => OpenPanel(loadPanel));
        settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
        exitButton.onClick.AddListener(ExitGame);
        if (patreonButton != null) patreonButton.onClick.AddListener(OpenPatreonLink);


        // Кнопки панели действий над слотом (Сохранить/Загрузить/Удалить)
        if (saveSlotActionButton != null) saveSlotActionButton.onClick.AddListener(SaveSelectedSlot);
        if (loadSlotActionButton != null) loadSlotActionButton.onClick.AddListener(LoadSelectedSlot);
        if (deleteSlotActionButton != null) deleteSlotActionButton.onClick.AddListener(DeleteSelectedSlot);
        if (closeSlotActionButton != null) closeSlotActionButton.onClick.AddListener(ClosePanel);

        startGameButton.onClick.AddListener(StartNewGame);
    }

    /// <summary>
    /// Открыть панель действий для конкретного слота (Сохранить / Загрузить / Удалить).
    /// Вызывается из SaveSlotUI при нажатии на ячейку слота (через интерфейс ISlotActionHost).
    /// </summary>
    public void OpenSlotActionPanel(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;

        if (slotActionTitleText != null)
            slotActionTitleText.text = $"Слот {slotIndex + 1}";

        if (loadSlotActionButton != null && _saveSystem != null)
        {
            SaveSlotInfo info = _saveSystem.GetSlotInfo(slotIndex);
            loadSlotActionButton.interactable = info != null && !info.isEmpty;
        }

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
        ClosePanel();
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
        _savePanelController?.RefreshSlots();
        ClosePanel();
    }

    private void StartNewGame()
    {
        if (string.IsNullOrEmpty(GameManager.Instance.nickname))
            GameManager.Instance.nickname = "Player";

        SceneManager.LoadScene(gameSceneName);
    }

    private void OpenPatreonLink()
    {
        if (!string.IsNullOrEmpty(patreonUrl))
            Application.OpenURL(patreonUrl);
    }

    /// <summary>
    /// Открыть панель (Load / Settings и т.п.) поверх текущей.
    /// Текущая (если есть) уходит в историю — для симметрии с ClosePanel().
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

    /// <summary>Закрыть текущую панель и вернуться на предыдущую из истории (если есть).</summary>
    public void ClosePanel()
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        currentPanel = panelHistory.Count > 0 ? panelHistory.Pop() : null;

        if (currentPanel != null)
            currentPanel.SetActive(true);
    }

    public void CloseAllPanels()
    {
        loadPanel.SetActive(false);
        settingsPanel.SetActive(false);
        if (slotActionPanel != null) slotActionPanel.SetActive(false);

        panelHistory.Clear();
        currentPanel = null;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ──────────────────────────────────────────────
    // INavigableScreen
    // PhoneNavigationManager.GoBack() ищет этот компонент на currentScreen
    // (= homeScreen = экран главного меню). Поэтому MainMenuManager должен
    // висеть на том же GameObject, что указан в PhoneNavigationManager как homeScreen.
    // ──────────────────────────────────────────────
    public bool TryHandleBack()
    {
        if (currentPanel == null)
            return false; // открытых панелей нет — пусть телефон сам решает (уйдёт в history/останется на homeScreen)

        ClosePanel();
        return true; // закрыли верхнюю панель, сам экран меню остался открыт
    }
}