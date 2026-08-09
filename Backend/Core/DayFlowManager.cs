using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayFlowManager : MonoBehaviour
{
    public static DayFlowManager Instance { get; private set; }

    [Header("Условия завершения дня")]
    [Tooltip("Список chatId, которые должны быть завершены, чтобы кнопка завершения дня сработала")]
    [SerializeField] private List<string> requiredChatsToEndDay = new List<string>();

    [Header("Переход дня")]
    [SerializeField] private bool useScreenFadeTransition = true;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.35f;
    [SerializeField] private float blackScreenHoldDuration = 0.1f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private DesktopManager desktopManager;

    private GameData gameData;
    private bool isTransitionInProgress;
    private Canvas transitionCanvas;
    private CanvasGroup transitionCanvasGroup;
    private Image transitionImage;

    public event System.Action<int> OnDayStarted;
    public event System.Action<int> OnDayEnded;

    public int CurrentDay => gameData != null ? gameData.currentDay : 1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.ApplyRuntimeSettingsFrom(this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureData();

        if (gameData == null)
            return;

        gameData.SetCurrentDay(gameData.currentDay);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ReloadStateFromGameData(bool notifyDayStarted = false)
    {
        EnsureData();

        if (gameData == null)
            return;

        gameData.SetCurrentDay(gameData.currentDay);

        if (notifyDayStarted)
            OnDayStarted?.Invoke(gameData.currentDay);
    }

    public bool CanEndDay()
    {
        EnsureData();

        if (gameData == null)
            return false;

        if (requiredChatsToEndDay == null)
            return true;

        ChatDatabase chatDatabase = GameManager.Instance != null ? GameManager.Instance.ChatDatabase : null;

        for (int i = 0; i < requiredChatsToEndDay.Count; i++)
        {
            string chatId = requiredChatsToEndDay[i];
            if (string.IsNullOrWhiteSpace(chatId))
                continue;

            Chat requiredChat = chatDatabase?.chats?.Find(c => c != null && c.id == chatId);
            if (requiredChat == null)
            {
                Debug.LogWarning($"[DayFlowManager] Required chat '{chatId}' не найден в базе. Завершение дня заблокировано.");
                return false;
            }

            int requiredChatDay = requiredChat.dayNumber > 0 ? requiredChat.dayNumber : 1;
            if (requiredChatDay != CurrentDay)
            {
                Debug.LogWarning($"[DayFlowManager] Required chat '{chatId}' относится к дню {requiredChatDay}, а сейчас день {CurrentDay}. Завершение дня заблокировано.");
                return false;
            }

            if (!gameData.IsChatCompletedForDay(chatId, CurrentDay))
                return false;
        }

        return true;
    }

    public bool TryEndDay()
    {
        EnsureData();

        if (gameData == null)
            return false;

        if (!CanEndDay())
        {
            Debug.LogWarning($"[DayFlowManager] Нельзя завершить день {gameData.currentDay}: не выполнены обязательные чаты.");
            return false;
        }

        int endedDay = gameData.currentDay;
        gameData.MarkDayCompleted(endedDay);
        OnDayEnded?.Invoke(endedDay);

        gameData.StartNextDay();
        OnDayStarted?.Invoke(gameData.currentDay);

        Debug.Log($"[DayFlowManager] День {endedDay} завершен. Текущий день: {gameData.currentDay}");
        return true;
    }

    public void TryEndDayFromUI()
    {
        if (isTransitionInProgress)
            return;

        if (!useScreenFadeTransition)
        {
            TryEndDay();
            return;
        }

        if (!CanEndDay())
        {
            TryEndDay();
            return;
        }

        StartCoroutine(TryEndDayWithTransition());
    }

    public void SetRequiredChatsToEndDay(List<string> chatIds)
    {
        requiredChatsToEndDay = chatIds ?? new List<string>();
    }

    private void EnsureData()
    {
        if (gameData == null && GameManager.Instance != null)
            gameData = GameManager.Instance.GameData;
    }

    private IEnumerator TryEndDayWithTransition()
    {
        isTransitionInProgress = true;

        EnsureDependencies();
        EnsureTransitionOverlay();

        yield return FadeTo(1f, fadeOutDuration);

        desktopManager?.ShowDesktopHome();

        TryEndDay();

        if (blackScreenHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(blackScreenHoldDuration);

        yield return FadeTo(0f, fadeInDuration);

        isTransitionInProgress = false;
    }

    private void EnsureDependencies()
    {
        if (desktopManager == null)
            desktopManager = FindAnyObjectByType<DesktopManager>();
    }

    private void EnsureTransitionOverlay()
    {
        if (transitionCanvas != null && transitionCanvasGroup != null && transitionImage != null)
        {
            transitionImage.color = fadeColor;
            return;
        }

        GameObject canvasObject = new GameObject("DayTransitionFadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        transitionCanvas = canvasObject.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = short.MaxValue;

        canvasObject.AddComponent<GraphicRaycaster>();

        transitionCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
        ApplyFadeAlpha(0f);

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        transitionImage = imageObject.AddComponent<Image>();
        transitionImage.color = fadeColor;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (transitionCanvasGroup == null)
            yield break;

        float startAlpha = transitionCanvasGroup.alpha;
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f)
        {
            ApplyFadeAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        ApplyFadeAlpha(targetAlpha);
    }

    private void ApplyFadeAlpha(float alpha)
    {
        if (transitionCanvasGroup == null)
            return;

        transitionCanvasGroup.alpha = alpha;

        bool blockInput = alpha > 0.001f;
        transitionCanvasGroup.blocksRaycasts = blockInput;
        transitionCanvasGroup.interactable = blockInput;
    }

    private void ApplyRuntimeSettingsFrom(DayFlowManager source)
    {
        if (source == null)
            return;

        requiredChatsToEndDay = source.requiredChatsToEndDay != null
            ? new List<string>(source.requiredChatsToEndDay)
            : new List<string>();

        useScreenFadeTransition = source.useScreenFadeTransition;
        fadeOutDuration = source.fadeOutDuration;
        fadeInDuration = source.fadeInDuration;
        blackScreenHoldDuration = source.blackScreenHoldDuration;
        fadeColor = source.fadeColor;

        if (source.desktopManager != null)
            desktopManager = source.desktopManager;
    }
}
