using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// онтроллер чата: управляет навигацией (открытие/закрытие чатов),
/// инициализацией списка, восстановлением истории и фоновыми сообщениями.
/// сё визуальное отображение делегирует в ChatView.
/// </summary>
public class PhoneChatController : MonoBehaviour, INavigableScreen
{
    [Header("Зависимости")]
    [SerializeField] private ChatView chatView;
    [SerializeField] private ScenarioManager scenarioManager;

    [Header("Список чатов (левая панель)")]
    [SerializeField] private Transform chatListContainer;
    [SerializeField] private GameObject chatItemPrefab;
    [SerializeField] private GameObject chatItemPrefabLine;

    [Header("Панели (переключение экранов)")]
    [SerializeField] private GameObject chatListPanel;
    [SerializeField] private GameObject chatWindowPanel;

    // ──────────────────────────────────────────────
    // риватные поля
    // ──────────────────────────────────────────────

    private GameData gameData;
    private ChatDatabase chatDatabase;

    private string selectedChatId;
    private ChatType currentChatType;

    private bool chatListInitialized;
    private int lastInitializedDay = -1;

    private Dictionary<string, ChatItem> chatItems = new();

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (scenarioManager == null)
            scenarioManager = FindAnyObjectByType<ScenarioManager>();

        // Важно: делаем это в Awake, а не в Start. Окно чата обычно выключено
        // DesktopManager-ом сразу после старта сцены (SetActive(false) для всех
        // окон приложений), а значит Start() этого объекта не выполнится, пока
        // игрок не откроет приложение — но фоновые сообщения (GameEventManager /
        // ScenarioManager.DeliverBackgroundMessages) должны находить чат в базе
        // ещё до этого. Awake выполняется всегда, независимо от активности окна.
        gameData     = GameManager.Instance.GameData ?? new GameData();
        chatDatabase = GameManager.Instance.ChatDatabase;

        InitializeChatList();
        RestoreUnreadIndicators();
    }

    private void Start()
    {
        if (chatView != null)
            chatView.OnSubmitPressed += OnSubmitButtonClicked;

        if (DayFlowManager.Instance != null)
            DayFlowManager.Instance.OnDayStarted += OnDayStartedHandler;

        ShowChatList();
    }

    private void OnEnable()
    {
        ShowChatList();
    }

    private void OnDisable()
    {
        CloseChatWindow();
    }

    private void OnDestroy()
    {
        if (DayFlowManager.Instance != null)
            DayFlowManager.Instance.OnDayStarted -= OnDayStartedHandler;
    }

    // ──────────────────────────────────────────────
    // INavigableScreen
    // ──────────────────────────────────────────────

    public bool TryHandleBack()
    {
        Debug.Log($"[Chat] TryHandleBack. chatWindowPanel active: {chatWindowPanel?.activeSelf}");
        if (chatWindowPanel != null && chatWindowPanel.activeSelf)
        {
            CloseChatWindow();
            ShowChatListInternal();
            return true;
        }
        return false;
    }

    // ──────────────────────────────────────────────
    // убличный API — делегаты в ChatView
    // спользуются ScenarioManager-ом
    // ──────────────────────────────────────────────

    public void AddMessage(string text, bool isPlayer, string senderName = "")
        => chatView?.AddMessage(text, isPlayer, senderName, currentChatType);

    public void AddImage(string imageId, bool isPlayer, string senderName = "")
    {
        chatView?.AddImage(imageId, isPlayer, senderName, currentChatType);
        UnlockGalleryItem(imageId);
    }

    public void AddDayDivider(int dayNumber, string rawText = null)
        => chatView?.AddDayDivider(dayNumber, rawText, currentChatType);

    public void ShowTypingIndicator()                   => chatView?.ShowTypingIndicator();
    public void HideTypingIndicator()                   => chatView?.HideTypingIndicator();
    public void UpdateTypingIndicatorText(string text)  => chatView?.UpdateTypingIndicatorText(text);
    public void SetStatusText(string text)              => chatView?.SetStatusText(text);
    public void ShowInputPrompt(string promptText = null)       => chatView?.ShowInputPrompt(promptText);
    public void HideInputPrompt()                       => chatView?.HideInputPrompt();

    public void SpawnChoiceButtons(List<ChatChoice> choices, System.Action<int> onSelected)
        => chatView?.SpawnChoiceButtons(choices, onSelected);

    public void ClearChoiceButtons() => chatView?.ClearChoiceButtons();

    /// <summary>
    /// Проброс состояния ввода игрока (зажат ли пробел) из View в
    /// ScenarioManager. ScenarioManager сам ничего не знает про клавиатуру —
    /// он только опрашивает этот метод.
    /// </summary>
    public bool IsSkipInputHeld() => chatView != null && chatView.IsSkipHeld();

    // ──────────────────────────────────────────────
    // авигация по чатам
    // ──────────────────────────────────────────────

    public void OpenChat(string chatId)
    {
        Chat chat = ResolveChatForSelection(chatId);
        if (chat == null)
        {
            chatView?.SetChatHeader("Чат", null);
            Debug.LogError($"[ChatController] Чат '{chatId}' не найден!");
            return;
        }

        string resolvedChatId = chat.id;

        Debug.Log($"[ChatController] OpenChat: '{chatId}' -> '{resolvedChatId}'");
        if (selectedChatId == resolvedChatId &&
            chatWindowPanel != null &&
            chatWindowPanel.activeSelf)
            return;

        selectedChatId  = resolvedChatId;

        if (chat != null)
        {
            currentChatType = chat.chatType;
            Sprite avatar = Resources.Load<Sprite>(chat.avatarPath);
            chatView?.SetChatHeader(chat.name, avatar);
        }
        else
        {
            chatView?.SetChatHeader("ат", null);
            Debug.LogError($"[ChatController] ат '{chatId}' не найден!");
        }

        chatView?.ClearMessages();
        chatView?.HideInputPrompt();

        if (scenarioManager == null)
        {
            Debug.LogError("[ChatController] scenarioManager не назначен!");
            return;
        }

        scenarioManager.ResetState();

        if (chat != null && gameData != null && !ThreadHasDayDivider(chat.id, gameData.currentDay))
            gameData.EnsureDayDividerInHistory(chat.id, gameData.currentDay);

        RestoreMessageHistory(resolvedChatId);
        chatItems.GetValueOrDefault(GetUiChatKey(resolvedChatId))?.HideUnreadIndicator();

        if (chat != null)
            gameData?.MarkChatAsRead(resolvedChatId, chat.messages.Count);

        ShowChatWindowInternal();
        scenarioManager.PlayDialogue(resolvedChatId);
    }

    public void SelectChat(string chatId) => OpenChat(chatId);

    public string GetSelectedChatId() => selectedChatId;

    public bool IsChatOpen(string chatId) =>
        !string.IsNullOrEmpty(selectedChatId) &&
        selectedChatId == chatId &&
        chatWindowPanel != null &&
        chatWindowPanel.activeSelf;

    /// <summary>ызывается DesktopManager при запуске — создаёт список ChatItem один раз.</summary>
    public void InitializeChatUI() => InitializeChatList();

    // ──────────────────────────────────────────────
    // акрытие чата
    // ──────────────────────────────────────────────

    public void CloseChatWindow()
    {
        scenarioManager?.ResetState();
        chatView?.ClearMessages();
        chatView?.HideTypingIndicator();
        chatView?.HideInputPrompt();
    }

    // ──────────────────────────────────────────────
    // оновые сообщения (от ScenarioManager)
    // ──────────────────────────────────────────────

    /// <summary>
    /// обавить сообщение в историю чата и, если чат открыт, отобразить его.
    /// спользуется ScenarioManager для доставки фоновых сообщений.
    /// </summary>
    public void AddMessageToChat(string chatId, ChatMessage message)
    {
        Chat chat = FindChatById(chatId);
        if (chat == null) return;

        string uiChatKey = GetUiChatKey(chatId);

        if (string.IsNullOrEmpty(message.id))
            message.id = $"runtime_{chatId}_{System.DateTime.UtcNow.Ticks}";

        // Дедупликация — по факту доставки (сохранён в истории), а не по наличию
        // в chat.messages: сообщения из сценария (фоновая доставка) и так уже
        // лежат в chat.messages (это распарсенный .txt), так что проверка по этому
        // списку всегда считала бы их "уже существующими" и обрывала обработку.
        bool alreadyDelivered = gameData != null && gameData.HasMessageInHistory(chatId, message.id);
        if (alreadyDelivered)
            return;

        if (!chat.messages.Contains(message))
            chat.messages.Add(message);

        // Разблокируем изображение сразу в момент доставки сообщения в чат,
        // даже если окно чата сейчас закрыто.
        if (!string.IsNullOrWhiteSpace(message.imageId))
            UnlockGalleryItem(message.imageId);

        if (IsChatOpen(chatId))
        {
            DisplayMessage(message);
            gameData?.MarkChatAsRead(chatId, chat.messages.Count);
        }
        else
        {
            gameData?.AddUnreadMessage(chatId);
            chatItems.GetValueOrDefault(uiChatKey)?.ShowUnreadIndicator();
        }

        gameData?.AddMessageToHistory(chatId, message);

        string preview = !string.IsNullOrEmpty(message.imageId) ? "[фото]" : message.text;
        chatItems.GetValueOrDefault(uiChatKey)?.UpdatePreview(preview);
    }

    private void RestoreUnreadIndicators()
    {
        foreach (var pair in chatItems)
        {
            string activeChatId = ResolveActiveChatIdForUiKey(pair.Key);
            int unreadCount = gameData?.GetUnreadMessageCount(activeChatId) ?? 0;
            if (unreadCount > 0)
                pair.Value?.ShowUnreadIndicator();
            else
                pair.Value?.HideUnreadIndicator();
        }
    }

    // ──────────────────────────────────────────────
    // риватные методы — навигация и инициализация
    // ──────────────────────────────────────────────

    private void ShowChatList()
    {
        if (chatWindowPanel != null) chatWindowPanel.SetActive(false);
        if (chatListPanel   != null) chatListPanel.SetActive(true);
    }

    private void ShowChatListInternal()
    {
        if (chatWindowPanel != null) chatWindowPanel.SetActive(false);
        if (chatListPanel   != null) chatListPanel.SetActive(true);
    }

    private void ShowChatWindowInternal()
    {
        if (chatListPanel   != null) chatListPanel.SetActive(false);
        if (chatWindowPanel != null) chatWindowPanel.SetActive(true);
    }

    private void InitializeChatList(bool forceRebuild = false)
    {
        int currentDay = GetCurrentDayNumber();
        if (!forceRebuild && chatListInitialized && lastInitializedDay == currentDay)
            return;

        bool needClearExistingItems = forceRebuild || chatListInitialized || lastInitializedDay != currentDay;
        if (needClearExistingItems)
        {
            foreach (Transform child in chatListContainer)
                Destroy(child.gameObject);

            chatItems.Clear();
        }

        if (chatItemPrefabLine != null)
            Instantiate(chatItemPrefabLine, chatListContainer);

        foreach (Chat chat in GetChatsForCurrentDay())
        {
            Sprite avatar = Resources.Load<Sprite>(chat.avatarPath);
            CreateChatItem(GetUiChatKey(chat.id), chat.name, avatar);
        }

        lastInitializedDay = currentDay;
        chatListInitialized = true;
    }

    private void OnDayStartedHandler(int _)
    {
        InitializeChatList(true);
        RestoreUnreadIndicators();

        TryAppendDayDividerForOpenedChat();
    }

    private void CreateChatItem(string id, string name, Sprite avatar)
    {
        GameObject itemGO = Instantiate(chatItemPrefab, chatListContainer);
        if (chatItemPrefabLine != null)
            Instantiate(chatItemPrefabLine, chatListContainer);

        if (!itemGO.TryGetComponent<ChatItem>(out var chatItem))
        {
            Debug.LogError("[ChatController] ChatItem компонент не найден на prefab!");
            return;
        }

        chatItem.SetupChat(id, name, avatar, chatId => OpenChat(chatId));
        chatItems[id] = chatItem;
    }

    private void RestoreMessageHistory(string chatId)
    {
        List<SavedChatMessage> history = GetHistoryForDisplay(chatId);
        foreach (var msg in history)
        {
            if (msg == null)
                continue;

            if (msg.isSystemMarker)
            {
                // Формат разделителя берём из ChatView (dayDividerFormat),
                // а не из сохранённого текста, чтобы локализация менялась
                // сразу после правки поля в инспекторе.
                AddDayDivider(msg.dayNumber, null);
                continue;
            }

            bool isPlayer = msg.senderId == "player";
            bool isImage  = !string.IsNullOrEmpty(msg.imageId);

            if (isImage) AddImage(msg.imageId, isPlayer, msg.senderName);
            else         AddMessage(msg.text, isPlayer, msg.senderName);
        }
    }

    private List<SavedChatMessage> GetHistoryForDisplay(string chatId)
    {
        if (gameData == null || string.IsNullOrWhiteSpace(chatId))
            return new List<SavedChatMessage>();

        Chat selectedChat = FindChatById(chatId, false);
        if (selectedChat == null)
            return gameData.GetChatHistory(chatId);

        string threadKey = GetThreadKey(selectedChat);
        if (string.IsNullOrWhiteSpace(threadKey) || chatDatabase == null || chatDatabase.chats == null)
            return gameData.GetChatHistory(chatId);

        int currentDay = GetCurrentDayNumber();
        var threadChats = new List<Chat>();

        foreach (Chat chat in chatDatabase.chats)
        {
            if (chat == null)
                continue;

            if (GetThreadKey(chat) != threadKey)
                continue;

            int chatDay = chat.dayNumber > 0 ? chat.dayNumber : 1;
            if (chatDay > currentDay)
                continue;

            threadChats.Add(chat);
        }

        threadChats.Sort((left, right) =>
        {
            int leftDay = left != null && left.dayNumber > 0 ? left.dayNumber : 1;
            int rightDay = right != null && right.dayNumber > 0 ? right.dayNumber : 1;

            int dayCompare = leftDay.CompareTo(rightDay);
            if (dayCompare != 0)
                return dayCompare;

            return string.CompareOrdinal(left?.id, right?.id);
        });

        var mergedHistory = new List<SavedChatMessage>();
        for (int i = 0; i < threadChats.Count; i++)
        {
            Chat threadChat = threadChats[i];
            if (threadChat == null || string.IsNullOrWhiteSpace(threadChat.id))
                continue;

            List<SavedChatMessage> part = gameData.GetChatHistory(threadChat.id);
            if (part == null || part.Count == 0)
                continue;

            mergedHistory.AddRange(part);
        }

        return mergedHistory.Count > 0 ? mergedHistory : gameData.GetChatHistory(chatId);
    }

    private void DisplayMessage(ChatMessage msg)
    {
        bool isPlayer = msg.senderId == "player";
        bool isImage  = !string.IsNullOrEmpty(msg.imageId);
        if (isImage) AddImage(msg.imageId, isPlayer, msg.senderName);
        else         AddMessage(msg.text,  isPlayer, msg.senderName);
    }

    private int GetCurrentDayNumber()
    {
        return gameData != null ? gameData.currentDay : 1;
    }

    private void TryAppendDayDividerForOpenedChat()
    {
        if (gameData == null)
            return;

        if (chatWindowPanel == null || !chatWindowPanel.activeSelf)
            return;

        if (string.IsNullOrWhiteSpace(selectedChatId))
            return;

        int currentDay = GetCurrentDayNumber();
        bool dividerAlreadyInHistory = ThreadHasDayDivider(selectedChatId, currentDay);

        if (dividerAlreadyInHistory)
            return; // маркер этого дня уже есть где-то в треде — второй не создаём

        gameData.EnsureDayDividerInHistory(selectedChatId, currentDay);
        AddDayDivider(currentDay);
    }

    private bool HasDayDividerInHistory(string chatId, int dayNumber)
    {
        if (gameData == null || string.IsNullOrWhiteSpace(chatId))
            return false;

        List<SavedChatMessage> history = gameData.GetChatHistory(chatId);
        if (history == null)
            return false;

        for (int i = 0; i < history.Count; i++)
        {
            SavedChatMessage message = history[i];
            if (message == null || !message.isSystemMarker)
                continue;

            int markerDay = message.dayNumber > 0 ? message.dayNumber : 1;
            if (markerDay == dayNumber)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Разные дни одного треда (диалога) хранятся как разные Chat.id
    /// (см. ResolveChatForSelection/GetThreadKey), каждый со своей отдельной
    /// историей в GameData. Разделитель дня из-за этого может быть добавлен
    /// то в историю "вчерашнего" chat.id (пока окно было открыто и наступил
    /// новый день), то в историю "сегодняшнего" chat.id (при следующем
    /// открытии чата) — HasDayDividerInHistory для одного конкретного chatId
    /// такой дубликат не увидит. Эта проверка ищет маркер дня по ВСЕМ
    /// чатам треда, а не только по одному chatId.
    /// </summary>
    private bool ThreadHasDayDivider(string chatId, int dayNumber)
    {
        Chat selectedChat = FindChatById(chatId, false);
        if (selectedChat == null)
            return HasDayDividerInHistory(chatId, dayNumber);

        string threadKey = GetThreadKey(selectedChat);
        if (string.IsNullOrWhiteSpace(threadKey) || chatDatabase == null || chatDatabase.chats == null)
            return HasDayDividerInHistory(chatId, dayNumber);

        int currentDay = GetCurrentDayNumber();

        foreach (Chat chat in chatDatabase.chats)
        {
            if (chat == null || GetThreadKey(chat) != threadKey)
                continue;

            int chatDay = chat.dayNumber > 0 ? chat.dayNumber : 1;
            if (chatDay > currentDay)
                continue;

            if (HasDayDividerInHistory(chat.id, dayNumber))
                return true;
        }

        return false;
    }

    private static string GetThreadKey(Chat chat)
    {
        if (chat == null)
            return string.Empty;

        return !string.IsNullOrWhiteSpace(chat.threadId) ? chat.threadId : chat.id;
    }

    private List<Chat> GetChatsForCurrentDay()
    {
        var result = new List<Chat>();
        if (chatDatabase == null || chatDatabase.chats == null)
            return result;

        int currentDay = GetCurrentDayNumber();
        var bestByThread = new Dictionary<string, Chat>();
        var orderedKeys = new List<string>();

        foreach (Chat chat in chatDatabase.chats)
        {
            if (chat == null || string.IsNullOrWhiteSpace(chat.id))
                continue;

            int chatDay = chat.dayNumber > 0 ? chat.dayNumber : 1;
            if (chatDay > currentDay)
                continue;

            string threadKey = GetThreadKey(chat);
            if (string.IsNullOrWhiteSpace(threadKey))
                threadKey = chat.id;

            if (!bestByThread.TryGetValue(threadKey, out Chat existing))
            {
                bestByThread[threadKey] = chat;
                orderedKeys.Add(threadKey);
                continue;
            }

            int existingDay = existing.dayNumber > 0 ? existing.dayNumber : 1;
            if (chatDay > existingDay)
                bestByThread[threadKey] = chat;
        }

        for (int i = 0; i < orderedKeys.Count; i++)
        {
            string key = orderedKeys[i];
            if (bestByThread.TryGetValue(key, out Chat chat) && chat != null)
                result.Add(chat);
        }

        return result;
    }

    private Chat ResolveChatForSelection(string selectedKey)
    {
        if (string.IsNullOrWhiteSpace(selectedKey) || chatDatabase == null || chatDatabase.chats == null)
            return null;

        int currentDay = GetCurrentDayNumber();
        Chat exactById = FindChatById(selectedKey, false);

        string threadKey = selectedKey;
        if (exactById != null && !string.IsNullOrWhiteSpace(exactById.threadId))
            threadKey = exactById.threadId;

        Chat best = null;
        foreach (Chat chat in chatDatabase.chats)
        {
            if (chat == null)
                continue;

            if (GetThreadKey(chat) != threadKey)
                continue;

            int chatDay = chat.dayNumber > 0 ? chat.dayNumber : 1;
            if (chatDay > currentDay)
                continue;

            if (best == null)
            {
                best = chat;
                continue;
            }

            int bestDay = best.dayNumber > 0 ? best.dayNumber : 1;
            if (chatDay > bestDay)
                best = chat;
        }

        return best ?? exactById;
    }

    private string ResolveActiveChatIdForUiKey(string uiKey)
    {
        Chat resolved = ResolveChatForSelection(uiKey);
        return resolved != null ? resolved.id : uiKey;
    }

    private string GetUiChatKey(string chatId)
    {
        Chat chat = FindChatById(chatId, false);
        if (chat == null)
            return chatId;

        string threadKey = GetThreadKey(chat);
        return string.IsNullOrWhiteSpace(threadKey) ? chat.id : threadKey;
    }

    private Chat FindChatById(string chatId, bool logWarning = true)
    {
        var chat = chatDatabase?.chats.Find(c => c.id == chatId);
        if (chat == null && logWarning)
            Debug.LogWarning($"[ChatController] ат '{chatId}' не найден в базе.");
        return chat;
    }

    private void UnlockGalleryItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return;

        if (GalleryService.Instance != null)
        {
            bool changed = GalleryService.Instance.UnlockItem(itemId);
            Debug.Log($"[PhoneChatController] Gallery unlock via service: {itemId}, changed={changed}");
            return;
        }

        // Fallback на случай ранней доставки сообщения до инициализации GalleryService.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnlockGalleryItem(itemId);
            Debug.Log($"[PhoneChatController] Gallery unlock via fallback GameManager: {itemId}");
        }
    }

    private void OnSubmitButtonClicked()
    {
        scenarioManager?.OnPlayerActionButtonPressed();
    }
}