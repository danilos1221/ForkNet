using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Отвечает ТОЛЬКО за логику сценария:
///   - воспроизведение последовательности сообщений из чата
///   - ожидание ввода игрока (пробел / кнопка)
///   - выбор вариантов ответа
///   - сохранение прогресса
///   - фоновая доставка сообщений при закрытом чате
/// UI обновляется через ChatManagerAdvanced.
/// </summary>
public class ScenarioManager : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector-поля
    // ──────────────────────────────────────────────
    private int seqCounter;
    private int dialogueGeneration;
    private int inputVersion;
    [Header("Зависимости")]
    [SerializeField] private ChatManagerAdvanced chatManager;
    //[SerializeField] private BackgroundMessageReceiver backgroundReceiver;

    [Header("Тайминги")]
    [SerializeField] private float dialogueSpeedMultiplier = 1f;
    [SerializeField] private float delayBetweenMessages    = 0.5f;
    [SerializeField] private float typingAnimationSpeed    = 0.3f;

    // ──────────────────────────────────────────────
    // Приватные поля
    // ──────────────────────────────────────────────

    private GameData     gameData;
    private ChatDatabase chatDatabase;

    private Chat   currentChat;
    private string activeChatId;     // ID чата, диалог которого сейчас воспроизводится
    private string currentMessageId; // ID сообщения, на котором стоит воспроизведение

    private bool isShowingNPCMessage; // флаг анимации печатания НПС

    private Coroutine playSequenceCoroutine;
    private Coroutine typingAnimationCoroutine;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (chatManager == null)
            chatManager = FindAnyObjectByType<ChatManagerAdvanced>();
        //[if (backgroundReceiver == null)
        //    backgroundReceiver = BackgroundMessageReceiver.Instance;
    }

    private void Start()
    {
        chatDatabase = GameManager.Instance.ChatDatabase;
        gameData     = GameManager.Instance.GameData ?? new GameData();
    }

    private void Update()
    {
    }


    /// <summary>Вызывается кнопкой «Отправить» из ChatManagerAdvanced.</summary>
    public void OnPlayerActionButtonPressed() => inputVersion++;

    public void ResetState()
    {
        dialogueGeneration++; // 🔥 инвалидируем ВСЕ старые корутины

        //Debug.Log($"ResetState gen={dialogueGeneration}");

        SavePendingNPCMessageIfNeeded();

        activeChatId = "";

        StopCoroutineSafe(ref playSequenceCoroutine);
        StopCoroutineSafe(ref typingAnimationCoroutine);

        chatManager?.HideTypingIndicator();
        chatManager?.ClearChoiceButtons();

        isShowingNPCMessage = false;
    }

    /// <summary>Запустить воспроизведение диалога из чата с указанным ID.</summary>
    public void PlayDialogue(string chatId)
    {
        //Debug.Log($"PlayDialogue chat={chatId} gen={dialogueGeneration}");
        
        currentChat = chatDatabase.chats.Find(c => c.id == chatId);
        
        if (currentChat == null)
        {
            Debug.LogError($"Chat not found: {chatId}");
            return;
        }
        currentChat.status = "В сети";
        activeChatId = chatId;
        currentMessageId = ResolveStartMessageId(chatId);

        if (string.IsNullOrEmpty(currentMessageId))
            return;

        StopCoroutineSafe(ref playSequenceCoroutine);

        int gen = dialogueGeneration;
        playSequenceCoroutine = StartCoroutine(PlayChatSequence(gen));
    }

    // ──────────────────────────────────────────────
    // Главный цикл воспроизведения
    // ──────────────────────────────────────────────

    private IEnumerator PlayChatSequence(int gen)
    {
        if (currentChat == null || currentChat.messages.Count == 0)
            yield break;

        int id = ++seqCounter;
        //Debug.Log($"SEQ {id} START gen={gen}");

        try
        {
            currentChat.status = "В сети";

            ChatMessage current = ResolveJumpTarget(
                FindMessageById(currentMessageId) ?? currentChat.messages[0]);

            while (current != null)
            {
                Debug.Log($"LOOP STEP current={(current != null ? current.id : "NULL")}");
                if (gen != dialogueGeneration)
                    yield break;

                string processingChatId = currentChat.id;
                
                if (current.type == "choice")
                {
                    yield return StartCoroutine(HandleChoiceMessage(current));
                    current = ResolveJumpTarget(FindMessageById(currentMessageId));
                }
                else if (IsGotoOnlyMessage(current))
                {
                    current = JumpTo(current.@goto);
                }
                else
                {
                    Debug.Log($"SEQ {id} PROCESSING chat={processingChatId} msg={current.type} gen={gen}");
                    yield return StartCoroutine(
                        HandleRegularMessage(current, processingChatId, gen));

                    current = GetNextMessage(current);
                    Debug.Log($"SEQ {id} NEXT chat={processingChatId} msg={(current != null ? current.type : "<end>")} gen={gen}");
                }

                SaveProgress(current);
                /*
                if (current.id == "END")
                {
                    break;
                }*/

            }
        }
        finally
        {
            Debug.Log($"SEQ {id} END gen={gen}");
        }
    }

    // ──────────────────────────────────────────────
    // Обработка типов сообщений
    // ──────────────────────────────────────────────

    private IEnumerator HandleChoiceMessage(ChatMessage choiceMessage)
    {
        if (chatManager == null) yield break;

        int selectedIndex = -1;

        chatManager.SpawnChoiceButtons(
            choiceMessage.choices,
            idx => selectedIndex = idx);

        yield return new WaitUntil(() => selectedIndex >= 0);

        chatManager.ClearChoiceButtons();

        if (selectedIndex >= 0 && selectedIndex < choiceMessage.choices.Count)
        {
            ChatChoice selected = choiceMessage.choices[selectedIndex];

            ChatMessage selectedChoiceMessage = new ChatMessage
            {
                id = $"{choiceMessage.id}_choice_{selectedIndex}",
                senderId = "player",
                senderName = "Ты",
                text = selected.text,
                timestamp = ""
            };

            chatManager.AddMessage(selectedChoiceMessage.text, true);

            if (!gameData.HasMessageInHistory(currentChat.id, selectedChoiceMessage.id))
                gameData.AddMessageToHistory(currentChat.id, selectedChoiceMessage);

            currentMessageId = selected.@goto;
        }
        else
        {
            Debug.LogWarning("[ScenarioManager] Выбор вышел за пределы диапазона.");
        }
    }

    private IEnumerator HandleRegularMessage(ChatMessage message, string chatId, int gen)
    {
        if (message.senderId == "player")
            yield return HandlePlayerMessage(message, chatId, gen);
        else
            yield return HandleNPCMessage(message, chatId, gen);
    }

    private IEnumerator HandlePlayerMessage(ChatMessage message, string chatId, int gen)
    {
        //Debug.Log($"WAIT START chat={chatId} gen={gen}");

        chatManager?.ShowInputPrompt();

        inputVersion++;
        int myInput = inputVersion;

        //Debug.Log($"dialogueGeneration={dialogueGeneration} inputVersion={inputVersion} myInput={myInput} chat={chatId} gen={gen}");
        //Debug.Log($"PLAYER WAIT START msg={message.id}");
        yield return new WaitUntil(() =>
            gen == dialogueGeneration &&
            inputVersion > myInput &&
            IsProcessingChatActive(chatId));
        //Debug.Log($"PLAYER WAIT END msg={message.id}");
        if (gen != dialogueGeneration)
            yield break;
        
        //Debug.Log($"WAIT END chat={chatId} gen={gen}");

        bool isImage = !string.IsNullOrEmpty(message.imageId);

        if (isImage)
            chatManager?.AddImage(message.imageId, true);
        else
            chatManager?.AddMessage(message.text, true);

        chatManager?.HideInputPrompt();
        if (!gameData.HasMessageInHistory(
                chatId,
                message.id))
        {
            gameData.AddMessageToHistory(
                chatId,
                message
            );
        }
    }

    private IEnumerator HandleNPCMessage(ChatMessage message, string chatId, int gen)
    {
        yield return new WaitForSeconds(delayBetweenMessages / dialogueSpeedMultiplier);

        if (gen != dialogueGeneration)
            yield break;

        bool isImage = !string.IsNullOrEmpty(message.imageId);

        if (!isImage && IsProcessingChatActive(chatId))
        {
            isShowingNPCMessage = true;
            //Debug.Log($"NPC MESSAGE START chat={chatId} gen={gen}");
            chatManager?.ShowTypingIndicator();
            typingAnimationCoroutine = StartCoroutine(TypingAnimation());
        }

        yield return new WaitForSeconds(delayBetweenMessages / dialogueSpeedMultiplier);

        StopCoroutineSafe(ref typingAnimationCoroutine);
        isShowingNPCMessage = false;
        chatManager?.HideTypingIndicator();
        chatManager?.SetStatusText("В сети");
        if (gen != dialogueGeneration)
            yield break;

        DeliverNPCMessage(message, chatId);
        if (!gameData.HasMessageInHistory(
                chatId,
                message.id))
        {
            gameData.AddMessageToHistory(
                chatId,
                message
            );
        }
    }

    // ──────────────────────────────────────────────
    // Доставка сообщения НПС (с учётом фонового режима)
    // ──────────────────────────────────────────────

    /// <summary>
    /// Если чат всё ещё открыт — добавляем сообщение в UI.
    /// Если чат закрыт или переключён — пишем в историю фоном + помечаем непрочитанным.
    /// </summary>
    private void DeliverNPCMessage(ChatMessage message, string targetChatId)
    {
        if (IsProcessingChatActive(targetChatId))
        {
            chatManager?.AddMessageToChat(targetChatId, message);
        }
        else
        {
            chatManager?.AddMessageToChat(targetChatId, message);
            //backgroundReceiver?.MarkMessageAsUnread(targetChatId);
        }
    }

    // ──────────────────────────────────────────────
    // Прогресс
    // ──────────────────────────────────────────────

    private void SaveProgress(ChatMessage nextMessage)
    {
        if (gameData == null || currentChat == null) return;

        int nextIndex;
        bool isCompleted;

        if (nextMessage == null)
        {
            nextIndex   = currentChat.messages.Count;
            isCompleted = true;
        }
        else
        {
            nextIndex = currentChat.messages.IndexOf(nextMessage);
            if (nextIndex < 0)
            {
                nextIndex   = currentChat.messages.Count;
                isCompleted = true;
            }
            else
            {
                isCompleted = false;
            }
        }

        gameData.SetChatProgress(currentChat.id, nextIndex, isCompleted);
    }

    /// <summary>
    /// Если при сбросе состояния шло НПС-сообщение —
    /// сохраняем его в историю чата, чтобы игрок не потерял его.
    /// </summary>
    private void SavePendingNPCMessageIfNeeded()
    {
        if (!isShowingNPCMessage || currentChat == null) return;

        ChatMessage pending = FindMessageById(currentMessageId);
        if (pending == null) return;

        string pendingChatId = currentChat.id;

        chatManager?.AddMessageToChat(pendingChatId, pending);

        int pendingIndex = currentChat.messages.IndexOf(pending);
        int nextIndex    = Mathf.Max(0, pendingIndex) + 1;
        bool isCompleted = nextIndex >= currentChat.messages.Count;

        gameData?.SetChatProgress(pendingChatId, nextIndex, isCompleted);
    }

    // ──────────────────────────────────────────────
    // Навигация по сообщениям
    // ──────────────────────────────────────────────

    private string ResolveStartMessageId(string chatId)
    {
        if (gameData == null) return GetFirstMessageId();

        int savedIndex = gameData.GetChatProgress(chatId);

        if (savedIndex <= 0)
            return GetFirstMessageId();

        if (savedIndex >= currentChat.messages.Count)
            return ""; // чат завершён

        var msg = currentChat.messages[savedIndex];
        EnsureMessageHasId(msg, savedIndex);

        ChatMessage playable = ResolveJumpTarget(msg);
        return playable != null ? playable.id : "";
    }

    private string GetFirstMessageId()
    {
        if (currentChat == null || currentChat.messages.Count == 0)
            return "";

        var first = currentChat.messages[0];
        EnsureMessageHasId(first, 0);

        ChatMessage playable = ResolveJumpTarget(first);
        return playable != null ? playable.id : "";
    }

    private ChatMessage GetNextMessage(ChatMessage current)
    {
        if (current == null || currentChat == null) return null;

        if (!string.IsNullOrEmpty(current.@goto))
            return JumpTo(current.@goto);

        int index = currentChat.messages.IndexOf(current);
        if (index < 0 || index >= currentChat.messages.Count - 1)
            return null;

        return ResolveLinearNext(index + 1);
    }

    private ChatMessage JumpTo(string targetId)
    {
        currentMessageId = targetId;
        return ResolveJumpTarget(FindMessageById(targetId));
    }

    private ChatMessage FindMessageById(string messageId)
    {
        if (string.IsNullOrEmpty(messageId) || currentChat == null) return null;

        foreach (var msg in currentChat.messages)
        {
            if (msg.id == messageId) return msg;
        }

        Debug.LogWarning($"[ScenarioManager] Сообщение с id '{messageId}' не найдено в чате '{currentChat.id}'.");
        return null;
    }


    private static void EnsureMessageHasId(ChatMessage msg, int index)
    {
        if (string.IsNullOrEmpty(msg.id))
            msg.id = $"msg_{index}";
    }

    private ChatMessage ResolveJumpTarget(ChatMessage message)
    {
        ChatMessage current = message;

        while (current != null && IsLabelMarker(current))
        {
            int index = currentChat.messages.IndexOf(current);
            if (index < 0 || index >= currentChat.messages.Count - 1)
                return null;

            current = currentChat.messages[index + 1];
            EnsureMessageHasId(current, index + 1);
        }

        if (current != null && IsEndBlockMessage(current))
            return ResolveLinearNext(currentChat.messages.IndexOf(current) + 1);

        if (current != null)
            currentMessageId = current.id;

        return current;
    }

    private ChatMessage ResolveLinearNext(int startIndex)
    {
        if (currentChat == null)
            return null;

        int index = startIndex;

        while (index < currentChat.messages.Count)
        {
            ChatMessage current = currentChat.messages[index];
            EnsureMessageHasId(current, index);

            if (IsEndBlockMessage(current))
            {
                index++;

                while (index < currentChat.messages.Count && IsLabelMarker(currentChat.messages[index]))
                    index = SkipLabeledBlock(index);

                continue;
            }

            if (IsLabelMarker(current))
            {
                index++;
                continue;
            }

            currentMessageId = current.id;
            return current;
        }

        return null;
    }

    private int SkipLabeledBlock(int labelIndex)
    {
        int index = labelIndex + 1;

        while (index < currentChat.messages.Count)
        {
            ChatMessage current = currentChat.messages[index];
            EnsureMessageHasId(current, index);

            if (IsEndBlockMessage(current))
                return index + 1;

            index++;
        }

        return index;
    }

    // ──────────────────────────────────────────────
    // Вспомогательные проверки
    // ──────────────────────────────────────────────

    private bool IsCurrentChatActive() =>
        !string.IsNullOrEmpty(activeChatId) && activeChatId == currentChat?.id;


    private bool IsProcessingChatActive(string processingChatId) =>
        !string.IsNullOrEmpty(activeChatId) && activeChatId == processingChatId;

    /// <summary>Сообщение-переход: только goto, без текста и отправителя.</summary>
    private static bool IsGotoOnlyMessage(ChatMessage msg) =>
        !string.IsNullOrEmpty(msg.@goto) &&
        string.IsNullOrEmpty(msg.senderId) &&
        string.IsNullOrEmpty(msg.text);

    /// <summary>Маркер начала именованного блока, который используется как точка входа для перехода.</summary>
    private static bool IsLabelMarker(ChatMessage msg) =>
        msg != null &&
        (string.Equals(msg.type, "label", System.StringComparison.Ordinal) ||
         IsLabelOnlyMessage(msg));

    /// <summary>Маркер конца текущего блока ветвления.</summary>
    private static bool IsEndBlockMessage(ChatMessage msg) =>
        msg != null &&
        string.Equals(msg.type, "block_end", System.StringComparison.Ordinal);

    /// <summary>Служебный label старого формата без type, который нужен только как точка входа для перехода.</summary>
    private static bool IsLabelOnlyMessage(ChatMessage msg) =>
        msg != null &&
        !string.IsNullOrEmpty(msg.id) &&
        string.IsNullOrEmpty(msg.type) &&
        string.IsNullOrEmpty(msg.senderId) &&
        string.IsNullOrEmpty(msg.text) &&
        string.IsNullOrEmpty(msg.@goto);

    private void StopCoroutineSafe(ref Coroutine coroutine)
    {
        //Debug.Log($"StopCoroutineSafe: coroutine = {(coroutine != null ? "OK" : "NULL")}");
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    // ──────────────────────────────────────────────
    // Анимация печатания
    // ──────────────────────────────────────────────

    private IEnumerator TypingAnimation()
    {
        int dots = 0;
        while (isShowingNPCMessage)
        {
            string text = "пишет" + new string('.', dots + 1);
            chatManager?.SetStatusText(text);
            chatManager?.UpdateTypingIndicatorText(text);

            dots = (dots + 1) % 3;
            yield return new WaitForSeconds(typingAnimationSpeed);
        }
    }
}