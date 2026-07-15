using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

[System.Serializable]
public class ChatProgressEntry
{
    public string chatId;
    public ChatProgress progress;
}

[System.Serializable]
public class GameSave
{
    public Dictionary<string, int> characterAffection;
    public List<GalleryImageData> galleryItems;
    public List<string> unlockedGalleryItems;
    public List<ChatProgressEntry> chatProgressList = new List<ChatProgressEntry>();
    public string saveDate;  // Дата сохранения
    public List<ChatHistory> chatHistories = new List<ChatHistory>();
}

[System.Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public string saveDate;
    public bool isEmpty;
}

[System.Serializable]
public class DesktopLayout
{
    public Dictionary<string, Vector2> iconPositions = new Dictionary<string, Vector2>();
    
    public void SaveIconPosition(string iconId, Vector2 position)
    {
        iconPositions[iconId] = position;
    }
    
    public Vector2 GetIconPosition(string iconId, Vector2 defaultPos)
    {
        return iconPositions.ContainsKey(iconId) ? iconPositions[iconId] : defaultPos;
    }
}

public class SaveSystem : MonoBehaviour
{
    private const int MAX_SAVE_SLOTS = 5;
    private string savePath;
    private GameData gameData;
    
    private void Awake()
    {
        // Инициализируем путь в Awake, а не в Start, чтобы он был готов раньше
        savePath = Application.persistentDataPath;
        
        // Создаем папку для сохранений если её нет
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

    }
    private void Start() {
        gameData = GameManager.Instance.GameData;
    }

    
    /// <summary>
    /// Сохранить игру в определенный слот
    /// </summary>
    public void SaveGameToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
        {
            Debug.LogError("Некорректный индекс слота: " + slotIndex);
            return;
        }
        
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("SavePath не инициализирован!");
            return;
        }
        
        if (gameData == null)
        {
            Debug.LogError("GameData не инициализирована!");
            return;
        }
        
        GameSave save = new GameSave
        {
            characterAffection = gameData.characterAffection ?? new Dictionary<string, int>(),
            galleryItems = gameData.GetGalleryItems() ?? new List<GalleryImageData>(),
            unlockedGalleryItems = gameData.unlockedGalleryItems ?? new List<string>(),

            // НОВОЕ
            chatHistories = gameData.chatHistories ?? new List<ChatHistory>(),

            saveDate = System.DateTime.Now.ToString("dd.MM.yyyy HH:mm")
        };
        Debug.Log("Character Affection Count: " + save.characterAffection.Count);
        Debug.Log("Gallery Items Count: " + save.galleryItems.Count);
        Debug.Log("Unlocked Gallery Items Count: " + save.unlockedGalleryItems.Count);
        
        
        // Конвертируем Dictionary в List для сериализации
        save.chatProgressList = new List<ChatProgressEntry>();
        if (gameData.chatProgress != null)
        {
            foreach (var kvp in gameData.chatProgress)
            {
                save.chatProgressList.Add(new ChatProgressEntry
                {
                    chatId = kvp.Key,
                    progress = kvp.Value
                });
            }
        }
        Debug.Log("Chat Progress Count: " + save.chatProgressList.Count);
        
        string slotPath = Path.Combine(savePath, $"savegame_{slotIndex}.json");
        string json = JsonUtility.ToJson(save, true);
        File.WriteAllText(slotPath, json);
        
        Debug.Log($"Игра сохранена в слот {slotIndex}: " + slotPath);
    }
    
    /// <summary>
    /// Загрузить игру из определенного слота
    /// </summary>
    public void LoadGameFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
        {
            Debug.LogError($"Некорректный слот: {slotIndex}");
            return;
        }

        string slotPath = Path.Combine(savePath, $"savegame_{slotIndex}.json");

        if (!File.Exists(slotPath))
        {
            Debug.LogWarning($"Файл не найден: {slotPath}");
            return;
        }

        string json = File.ReadAllText(slotPath);

        GameSave save = JsonUtility.FromJson<GameSave>(json);

        GameLoadManager.Instance.LoadGame(save);

        Debug.Log($"Запрошена загрузка слота {slotIndex}");
    }
    
    /// <summary>
    /// Получить информацию о слоте
    /// </summary>
    public SaveSlotInfo GetSlotInfo(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
        {
            return null;
        }
        
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("SavePath не инициализирован!");
            return null;
        }
        
        string slotPath = Path.Combine(savePath, $"savegame_{slotIndex}.json");
        
        SaveSlotInfo info = new SaveSlotInfo
        {
            slotIndex = slotIndex,
            isEmpty = !File.Exists(slotPath)
        };
        
        if (!info.isEmpty)
        {
            try
            {
                string json = File.ReadAllText(slotPath);
                GameSave save = JsonUtility.FromJson<GameSave>(json);
                info.saveDate = save.saveDate;
            }
            catch
            {
                info.isEmpty = true;
                info.saveDate = "Ошибка чтения";
            }
        }
        else
        {
            info.saveDate = "Пусто";
        }
        
        return info;
    }
    
    /// <summary>
    /// Получить информацию обо всех слотах
    /// </summary>
    public List<SaveSlotInfo> GetAllSlots()
    {
        List<SaveSlotInfo> slots = new List<SaveSlotInfo>();
        
        for (int i = 0; i < MAX_SAVE_SLOTS; i++)
        {
            slots.Add(GetSlotInfo(i));
        }
        
        return slots;
    }
    
    /// <summary>
    /// Удалить сохранение из слота
    /// </summary>
    public void DeleteSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SAVE_SLOTS)
        {
            Debug.LogError("Некорректный индекс слота: " + slotIndex);
            return;
        }
        
        if (string.IsNullOrEmpty(savePath))
        {
            Debug.LogError("SavePath не инициализирован!");
            return;
        }
        
        string slotPath = Path.Combine(savePath, $"savegame_{slotIndex}.json");
        
        if (File.Exists(slotPath))
        {
            File.Delete(slotPath);
            Debug.Log($"Сохранение в слоте {slotIndex} удалено");
        }
    }
    public void ApplyLoadedSave(GameSave save)
    {
        gameData.characterAffection =
            save.characterAffection ?? new Dictionary<string, int>();

        if (save.galleryItems != null && save.galleryItems.Count > 0)
        {
            gameData.galleryItems = save.galleryItems;
            gameData.unlockedGalleryItems = new List<string>();
        }
        else
        {
            gameData.galleryItems = new List<GalleryImageData>();

            List<string> legacyUnlocked =
                save.unlockedGalleryItems ?? new List<string>();

            foreach (string itemId in legacyUnlocked)
            {
                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    gameData.galleryItems.Add(
                        new GalleryImageData(itemId, true));
                }
            }

            gameData.unlockedGalleryItems = new List<string>();
        }
        gameData.chatHistories =
        save.chatHistories ??
        new List<ChatHistory>();

        gameData.chatProgress = new Dictionary<string, ChatProgress>();

        if (save.chatProgressList != null)
        {
            foreach (var entry in save.chatProgressList)
            {
                gameData.chatProgress[entry.chatId] = entry.progress;
            }
        }

        
        Debug.Log("Сохранение загружено");
        Debug.Log("Character Affection Count: " + gameData.characterAffection.Count);
        Debug.Log("Gallery Items Count: " + gameData.galleryItems.Count);
        Debug.Log("Unlocked Gallery Items Count: " + gameData.unlockedGalleryItems.Count);
        //Debug.Log("Chat Progress Count: " + gameData.chatProgress.Count);
        Debug.Log("Chat Histories Count: " + gameData.chatHistories.Count);
    }

    private void OnApplicationQuit()
    {
        // Больше не сохраняем автоматически
    }
}
