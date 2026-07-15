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
    [Header("ависимости")]
    [SerializeField] private ChatView chatView;
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private GalleryManager galleryManager;

    [Header("Список чатов (левая панель)")]
    [SerializeField] private Transform chatListContainer;
    [SerializeField] private GameObject chatItemPrefab;
    [SerializeField] private GameObject chatItemPrefabLine;

    [Header("анели (переключение экранов)")]
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
    private bool wasOpenedBefore;

    private Dictionary<string, ChatItem> chatItems = new();

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (scenarioManager == null)
            scenarioManager = FindAnyObjectByType<ScenarioManager>();
    }

    private void Start()
    {
        gameData     = GameManager.Instance.GameData ?? new GameData();
        chatDatabase = GameManager.Instance.ChatDatabase;

        InitializeChatList();

        if (chatView != null)
            chatView.OnSubmitPressed += OnSubmitButtonClicked;

        ShowChatList();
    }

    private void OnEnable()
    {
        wasOpenedBefore = true;
        ShowChatList();
    }

    private void OnDisable()
    {
        CloseChatWindow();
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

    public void ShowTypingIndicator()                   => chatView?.ShowTypingIndicator();
    public void HideTypingIndicator()                   => chatView?.HideTypingIndicator();
    public void UpdateTypingIndicatorText(string text)  => chatView?.UpdateTypingIndicatorText(text);
    public void SetStatusText(string text)              => chatView?.SetStatusText(text);
    public void ShowInputPrompt()                       => chatView?.ShowInputPrompt();
    public void HideInputPrompt()                       => chatView?.HideInputPrompt();

    public void SpawnChoiceButtons(List<ChatChoice> choices, System.Action<int> onSelected)
        => chatView?.SpawnChoiceButtons(choices, onSelected);

    public void ClearChoiceButtons() => chatView?.ClearChoiceButtons();

    // ──────────────────────────────────────────────
    // авигация по чатам
    // ──────────────────────────────────────────────

    public void OpenChat(string chatId)
    {
        Debug.Log($"[ChatController] OpenChat: '{chatId}'");
        if (selectedChatId == chatId && !wasOpenedBefore) return;

        selectedChatId  = chatId;
        wasOpenedBefore = false;

        Chat chat = FindChatById(chatId);
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
        RestoreMessageHistory(chatId);
        chatItems.GetValueOrDefault(chatId)?.HideUnreadIndicator();

        ShowChatWindowInternal();
        scenarioManager.PlayDialogue(chatId);
    }

    public void SelectChat(string chatId) => OpenChat(chatId);

    public string GetSelectedChatId() => selectedChatId;

    public bool IsChatOpen(string chatId) =>
        !string.IsNullOrEmpty(selectedChatId) && selectedChatId == chatId;

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

        bool alreadyExists = chat.messages.Exists(m =>
            m.text      == message.text      &&
            m.senderId  == message.senderId  &&
            m.timestamp == message.timestamp);

        if (!alreadyExists)
            chat.messages.Add(message);

        if (IsChatOpen(chatId))
            DisplayMessage(message);
        else
            chatItems.GetValueOrDefault(chatId)?.ShowUnreadIndicator();

        string preview = !string.IsNullOrEmpty(message.imageId) ? "[ото]" : message.text;
        chatItems.GetValueOrDefault(chatId)?.UpdatePreview(preview);
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

    private void InitializeChatList()
    {
        if (chatListInitialized) return;

        if (chatItemPrefabLine != null)
            Instantiate(chatItemPrefabLine, chatListContainer);

        foreach (Chat chat in chatDatabase.chats)
        {
            Sprite avatar = Resources.Load<Sprite>(chat.avatarPath);
            CreateChatItem(chat.id, chat.name, avatar);
        }
        chatListInitialized = true;
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
        List<SavedChatMessage> history = gameData.GetChatHistory(chatId);
        foreach (var msg in history)
        {
            bool isPlayer = msg.senderId == "player";
            bool isImage  = !string.IsNullOrEmpty(msg.imageId);

            if (isImage) AddImage(msg.imageId, isPlayer, msg.senderName);
            else         AddMessage(msg.text, isPlayer, msg.senderName);
        }
    }

    private void DisplayMessage(ChatMessage msg)
    {
        bool isPlayer = msg.senderId == "player";
        bool isImage  = !string.IsNullOrEmpty(msg.imageId);
        if (isImage) AddImage(msg.imageId, isPlayer, msg.senderName);
        else         AddMessage(msg.text,  isPlayer, msg.senderName);
    }

    private Chat FindChatById(string chatId)
    {
        var chat = chatDatabase?.chats.Find(c => c.id == chatId);
        if (chat == null)
            Debug.LogWarning($"[ChatController] ат '{chatId}' не найден в базе.");
        return chat;
    }

    private void UnlockGalleryItem(string itemId)
    {
        var galMgr = galleryManager != null ? galleryManager : FindAnyObjectByType<GalleryManager>();
        galMgr?.UnlockGalleryItem(itemId);
    }

    private void OnSubmitButtonClicked()
    {
        Debug.Log("[ChatController] нопка отправки нажата");
        scenarioManager?.OnPlayerActionButtonPressed();
    }
}
