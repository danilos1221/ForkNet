using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class ChatChoice
{
    public string text;
    public string @goto;  // Куда перейти при выборе этого варианта (используется @ для экранирования зарезервированного слова)
}

[System.Serializable]
public class ChatMessage
{
    public string senderId;
    public string senderName;
    public string text;
    public string timestamp;
    public string senderColor = "#FFFFFF";
    public string imageId;  // ID изображения (если есть), по которому загружаем спрайт
    
    // ✅ НОВОЕ: Поддержка выборов и навигации по VN
    public string id;  // ID сообщения для ссылок (goto)
    public string type;  // Тип сообщения ("choice" для выборов)
    public List<ChatChoice> choices;  // Варианты выбора (если type == "choice")
    public string @goto;  // Переход на сообщение с этим id (используется @ для экранирования зарезервированного слова)
}

public enum ChatType
{
    Private,
    Group
}

[System.Serializable]
public class Chat
{
    public string id;
    public string name;
    public string status;
    public string avatarPath;  // Путь к ресурсу: "Images/Avatars/alice"
    public ChatType chatType;
    public List<ChatMessage> messages = new List<ChatMessage>();
}

[System.Serializable]
public class ChatDatabaseContainer
{
    public List<Chat> chats = new List<Chat>();
}

[System.Serializable]
public class ChatDatabase
{
    public static ChatDatabase LoadFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("Chats/chats");
        
        if (jsonFile == null)
        {
            Debug.LogError("Chats JSON file not found at 'Resources/Chats/chats_prod.json'!");
            return new ChatDatabase();
        }

        try
        {
            ChatDatabaseContainer container = JsonUtility.FromJson<ChatDatabaseContainer>(jsonFile.text);
            ChatDatabase db = new ChatDatabase();
            db.chats = container.chats;
            return db;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing chats JSON: {e.Message}");
            return new ChatDatabase();
        }
    }
    public static ChatDatabase Load()
    {
        ChatDatabase db = new ChatDatabase();

        TextAsset[] files = Resources.LoadAll<TextAsset>("Chats");

        foreach (TextAsset file in files)
        {
            try
            {
                Chat chat = ChatParser.Parse(file.text);
                db.chats.Add(chat);
                //Debug.Log($"Loaded chat: {chat.id} from file: {file.name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка чтения {file.name}: {e}");
            }
        }
        return db;
    }

    
    public List<Chat> chats = new List<Chat>();
}
