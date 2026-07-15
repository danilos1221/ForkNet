using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public enum ChatType
{
    Private,
    Group
}

public class ChatManagerAdvanced : MonoBehaviour, INavigableScreen
{

    [Header("Зависимости")]
    [SerializeField] private ScenarioManager scenarioManager;
    [SerializeField] private GalleryManager galleryManager;
    //[SerializeField] private BackgroundMessageReceiver backgroundReceiver;

    [Header("Список чатов (левая панель)")]
    [SerializeField] private Transform chatListContainer;
    [SerializeField] private GameObject chatItemPrefab;
    [SerializeField] private GameObject chatItemPrefabLine;

    [Header("Шапка открытого чата")]
    [SerializeField] private Image chatHeaderAvatar;
    [SerializeField] private TextMeshProUGUI chatHeaderName;
    [SerializeField] private TextMeshProUGUI statusText;
    [Header("Панели (переключение экранов)")]
    [SerializeField] private GameObject chatListPanel;   // корневой объект списка чатов
    [SerializeField] private GameObject chatWindowPanel;  // корневой объект окна переписки

    [Header("Область сообщений")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private string imageBasePath = "Images/Gallery";

    [Header("Индикатор печатания")]
    [SerializeField] private GameObject typingStatusPrefab;

    [Header("Ввод игрока")]
    [SerializeField] private TextMeshProUGUI inputPromptText;
    [SerializeField] private Button submitMessageButton;

    [Header("Кнопки выбора")]
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonsContainer;

    [Header("Настройки")]
    [SerializeField] private string messageAppearSoundName = "message_appear";
    [SerializeField] private float messageSoundVolume = 0.7f;
    [SerializeField] private int maxMessageHistorySize = 100;

    // ──────────────────────────────────────────────
    // Приватные поля
    // ──────────────────────────────────────────────

    private GameData gameData;
    private ChatDatabase chatDatabase;

    private string selectedChatId;
    private ChatType currentChatType;

    private readonly List<GameObject> displayedMessages = new();
    private readonly List<GameObject> activeChoiceButtons = new();

    private GameObject activeTypingStatusObject;
    private bool chatListInitialized;

    private Dictionary<string, ChatItem> chatItems = new();

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────
    private bool wasOpenedBefore = false;
    //PULSE BUTTON
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.1f;

    private Coroutine pulseCoroutine;
    private Vector3 originalButtonScale;

    private void Awake()
    {
        // Если зависимости не заданы в Inspector — ищем на сцене один раз
        if (scenarioManager == null)
            scenarioManager = FindAnyObjectByType<ScenarioManager>();
        //if (backgroundReceiver == null)
        //    backgroundReceiver = BackgroundMessageReceiver.Instance;
    }

    private void Start()
    {
        gameData = GameManager.Instance.GameData ?? new GameData();
        chatDatabase = GameManager.Instance.ChatDatabase;

        InitializeChatList();

        if (submitMessageButton != null)
        {
            submitMessageButton.onClick.AddListener(OnSubmitButtonClicked);
        }

        ShowChatList();
    }
    private void ShowChatWindow()
    {
        PhoneNavigationManager.Instance?.OpenScreen(chatWindowPanel);
    }
    public bool TryHandleBack()
    {
        Debug.Log($"[Chat] TryHandleBack called. chatWindowPanel active: {chatWindowPanel?.activeSelf}");
        if (chatWindowPanel != null && chatWindowPanel.activeSelf)
        {
            CloseChatWindow();
            ShowChatListInternal();
            return true; // обработали сами, из ChatPanel не выходим
        }

        return false; // мы уже на списке чатов — пусть PhoneNavigationManager уходит выше
    }

    private void ShowChatList()
    {
        if (chatWindowPanel != null) chatWindowPanel.SetActive(false);
        if (chatListPanel != null) chatListPanel.SetActive(true);
    }
    private void ShowChatWindowInternal()
    {
        if (chatListPanel != null) chatListPanel.SetActive(false);
        if (chatWindowPanel != null) chatWindowPanel.SetActive(true);
    }

    private void ShowChatListInternal()
    {
        if (chatWindowPanel != null) chatWindowPanel.SetActive(false);
        if (chatListPanel != null) chatListPanel.SetActive(true);
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
    // Публичный API — вызывается из ScenarioManager
    // ──────────────────────────────────────────────

    /// <summary>Показать сообщение в текущем чате.</summary>
    public void AddMessage(string text, bool isPlayer, string senderName = "")
    {
        Debug.Log($"[ChatManager] AddMessage: '{text}' from '{senderName}' (isPlayer={isPlayer})");
        GameObject msgGO = Instantiate(messagePrefab, messageContainer);
        if (!msgGO.TryGetComponent<MessageUI>(out var messageUI)) return;

        if (currentChatType == ChatType.Private)
        {
            if (isPlayer) messageUI.SetPlayerMessage(text);
            else          messageUI.SetOtherMessage(text);
        }
        else
        {
            if (isPlayer) messageUI.SetGroupPlayerMessage(text);
            else          messageUI.SetGroupOtherMessage(senderName, text);
        }

        RegisterDisplayedMessage(msgGO);
    }

    /// <summary>Показать изображение в текущем чате.</summary>
    public void AddImage(string imageId, bool isPlayer, string senderName = "")
    {
        Sprite sprite = Resources.Load<Sprite>($"{imageBasePath}/{imageId}");
        if (sprite == null)
        {
            Debug.LogWarning($"[ChatManager] Изображение не найдено: {imageBasePath}/{imageId}");
            return;
        }
        if (imagePrefab == null)
        {
            Debug.LogWarning("[ChatManager] imagePrefab не назначен!");
            return;
        }

        GameObject imageGO = Instantiate(imagePrefab, messageContainer);
        if (!imageGO.TryGetComponent<MessageUI>(out var messageUI)) return;

        if (isPlayer)
            messageUI.SetPlayerImage(sprite, imageId);
        else if (currentChatType == ChatType.Private)
            messageUI.SetOtherImage(sprite, imageId);
        else
            messageUI.SetGroupOtherImage(senderName, sprite, imageId);

        RegisterDisplayedMessage(imageGO);
        UnlockGalleryItem(imageId);
    }

    /// <summary>
    /// Добавить сообщение в историю чата и, если чат открыт, отобразить его.
    /// Используется для фоновых сообщений (когда чат закрыт).
    /// </summary>
    public void AddMessageToChat(string chatId, ChatMessage message)
    {
        Chat chat = FindChatById(chatId);
        if (chat == null) return;

        // Избегаем дублей по тексту + отправителю + времени
        bool alreadyExists = chat.messages.Exists(m =>
            m.text == message.text &&
            m.senderId == message.senderId &&
            m.timestamp == message.timestamp);

        if (!alreadyExists)
            chat.messages.Add(message);

        if (IsChatOpen(chatId))
            DisplayMessage(message);
        else
            chatItems[chatId]?.ShowUnreadIndicator();

        string previewText = !string.IsNullOrEmpty(message.imageId)
            ? "[Фото]"
            : message.text;

        chatItems[chatId]?.UpdatePreview(previewText);
    }

    // ──────────────────────────────────────────────
    // Индикатор печатания
    // ──────────────────────────────────────────────

    public void ShowTypingIndicator()
    {
        if (typingStatusPrefab == null) return;
        if (activeTypingStatusObject != null) return; // уже показан

        activeTypingStatusObject = Instantiate(typingStatusPrefab, messageContainer);
        ScrollToBottom();
    }

    public void UpdateTypingIndicatorText(string text)
    {
        if (activeTypingStatusObject == null) return;
        var tmp = activeTypingStatusObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    public void HideTypingIndicator()
    {
        if (activeTypingStatusObject == null) return;
        Destroy(activeTypingStatusObject);
        activeTypingStatusObject = null;
    }

    // ──────────────────────────────────────────────
    // Статус в шапке
    // ──────────────────────────────────────────────

    public void SetStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    // ──────────────────────────────────────────────
    // Подсказка ввода игрока
    // ──────────────────────────────────────────────

    public void ShowInputPrompt()
    {
        if (inputPromptText == null) return;
        inputPromptText.text = "[Написать сообщение:]";
        inputPromptText.gameObject.SetActive(true);

        StartButtonPulse();
    }

    public void HideInputPrompt()
    {
        if (inputPromptText != null)
            inputPromptText.gameObject.SetActive(false);

        StopButtonPulse();
    }

    // ──────────────────────────────────────────────
    // Кнопки выбора (спавн/удаление)
    // Ожидание выбора — в ScenarioManager
    // ──────────────────────────────────────────────

    /// <summary>
    /// Создаёт кнопки вариантов ответа. Возвращает индекс выбранного варианта через onSelected.
    /// Ожидание выбора управляется снаружи (ScenarioManager).
    /// </summary>
    public void SpawnChoiceButtons(List<ChatChoice> choices, System.Action<int> onSelected)
    {
        if (choiceButtonPrefab == null || choiceButtonsContainer == null)
        {
            Debug.LogError("[ChatManager] choiceButtonPrefab или choiceButtonsContainer не заданы!");
            return;
        }

        ClearChoiceButtons();

        for (int i = 0; i < choices.Count; i++)
        {
            int capturedIndex = i;
            GameObject btn = Instantiate(choiceButtonPrefab, choiceButtonsContainer);
            btn.name = $"ChoiceButton_{i}";

            // Поддерживаем и TMP и обычный Text
            var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = choices[i].text;
            else
            {
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null) txt.text = choices[i].text;
            }

            if (btn.TryGetComponent<Button>(out var button))
                button.onClick.AddListener(() => onSelected?.Invoke(capturedIndex));

            activeChoiceButtons.Add(btn);
        }
    }

    public void ClearChoiceButtons()
    {
        foreach (var btn in activeChoiceButtons)
        {
            if (btn != null) Destroy(btn);
        }
        activeChoiceButtons.Clear();
    }

    // ──────────────────────────────────────────────
    // Навигация по чатам
    // ──────────────────────────────────────────────

    /// <summary>Открыть чат по ID (сброс + воспроизведение сценария).</summary>
    public void OpenChat(string chatId)
    {
        Debug.Log($"[ChatManager] OpenChat called with chatId: '{chatId}'");
        if (selectedChatId == chatId && !wasOpenedBefore) return;

        selectedChatId = chatId;
        wasOpenedBefore = false;

        Chat chat = FindChatById(chatId);
        if (chat != null)
        {
            currentChatType = chat.chatType;
            UpdateChatHeader(chat);
        }
        else
        {
            chatHeaderName.text = "Чат";
            Debug.LogError($"[ChatManager] Чат '{chatId}' не найден!");
        }

        ClearMessagesFromScreen();
        HideInputPrompt();

        if (scenarioManager == null)
        {
            Debug.LogError("[ChatManager] scenarioManager не назначен!");
            return;
        }

        scenarioManager.ResetState();

        RestoreMessageHistory(chatId);

        chatItems[chatId]?.HideUnreadIndicator();

        ShowChatWindowInternal(); // <-- переключаем экран сюда

        scenarioManager.PlayDialogue(chatId);
    }
    
    public void SelectChat(string chatId) => OpenChat(chatId);

    public string GetSelectedChatId() => selectedChatId;

    public bool IsChatOpen(string chatId) =>
        !string.IsNullOrEmpty(selectedChatId) && selectedChatId == chatId;

    /// <summary>
    /// Вызывается DesktopManager при запуске — создаёт список ChatItem один раз.
    /// </summary>
    public void InitializeChatUI() => InitializeChatList();

    // ──────────────────────────────────────────────
    // Закрытие чата
    // ──────────────────────────────────────────────

    public void CloseChatWindow()
    {
        scenarioManager?.ResetState();
        ClearMessagesFromScreen();
        HideTypingIndicator();
        HideInputPrompt();

        // Сохраняем факт прочтения
        if (!string.IsNullOrEmpty(selectedChatId))
        {
            Chat chat = FindChatById(selectedChatId);
            //if (chat != null)
                //backgroundReceiver?.MarkChatAsRead(selectedChatId, chat.messages.Count);
        }
    }

    // ──────────────────────────────────────────────
    // Приватные методы
    // ──────────────────────────────────────────────

    private void InitializeChatList()
    {
        if (chatListInitialized) return;
        GameObject itemGOLine = Instantiate(chatItemPrefabLine, chatListContainer);
        foreach (Chat chat in chatDatabase.chats)
        {
            Sprite avatar = Resources.Load<Sprite>(chat.avatarPath);
            /*string lastMessage = chat.messages.Count > 0
                ? chat.messages[^1].text
                : "Нет сообщений";*/
            //Debug.Log($"[ChatManager] Creating ChatItem for chatId: '{chat.id}', name: '{chat.name}', avatarPath: '{chat.avatarPath}'");
            CreateChatItem(chat.id, chat.name, avatar);
        }

        chatListInitialized = true;
    }

    private void CreateChatItem(string id, string name, Sprite avatar)
    {
        GameObject itemGO = Instantiate(chatItemPrefab, chatListContainer);
        GameObject itemGOLine = Instantiate(chatItemPrefabLine, chatListContainer);
        if (!itemGO.TryGetComponent<ChatItem>(out var chatItem))
        {
            Debug.LogError("[ChatManager] ChatItem компонент не найден на prefab!");
            return;
        }

        chatItem.SetupChat(id, name, avatar,
            chatId => OpenChat(chatId));

        //backgroundReceiver?.RegisterChatItem(id, chatItem);
        chatItems[id] = chatItem;
    }

    private void UpdateChatHeader(Chat chat)
    {
        chatHeaderName.text = chat.name;
        Sprite avatar = Resources.Load<Sprite>(chat.avatarPath);
        if (avatar != null && chatHeaderAvatar != null)
            chatHeaderAvatar.sprite = avatar;
    }

    /// <summary>
    /// Мгновенно восстанавливает уже пройденные сообщения из истории (без анимации).
    /// </summary>
    private void RestoreMessageHistory(string chatId)
    {
        List<SavedChatMessage> history =
            gameData.GetChatHistory(chatId);

        foreach (var msg in history)
        {
            bool isPlayer = msg.senderId == "player";
            bool isImage = !string.IsNullOrEmpty(msg.imageId);

            if (isImage)
                AddImage(msg.imageId, isPlayer, msg.senderName);
            else
                AddMessage(msg.text, isPlayer, msg.senderName);
        }
    }

    private bool IsDisplayableMessage(ChatMessage msg)
    {
        if (msg == null)
            return false;

        if (msg.type == "choice")
            return false;

        if (IsGotoOnlyMessage(msg))
            return false;

        return !string.IsNullOrEmpty(msg.text)
            || !string.IsNullOrEmpty(msg.imageId);
    }

    private static bool IsGotoOnlyMessage(ChatMessage msg) =>
        !string.IsNullOrEmpty(msg.@goto) &&
        string.IsNullOrEmpty(msg.senderId) &&
        string.IsNullOrEmpty(msg.text);

    /// <summary>
    /// Отображает одно сообщение (текст или изображение) без добавления в историю.
    /// </summary>
    private void DisplayMessage(ChatMessage msg)
    {
        bool isPlayer = msg.senderId == "player";
        bool isImage  = !string.IsNullOrEmpty(msg.imageId);

        if (isImage) AddImage(msg.imageId, isPlayer, msg.senderName);
        else         AddMessage(msg.text,  isPlayer, msg.senderName);
    }

    private void RegisterDisplayedMessage(GameObject msgGO)
    {
        displayedMessages.Add(msgGO);

        if (displayedMessages.Count > maxMessageHistorySize)
            RemoveOldestDisplayedMessage();

        PlayMessageSound();
        ScrollToBottom();
    }

    private void RemoveOldestDisplayedMessage()
    {
        if (displayedMessages.Count == 0) return;

        Destroy(displayedMessages[0]);
        displayedMessages.RemoveAt(0);
    }

    private void ClearMessagesFromScreen()
    {
        foreach (var msg in displayedMessages)
        {
            if (msg != null) Destroy(msg);
        }
        displayedMessages.Clear();

        HideTypingIndicator();
    }

    private Chat FindChatById(string chatId)
    {
        var chat = chatDatabase?.chats.Find(c => c.id == chatId);
        if (chat == null)
            Debug.LogWarning($"[ChatManager] Чат '{chatId}' не найден в базе.");
        return chat;
    }

    private void PlayMessageSound()
    {
        if (SFXManager.Instance != null && !string.IsNullOrEmpty(messageAppearSoundName))
            SFXManager.Instance.PlaySound(messageAppearSoundName, messageSoundVolume);
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollView.verticalNormalizedPosition = 0f;
    }

    private void UnlockGalleryItem(string itemId)
    {
        var galMgr = galleryManager != null ? galleryManager : FindAnyObjectByType<GalleryManager>();
        if (galMgr == null) return;

        galMgr.UnlockGalleryItem(itemId);
    }

    private void SetEmptyChatState()
    {
        selectedChatId = null;

        // Заголовок
        if (chatHeaderName != null)
            chatHeaderName.text = string.Empty;

        if (chatHeaderAvatar != null)
            chatHeaderAvatar.sprite = null;

        if (statusText != null)
            statusText.text = string.Empty;

        // Очистка сообщений
        ClearMessagesFromScreen();

        // UI ввод
        HideInputPrompt();

        // выборы
        ClearChoiceButtons();

        // индикаторы
        HideTypingIndicator();
    }

    private void DumpChat(Chat chat)
    {
        Debug.Log($"===== CHAT DUMP: {chat.id} =====");

        for (int i = 0; i < chat.messages.Count; i++)
        {
            var msg = chat.messages[i];

            Debug.Log(
                $"[{i}] sender={msg.senderId}, " +
                $"text='{msg.text}', " +
                $"imageId='{msg.imageId}'"
            );
        }

        Debug.Log("===== END CHAT DUMP =====");
    }

    private void StartButtonPulse()
    {
        if (submitMessageButton == null) return;

        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        originalButtonScale = submitMessageButton.transform.localScale;
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void StopButtonPulse()
    {
        //Debug.Log("[ChatManager] Submit button clicked.");
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (submitMessageButton != null && originalButtonScale != Vector3.zero)
            submitMessageButton.transform.localScale = originalButtonScale;

        //Debug.Log("originalButtonScale reset to: " + originalButtonScale);
    }
    private IEnumerator PulseRoutine()
    {
        Transform t = submitMessageButton.transform;

        while (true)
        {
            // увеличиваем
            yield return ScaleTo(t, originalButtonScale * pulseScale, 0.5f);

            // уменьшаем
            yield return ScaleTo(t, originalButtonScale, 0.5f);
        }
    }

    private IEnumerator ScaleTo(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            target.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        target.localScale = targetScale;
    }
    public void OnSubmitButtonClicked()
    {
        Debug.Log("Кнопка нажата");
        StopButtonPulse();
        scenarioManager?.OnPlayerActionButtonPressed();
    }
}