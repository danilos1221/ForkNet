using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public class SavedChatMessage
{
    public string messageId;
    public string senderId;
    public string senderName;
    public string text;
    public string imageId;
    public bool isSystemMarker;
    public int dayNumber = 1;
}

[System.Serializable]
public class ChatHistory
{
    public string chatId;
    public List<SavedChatMessage> messages = new();
}



[System.Serializable]
public class ChatProgress
{
    public int messageIndex = 0;
    public bool isCompleted = false;
    public int unreadMessageCount = 0;  // Количество непрочитанных сообщений
    public int lastReadMessageIndex = -1;  // Индекс последнего прочитанного сообщения
    public string currentStatus = "";
}

[System.Serializable]
public class DayChatProgressEntry
{
    public string chatId;
    public ChatProgress progress = new ChatProgress();
}

[System.Serializable]
public class DayProgressState
{
    public int dayNumber = 1;
    public bool isCompleted = false;
    public List<DayChatProgressEntry> chatProgressEntries = new List<DayChatProgressEntry>();
}

[System.Serializable]
public class GalleryImageData
{
    public string itemId;
    public bool isUnlocked;

    public GalleryImageData(string itemId, bool isUnlocked = false)
    {
        this.itemId = itemId;
        this.isUnlocked = isUnlocked;
    }
}

[System.Serializable]
public class ChatCustomNameEntry
{
    public string chatKey;
    public string customName;
}

[System.Serializable]
public class DialogueScoreEntry
{
    public string key;
    public int value;
}

[System.Serializable]
public class GameData
{
    public int currentDay = 1;
    public List<DayProgressState> dayProgressStates = new List<DayProgressState>();

    public List<ChatHistory> chatHistories = new();
    public Dictionary<string, Character> characters = new Dictionary<string, Character>();
    [System.Obsolete("УСТАРЕЛО! Используйте новую систему JSON диалогов через DialogueScriptDatabase")]
    public Dictionary<string, Dialogue> dialogues = new Dictionary<string, Dialogue>();
    public Dictionary<string, int> characterAffection = new Dictionary<string, int>();
    public List<ChatCustomNameEntry> chatCustomNames = new List<ChatCustomNameEntry>();
    public List<DialogueScoreEntry> dialogueScores = new List<DialogueScoreEntry>();
    public List<GalleryImageData> galleryItems = new List<GalleryImageData>();
    //[System.Obsolete("Legacy поле. Используйте galleryItems с isUnlocked")]
    public List<string> unlockedGalleryItems = new List<string>();
    public Dictionary<string, ChatProgress> chatProgress = new Dictionary<string, ChatProgress>();

    // Игровой пайплайн событий (GameEventManager): именованные флаги истории и id уже сработавших одноразовых событий.
    public List<string> storyFlags = new List<string>();
    public List<string> firedEvents = new List<string>();

    public void AddCharacter(Character character)
    {
        characters[character.id] = character;
        characterAffection[character.id] = 0;
    }
    
    /// <summary>
    /// УСТАРЕЛО! Используйте новую систему JSON диалогов через DialogueScriptDatabase.GetDialogue()
    /// </summary>
    [System.Obsolete("Используйте новую систему JSON диалогов")]
    public void AddDialogue(Dialogue dialogue)
    {
        if (dialogues == null)
            dialogues = new Dictionary<string, Dialogue>();
        dialogues[dialogue.id] = dialogue;
    }
    
    public Character GetCharacter(string id)
    {
        return characters.ContainsKey(id) ? characters[id] : null;
    }
    
    /// <summary>
    /// УСТАРЕЛО! Используйте новую систему JSON диалогов через DialogueScriptDatabase.GetDialogue()
    /// </summary>
    [System.Obsolete("Используйте новую систему JSON диалогов")]
    public Dialogue GetDialogue(string id)
    {
        if (dialogues == null)
            return null;
        return dialogues.ContainsKey(id) ? dialogues[id] : null;
    }
    
    public int GetAffection(string characterId)
    {
        return characterAffection.ContainsKey(characterId) ? characterAffection[characterId] : 0;
    }
    
    public void ChangeAffection(string characterId, int change)
    {
        if (characterAffection.ContainsKey(characterId))
        {
            characterAffection[characterId] += change;
        }
    }

    public GalleryImageData GetGalleryItem(string itemId)
    {
        if (galleryItems == null || string.IsNullOrWhiteSpace(itemId))
            return null;

        for (int i = 0; i < galleryItems.Count; i++)
        {
            GalleryImageData item = galleryItems[i];
            if (item != null && item.itemId == itemId)
                return item;
        }

        return null;
    }

    public bool IsGalleryItemUnlocked(string itemId)
    {
        GalleryImageData item = GetGalleryItem(itemId);
        return item != null && item.isUnlocked;
    }

    public bool UnlockGalleryItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (galleryItems == null)
            galleryItems = new List<GalleryImageData>();

        MigrateLegacyUnlockedGallery();

        GalleryImageData item = GetGalleryItem(itemId);
        if (item == null)
        {
            item = new GalleryImageData(itemId, true);
            galleryItems.Add(item);
            return true;
        }

        if (item.isUnlocked)
            return false;

        item.isUnlocked = true;
        return true;
    }

    public List<GalleryImageData> GetGalleryItems()
    {
        if (galleryItems == null)
            galleryItems = new List<GalleryImageData>();

        MigrateLegacyUnlockedGallery();
        return galleryItems;
    }

    /// <summary>
    /// Нормализует состояние галереи: оставляет только валидные и разблокированные элементы,
    /// удаляет locked-записи от старой логики и дубликаты по itemId.
    /// </summary>
    public void CleanupGalleryItemsKeepUnlockedOnly()
    {
        if (galleryItems == null)
        {
            galleryItems = new List<GalleryImageData>();
            return;
        }

        MigrateLegacyUnlockedGallery();

        var uniqueIds = new HashSet<string>();
        var cleanedItems = new List<GalleryImageData>(galleryItems.Count);

        for (int i = 0; i < galleryItems.Count; i++)
        {
            GalleryImageData item = galleryItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            if (!item.isUnlocked)
                continue;

            if (!uniqueIds.Add(item.itemId))
                continue;

            cleanedItems.Add(new GalleryImageData(item.itemId, true));
        }

        galleryItems = cleanedItems;
    }

    private void MigrateLegacyUnlockedGallery()
    {
        if (unlockedGalleryItems == null || unlockedGalleryItems.Count == 0)
            return;

        if (galleryItems == null)
            galleryItems = new List<GalleryImageData>();

        for (int i = 0; i < unlockedGalleryItems.Count; i++)
        {
            string id = unlockedGalleryItems[i];
            if (string.IsNullOrWhiteSpace(id))
                continue;

            GalleryImageData item = GetGalleryItem(id);
            if (item == null)
            {
                galleryItems.Add(new GalleryImageData(id, true));
            }
            else
            {
                item.isUnlocked = true;
            }
        }

        unlockedGalleryItems.Clear();
    }

    public DayProgressState GetOrCreateDayProgressState(int dayNumber)
    {
        dayNumber = Mathf.Max(1, dayNumber);

        if (dayProgressStates == null)
            dayProgressStates = new List<DayProgressState>();

        for (int i = 0; i < dayProgressStates.Count; i++)
        {
            DayProgressState state = dayProgressStates[i];
            if (state != null && state.dayNumber == dayNumber)
                return state;
        }

        DayProgressState created = new DayProgressState
        {
            dayNumber = dayNumber,
            isCompleted = false,
            chatProgressEntries = new List<DayChatProgressEntry>()
        };

        dayProgressStates.Add(created);
        return created;
    }

    private DayChatProgressEntry GetOrCreateDayChatProgressEntry(DayProgressState dayState, string chatId)
    {
        if (dayState == null || string.IsNullOrWhiteSpace(chatId))
            return null;

        if (dayState.chatProgressEntries == null)
            dayState.chatProgressEntries = new List<DayChatProgressEntry>();

        for (int i = 0; i < dayState.chatProgressEntries.Count; i++)
        {
            DayChatProgressEntry entry = dayState.chatProgressEntries[i];
            if (entry != null && entry.chatId == chatId)
            {
                entry.progress ??= new ChatProgress();
                return entry;
            }
        }

        DayChatProgressEntry created = new DayChatProgressEntry
        {
            chatId = chatId,
            progress = new ChatProgress()
        };

        dayState.chatProgressEntries.Add(created);
        return created;
    }

    private ChatProgress GetOrCreateDayChatProgress(string chatId, int dayNumber)
    {
        DayProgressState dayState = GetOrCreateDayProgressState(dayNumber);
        DayChatProgressEntry entry = GetOrCreateDayChatProgressEntry(dayState, chatId);
        return entry?.progress;
    }

    private static ChatProgress CloneChatProgress(ChatProgress source)
    {
        if (source == null)
            return new ChatProgress();

        return new ChatProgress
        {
            messageIndex = source.messageIndex,
            isCompleted = source.isCompleted,
            unreadMessageCount = source.unreadMessageCount,
            lastReadMessageIndex = source.lastReadMessageIndex,
            currentStatus = source.currentStatus
        };
    }

    public void EnsureDayStateExists(int dayNumber)
    {
        GetOrCreateDayProgressState(dayNumber);
    }

    public void SetCurrentDay(int dayNumber)
    {
        currentDay = Mathf.Max(1, dayNumber);
        EnsureDayStateExists(currentDay);
    }

    public void MarkDayCompleted(int dayNumber)
    {
        DayProgressState state = GetOrCreateDayProgressState(dayNumber);
        state.isCompleted = true;
    }

    public void StartNextDay()
    {
        MarkDayCompleted(currentDay);
        SetCurrentDay(currentDay + 1);
    }

    public bool IsChatCompletedForDay(string chatId, int dayNumber)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return false;

        ChatProgress progress = GetOrCreateDayChatProgress(chatId, dayNumber);
        return progress != null && progress.isCompleted;
    }

    public bool CanEndCurrentDay(IEnumerable<string> requiredChatIds)
    {
        if (requiredChatIds == null)
            return true;

        foreach (string chatId in requiredChatIds)
        {
            if (string.IsNullOrWhiteSpace(chatId))
                continue;

            if (!IsChatCompletedForDay(chatId, currentDay))
                return false;
        }

        return true;
    }

    public void MigrateLegacyChatProgressToCurrentDay()
    {
        if (chatProgress == null || chatProgress.Count == 0)
            return;

        // Legacy-миграция должна выполняться только когда day-based прогресса
        // ещё нет вообще (старые сейвы). Иначе можно случайно перенести
        // completed-статусы из предыдущего дня в новый.
        if (HasAnyDayProgressEntries())
            return;

        DayProgressState dayState = GetOrCreateDayProgressState(currentDay);
        if (dayState.chatProgressEntries != null && dayState.chatProgressEntries.Count > 0)
            return;

        foreach (var kvp in chatProgress)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                continue;

            DayChatProgressEntry entry = GetOrCreateDayChatProgressEntry(dayState, kvp.Key);
            if (entry != null)
                entry.progress = CloneChatProgress(kvp.Value);
        }
    }

    private bool HasAnyDayProgressEntries()
    {
        if (dayProgressStates == null || dayProgressStates.Count == 0)
            return false;

        for (int i = 0; i < dayProgressStates.Count; i++)
        {
            DayProgressState state = dayProgressStates[i];
            if (state?.chatProgressEntries != null && state.chatProgressEntries.Count > 0)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Получить прогресс чата (индекс последнего сообщения)
    /// </summary>
    public int GetChatProgress(string chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return 0;

        MigrateLegacyChatProgressToCurrentDay();

        ChatProgress progress = GetOrCreateDayChatProgress(chatId, currentDay);
        return progress?.messageIndex ?? 0;
    }

    /// <summary>
    /// Сохранить прогресс чата
    /// </summary>
    public void SetChatProgress(string chatId, int messageIndex, bool isCompleted = false, string currentStatus = "")
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return;

        //Debug.Log($"[GameData.SetChatProgress] Сохранение прогресса для чата '{chatId}': messageIndex={messageIndex}, isCompleted={isCompleted}");
        MigrateLegacyChatProgressToCurrentDay();

        ChatProgress dayProgress = GetOrCreateDayChatProgress(chatId, currentDay);
        dayProgress.messageIndex = messageIndex;
        dayProgress.isCompleted = isCompleted;
        dayProgress.currentStatus = currentStatus;

        if (!chatProgress.ContainsKey(chatId))
            chatProgress[chatId] = new ChatProgress();
        
        chatProgress[chatId].messageIndex = messageIndex;
        chatProgress[chatId].isCompleted = isCompleted;
        chatProgress[chatId].currentStatus = currentStatus;
    }

    /// <summary>
    /// Проверить, завершён ли чат
    /// </summary>
    public bool IsChatCompleted(string chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return false;

        MigrateLegacyChatProgressToCurrentDay();
        return IsChatCompletedForDay(chatId, currentDay);
    }

    /// <summary>
    /// Очистить прогресс конкретного чата
    /// </summary>
    public void ResetChatProgress(string chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return;

        MigrateLegacyChatProgressToCurrentDay();

        ChatProgress dayProgress = GetOrCreateDayChatProgress(chatId, currentDay);
        if (dayProgress != null)
        {
            dayProgress.messageIndex = 0;
            dayProgress.isCompleted = false;
            dayProgress.currentStatus = "";
        }

        if (chatProgress.ContainsKey(chatId))
            chatProgress[chatId] = new ChatProgress();
    }

    /// <summary>
    /// Получить количество непрочитанных сообщений в чате
    /// </summary>
    public int GetUnreadMessageCount(string chatId)
    {
        if (chatProgress.ContainsKey(chatId))
        {
            int count = chatProgress[chatId].unreadMessageCount;
            Debug.Log($"[GameData.GetUnreadMessageCount] Чат '{chatId}': {count} непрочитанных");
            return count;
        }
        Debug.Log($"[GameData.GetUnreadMessageCount] Чат '{chatId}' не найден в chatProgress, возвращаю 0");
        return 0;
    }

    /// <summary>
    /// Увеличить счетчик непрочитанных сообщений (для сообщений полученных в фоне)
    /// </summary>
    public void AddUnreadMessage(string chatId)
    {
        if (!chatProgress.ContainsKey(chatId))
        {
            chatProgress[chatId] = new ChatProgress();
            Debug.Log($"[GameData.AddUnreadMessage] Создан новый ChatProgress для '{chatId}'");
        }
        
        chatProgress[chatId].unreadMessageCount++;
        Debug.Log($"[GameData.AddUnreadMessage] Для чата '{chatId}' увеличен счетчик на {chatProgress[chatId].unreadMessageCount}");
    }

    /// <summary>
    /// Пометить все сообщения в чате как прочитанные
    /// </summary>
    public void MarkChatAsRead(string chatId, int totalMessages)
    {
        if (!chatProgress.ContainsKey(chatId))
            chatProgress[chatId] = new ChatProgress();
        
        chatProgress[chatId].unreadMessageCount = 0;
        chatProgress[chatId].lastReadMessageIndex = totalMessages - 1;
    }

    public void EnsureDayDividerInHistory(string chatId, int dayNumber)
    {
        if (string.IsNullOrWhiteSpace(chatId))
            return;

        dayNumber = Mathf.Max(1, dayNumber);
        string dividerId = $"__day_divider_{dayNumber}";

        ChatHistory history = chatHistories.Find(h => h.chatId == chatId);

        if (history == null)
        {
            history = new ChatHistory
            {
                chatId = chatId
            };

            chatHistories.Add(history);
        }

        if (history.messages.Exists(m => m != null && m.messageId == dividerId))
            return;

        var divider = new SavedChatMessage
        {
            messageId = dividerId,
            senderId = "system",
            senderName = "System",
            text = $"--- DAY {dayNumber} ---",
            imageId = string.Empty,
            isSystemMarker = true,
            dayNumber = dayNumber
        };

        // Вставляем перед первым уже существующим сообщением этого дня —
        // сообщения нового дня иногда успевают прийти в историю (например,
        // через фоновую доставку) раньше, чем сюда доходит вызов
        // EnsureDayDividerInHistory. Если добавлять всегда в конец списка,
        // разделитель оказывается ПОСЛЕ таких сообщений вместо того, чтобы
        // открывать день перед ними.
        int insertIndex = history.messages.FindIndex(m => m != null && m.dayNumber >= dayNumber);

        if (insertIndex < 0)
            history.messages.Add(divider);
        else
            history.messages.Insert(insertIndex, divider);
    }

    public void AddMessageToHistory(string chatId, ChatMessage message)
    {
        ChatHistory history = chatHistories.Find(h => h.chatId == chatId);

        if (history == null)
        {
            history = new ChatHistory
            {
                chatId = chatId
            };

            chatHistories.Add(history);
        }

        history.messages.Add(new SavedChatMessage
        {
            messageId = message.id,
            senderId = message.senderId,
            senderName = message.senderName,
            text = message.text,
            imageId = message.imageId,
            isSystemMarker = false,
            dayNumber = currentDay
        });
    }
    public List<SavedChatMessage> GetChatHistory(string chatId)
    {
        ChatHistory history = chatHistories.Find(h => h.chatId == chatId);

        return history?.messages ?? new List<SavedChatMessage>();
    }

    public void ClearChatHistory(string chatId)
    {
        ChatHistory history = chatHistories.Find(h => h.chatId == chatId);

        if (history != null)
            history.messages.Clear();
    }

    public bool HasMessageInHistory(
    string chatId,
    string messageId)
    {
        ChatHistory history =
            chatHistories.Find(h => h.chatId == chatId);

        if (history == null)
            return false;

        return history.messages.Exists(
            m => m.messageId == messageId);
    }

    // ──────────────────────────────────────────────
    // Игровой пайплайн событий (GameEventManager)
    // ──────────────────────────────────────────────

    public bool HasStoryFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && storyFlags != null && storyFlags.Contains(flag);
    }

    public void SetStoryFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
            return;

        storyFlags ??= new List<string>();

        if (!storyFlags.Contains(flag))
            storyFlags.Add(flag);
    }

    public void ClearStoryFlag(string flag)
    {
        storyFlags?.Remove(flag);
    }

    public bool HasFiredEvent(string eventId)
    {
        return !string.IsNullOrWhiteSpace(eventId) && firedEvents != null && firedEvents.Contains(eventId);
    }

    public void MarkEventFired(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return;

        firedEvents ??= new List<string>();

        if (!firedEvents.Contains(eventId))
            firedEvents.Add(eventId);
    }

    public int GetDialogueScore(string scoreKey)
    {
        scoreKey = NormalizeChatKey(scoreKey);
        if (string.IsNullOrWhiteSpace(scoreKey))
            return 0;

        dialogueScores ??= new List<DialogueScoreEntry>();

        for (int i = 0; i < dialogueScores.Count; i++)
        {
            DialogueScoreEntry entry = dialogueScores[i];
            if (entry == null || entry.key != scoreKey)
                continue;

            return entry.value;
        }

        return 0;
    }

    public void SetDialogueScore(string scoreKey, int value)
    {
        scoreKey = NormalizeChatKey(scoreKey);
        if (string.IsNullOrWhiteSpace(scoreKey))
            return;

        dialogueScores ??= new List<DialogueScoreEntry>();

        for (int i = 0; i < dialogueScores.Count; i++)
        {
            DialogueScoreEntry entry = dialogueScores[i];
            if (entry == null || entry.key != scoreKey)
                continue;

            entry.value = value;
            return;
        }

        dialogueScores.Add(new DialogueScoreEntry
        {
            key = scoreKey,
            value = value
        });
    }

    public int AddDialogueScore(string scoreKey, int delta)
    {
        int nextValue = GetDialogueScore(scoreKey) + delta;
        SetDialogueScore(scoreKey, nextValue);
        return nextValue;
    }

    public string GetCustomChatName(string chatKey)
    {
        chatKey = NormalizeChatKey(chatKey);
        if (string.IsNullOrWhiteSpace(chatKey))
            return string.Empty;

        chatCustomNames ??= new List<ChatCustomNameEntry>();

        for (int i = 0; i < chatCustomNames.Count; i++)
        {
            ChatCustomNameEntry entry = chatCustomNames[i];
            if (entry == null || entry.chatKey != chatKey)
                continue;

            return entry.customName ?? string.Empty;
        }

        return string.Empty;
    }

    public void SetCustomChatName(string chatKey, string customName)
    {
        chatKey = NormalizeChatKey(chatKey);
        if (string.IsNullOrWhiteSpace(chatKey))
            return;

        string normalizedName = NormalizeCustomChatName(customName);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            ResetCustomChatName(chatKey);
            return;
        }

        chatCustomNames ??= new List<ChatCustomNameEntry>();

        for (int i = 0; i < chatCustomNames.Count; i++)
        {
            ChatCustomNameEntry entry = chatCustomNames[i];
            if (entry == null || entry.chatKey != chatKey)
                continue;

            entry.customName = normalizedName;
            return;
        }

        chatCustomNames.Add(new ChatCustomNameEntry
        {
            chatKey = chatKey,
            customName = normalizedName
        });
    }

    public void ResetCustomChatName(string chatKey)
    {
        chatKey = NormalizeChatKey(chatKey);
        if (string.IsNullOrWhiteSpace(chatKey) || chatCustomNames == null)
            return;

        chatCustomNames.RemoveAll(e => e != null && e.chatKey == chatKey);
    }

    public static string ResolveChatDisplayName(GameData gameData, string chatKey, string originalName)
    {
        string fallback = string.IsNullOrWhiteSpace(originalName) ? "Chat" : originalName;
        if (gameData == null)
            return fallback;

        string customName = gameData.GetCustomChatName(chatKey);
        return string.IsNullOrWhiteSpace(customName) ? fallback : customName;
    }

    private static string NormalizeChatKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
    }

    private static string NormalizeCustomChatName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        string trimmed = name.Trim();
        return trimmed.Length > 32 ? trimmed.Substring(0, 32) : trimmed;
    }

}