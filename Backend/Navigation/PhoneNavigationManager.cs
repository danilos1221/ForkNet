using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Управляет навигацией по экранам телефона: назад / домой / главное меню игры.
/// Единая точка правды — какой экран сейчас активен.
/// </summary>
public class PhoneNavigationManager : MonoBehaviour
{
    public static PhoneNavigationManager Instance { get; private set; }

    [Header("Ключевые экраны")]
    [SerializeField] private GameObject homeScreen;   // рабочий стол телефона
    [SerializeField] private GameObject gameMenuScreen; // главное меню игры (пауза/сохранения и т.п.)

    [Header("Все экраны-приложения телефона")]
    [Tooltip("Сюда добавляются ВСЕ панели, между которыми переключается навигация: список чатов, окно чата, галерея, настройки и т.д. homeScreen тоже должен быть в списке.")]
    [SerializeField] private List<GameObject> appScreens = new();

    [Header("Кнопки нижней панели телефона")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button menuButton;

    private readonly Stack<GameObject> history = new();
    private GameObject currentScreen;
    private DesktopManager desktopManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (backButton != null) backButton.onClick.AddListener(GoBack);
        if (homeButton != null) homeButton.onClick.AddListener(GoHome);
        if (menuButton != null) menuButton.onClick.AddListener(OpenGameMenu);

        if (homeScreen != null)
            desktopManager = homeScreen.GetComponent<DesktopManager>();

        SetActiveScreenInternal(homeScreen, clearHistory: true);
    }

    // ──────────────────────────────────────────────
    // Публичный API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Перегрузка с одним параметром — специально для UnityEvent/OnClick в инспекторе.
    /// Unity показывает в списке методов OnClick только методы РОВНО с одним параметром
    /// (значения по умолчанию через рефлексию не видны, поэтому двухпараметрический
    /// OpenScreen(GameObject, bool) в инспекторе не отображается). Всегда добавляет
    /// текущий экран в историю — это нужное поведение для кнопок "открыть экран X".
    /// </summary>
    public void OpenScreen(GameObject screen)
    {
        OpenScreen(screen, true);
    }

    /// <summary>
    /// Открыть произвольный экран приложения. Текущий экран уходит в историю
    /// (если addToHistory == true). Вызывается из кода — из других менеджеров
    /// (ChatManagerAdvanced.ShowChatWindow и т.п.) вместо ручного SetActive,
    /// либо когда нужно явно НЕ добавлять текущий экран в историю.
    /// </summary>
    public void OpenScreen(GameObject screen, bool addToHistory)
    {
        if (screen == null) return;

        // Если запрашивают тот же экран повторно, но он оказался скрыт
        // (например, после внутренней обработки "назад"), нужно
        // переактивировать его, иначе кнопка "не работает" до
        // переключения на другой экран.
        if (screen == currentScreen)
        {
            if (!screen.activeInHierarchy)
                SetActiveScreenInternal(screen, clearHistory: false);
            return;
        }

        if (addToHistory && currentScreen != null)
            history.Push(currentScreen);

        SetActiveScreenInternal(screen, clearHistory: false);
    }

    /// <summary>Кнопка "назад" — вернуться на предыдущий экран из истории.</summary>
    public void GoBack()
    {
        Debug.Log($"[Nav] GoBack called. currentScreen = {currentScreen?.name}");

        INavigableScreen navigable = ResolveNavigableForCurrentScreen();
        if (navigable != null)
        {
            Debug.Log($"[Nav] Found INavigableScreen for {currentScreen?.name}, calling TryHandleBack()");
            if (navigable.TryHandleBack())
            {
                Debug.Log("[Nav] TryHandleBack() returned true, staying on screen.");

                // Некоторые экраны могут вернуть true, но при этом скрыть
                // собственный root-объект. Тогда currentScreen остаётся
                // ссылаться на скрытый экран и повторное OpenScreen(screen)
                // не срабатывает. Если экран скрыт — уходим на previous/home.
                if (currentScreen != null && !currentScreen.activeInHierarchy)
                {
                    GameObject fallback = history.Count > 0 ? history.Pop() : homeScreen;
                    Debug.Log($"[Nav] Current screen became inactive after TryHandleBack, fallback to: {fallback?.name}");
                    SetActiveScreenInternal(fallback, clearHistory: false);
                }

                return;
            }
            Debug.Log("[Nav] TryHandleBack() returned false, going to previous screen.");
        }
        else
        {
            Debug.Log($"[Nav] No INavigableScreen found on {currentScreen?.name}");
        }

        GameObject previous = history.Count > 0 ? history.Pop() : homeScreen;
        Debug.Log($"[Nav] Going to previous screen: {previous?.name}");
        SetActiveScreenInternal(previous, clearHistory: false);
    }

    /// <summary>Кнопка "домой" — скрыть всё, кроме рабочего стола, история очищается.</summary>
    public void GoHome()
    {
        SetActiveScreenInternal(homeScreen, clearHistory: true);
    }

    /// <summary>Кнопка меню — открыть главное меню игры поверх текущего состояния.</summary>
    public void OpenGameMenu()
    {
        OpenScreen(gameMenuScreen);
    }

    public GameObject CurrentScreen => currentScreen;

    // ──────────────────────────────────────────────
    // Внутреннее
    // ──────────────────────────────────────────────

    private void SetActiveScreenInternal(GameObject screen, bool clearHistory)
    {
        if (screen == null) return;
        if (clearHistory) history.Clear();

        // Если на рабочем столе телефона было открыто приложение (например, мини-игра
        // в PasswordGameWindow) — сворачиваем его при переходе на другой экран/меню,
        // иначе его окно останется висеть поверх/позади нового экрана.
        if (screen != homeScreen)
            desktopManager?.MinimizeCurrentApp();

        foreach (var s in appScreens)
        {
            if (s != null) s.SetActive(s == screen);
        }

        // gameMenuScreen может быть отдельным оверлеем поверх appScreens
        if (gameMenuScreen != null && !appScreens.Contains(gameMenuScreen))
            gameMenuScreen.SetActive(screen == gameMenuScreen);

        currentScreen = screen;
    }

    private INavigableScreen ResolveNavigableForCurrentScreen()
    {
        if (currentScreen == null)
            return null;

        // 1) Обычный путь: компонент висит на самом root-объекте экрана.
        INavigableScreen navigable = currentScreen.GetComponent<INavigableScreen>();
        if (navigable != null)
            return navigable;

        // 2) Частый кейс в сцене: currentScreen — дочерний контейнер,
        // а INavigableScreen висит на родителе.
        navigable = currentScreen.GetComponentInParent<INavigableScreen>();
        if (navigable != null)
            return navigable;

        // 3) Фолбэк: компонент висит глубже в дочерних объектах экрана.
        navigable = currentScreen.GetComponentInChildren<INavigableScreen>(true);
        return navigable;
    }
}