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
public class GameData
{
    public List<ChatHistory> chatHistories = new();
    public Dictionary<string, Character> characters = new Dictionary<string, Character>();
    [System.Obsolete("УСТАРЕЛО! Используйте новую систему JSON диалогов через DialogueScriptDatabase")]
    public Dictionary<string, Dialogue> dialogues = new Dictionary<string, Dialogue>();
    public Dictionary<string, int> characterAffection = new Dictionary<string, int>();
    public List<GalleryImageData> galleryItems = new List<GalleryImageData>();
    //[System.Obsolete("Legacy поле. Используйте galleryItems с isUnlocked")]
    public List<string> unlockedGalleryItems = new List<string>();
    public Dictionary<string, ChatProgress> chatProgress = new Dictionary<string, ChatProgress>();
    
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

    public void EnsureGalleryItems(IEnumerable<string> allItemIds)
    {
        if (galleryItems == null)
            galleryItems = new List<GalleryImageData>();

        MigrateLegacyUnlockedGallery();

        if (allItemIds == null)
            return;

        foreach (string id in allItemIds)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (GetGalleryItem(id) == null)
                galleryItems.Add(new GalleryImageData(id, false));
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

    /// <summary>
    /// Получить прогресс чата (индекс последнего сообщения)
    /// </summary>
    public int GetChatProgress(string chatId)
    {
        if (chatProgress.ContainsKey(chatId))
            return chatProgress[chatId].messageIndex;
        return 0;
    }

    /// <summary>
    /// Сохранить прогресс чата
    /// </summary>
    public void SetChatProgress(string chatId, int messageIndex, bool isCompleted = false, string currentStatus = "")
    {
        //Debug.Log($"[GameData.SetChatProgress] Сохранение прогресса для чата '{chatId}': messageIndex={messageIndex}, isCompleted={isCompleted}");
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
        if (chatProgress.ContainsKey(chatId))
            return chatProgress[chatId].isCompleted;
        return false;
    }

    /// <summary>
    /// Очистить прогресс конкретного чата
    /// </summary>
    public void ResetChatProgress(string chatId)
    {
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
            imageId = message.imageId
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

}
