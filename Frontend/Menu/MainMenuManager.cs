using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    //[SerializeField] private GameObject menuPanel;

    [SerializeField] private GameObject loadPanel;
    [SerializeField] private GameObject savePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject menuPanel;

    [SerializeField] private Button loadButton;
    //[SerializeField] private Button saveButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [SerializeField] private Button closeLoadButton;
    [SerializeField] private Button closeSaveButton;
    [SerializeField] private Button closeSettingsButton;

    [SerializeField] private GameObject newGamePanel;

    [SerializeField] private Button newGameButton;
    [SerializeField] private Button backFromNewGameButton;
    [SerializeField] private Button startGameButton;

    [SerializeField] private TMP_InputField nicknameInput;

    [SerializeField] private string gameSceneName = "Game";

    private bool menuOpen = false;

    // Стек открытых панелей (Load / Settings), как в MenuManager —
    // чтобы поведение открытия/закрытия было единообразным по проекту.
    private readonly Stack<GameObject> panelHistory = new();
    private GameObject currentPanel;

    private void Start()
    {
        CloseAllPanels();

        // Главное меню
        //menuButton.onClick.AddListener(ToggleMenu);

        // Кнопки меню
        loadButton.onClick.AddListener(() => OpenPanel(loadPanel));
        //saveButton.onClick.AddListener(() => OpenPanel(savePanel));
        settingsButton.onClick.AddListener(() => OpenPanel(settingsPanel));
        exitButton.onClick.AddListener(ExitGame);

        // Кнопки закрытия панелей
        closeLoadButton.onClick.AddListener(ClosePanel);
        //closeSaveButton.onClick.AddListener(ClosePanel);
        closeSettingsButton.onClick.AddListener(ClosePanel);

        newGameButton.onClick.AddListener(OpenNewGamePanel);

        backFromNewGameButton.onClick.AddListener(ReturnToMainMenu);

        startGameButton.onClick.AddListener(StartNewGame);
    }

    private void ToggleMenu()
    {
        menuOpen = !menuOpen;
        //menuPanel.SetActive(menuOpen);

        // Закрываем все подпанели
        if (menuOpen)
        {
            CloseAllPanels();
        }
    }

    private void OpenNewGamePanel()
    {
        CloseAllPanels();

        // Если есть главное меню как отдельный объект:
        menuPanel.SetActive(false);

        newGamePanel.SetActive(true);
    }

    private void StartNewGame()
    {
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            nickname = "Player";
        }

        GameManager.Instance.nickname = nickname;

        SceneManager.LoadScene(gameSceneName);
    }

    private void ReturnToMainMenu()
    {
        newGamePanel.SetActive(false);

        // Если есть отдельная панель главного меню:
        menuPanel.SetActive(true);
    }

    public void ToggleMenuFromCode()
    {
        ToggleMenu();
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
        newGamePanel.SetActive(false);

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
}