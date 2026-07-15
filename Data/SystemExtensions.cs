using UnityEngine;
using System.Collections.Generic;

// ============================================
// ПРИМЕРЫ РАСШИРЕНИЙ И МОДИФИКАЦИЙ
// ============================================

// 1️⃣ РАСШИРЕННАЯ СИСТЕМА ПЕРСОНАЖЕЙ С ЭМОЦИЯМИ
// ============================================

public enum Emotion
{
    Happy,
    Sad,
    Angry,
    Surprised,
    Thinking,
    Neutral
}

[System.Serializable]
public class CharacterAdvanced : Character
{
    public Emotion currentEmotion = Emotion.Neutral;
    public int reputation = 0;
    public bool isActive = false;
    
    public CharacterAdvanced(string id, string name, string color) 
        : base(id, name, color) { }
    
    public Sprite GetEmotionSprite()
    {
        return Resources.Load<Sprite>($"Characters/{id}_{currentEmotion}");
    }
    
    public void ChangeReputation(int change)
    {
        reputation += change;
    }
}

// 2️⃣ СИСТЕМА УВЕДОМЛЕНИЙ ВРОДЕ РЕАЛЬНОГО МЕССЕНДЖЕРА
// ============================================

public class ChatNotification
{
    public string characterId;
    public string message;
    public System.DateTime timestamp;
    public bool isRead = false;
}

public class NotificationSystem : MonoBehaviour
{
    private List<ChatNotification> notifications = new List<ChatNotification>();
    
    public void AddNotification(string characterId, string message)
    {
        notifications.Add(new ChatNotification
        {
            characterId = characterId,
            message = message,
            timestamp = System.DateTime.Now
        });
        
        Debug.Log($"📬 Новое сообщение от {characterId}: {message}");
    }
    
    public List<ChatNotification> GetUnreadNotifications()
    {
        return notifications.FindAll(n => !n.isRead);
    }
    
    public int GetUnreadCount()
    {
        return GetUnreadNotifications().Count;
    }
    
    public void MarkAsRead(ChatNotification notification)
    {
        notification.isRead = true;
    }
}

// 3️⃣ РАСШИРЕННАЯ СИСТЕМА ДИАЛОГОВ С УСЛОВИЯМИ
// ============================================

public class DialogueCondition
{
    public string characterId;
    public int requiredAffection;
    public bool mustHaveItem;
    
    public bool IsMet(GameData gameData)
    {
        int affection = gameData.GetAffection(characterId);
        return affection >= requiredAffection;
    }
}

[System.Serializable]
public class AdvancedDialogue : Dialogue
{
    public DialogueCondition[] conditions;
    
    public AdvancedDialogue(string id, string title) : base(id, title) { }
    
    public bool CanAccess(GameData gameData)
    {
        if (conditions == null || conditions.Length == 0)
            return true;
            
        foreach (var condition in conditions)
        {
            if (!condition.IsMet(gameData))
                return false;
        }
        
        return true;
    }
}

// 4️⃣ СИСТЕМА ПОБОЧНЫХ КВЕСТОВ
// ============================================

public enum QuestStatus
{
    Available,
    InProgress,
    Completed,
    Failed
}

[System.Serializable]
public class Quest
{
    public string id;
    public string title;
    public string description;
    public QuestStatus status = QuestStatus.Available;
    public string[] relatedCharacters;
    public int rewardAffection;
}

public class QuestManager : MonoBehaviour
{
    private Dictionary<string, Quest> quests = new Dictionary<string, Quest>();
    
    public void AddQuest(Quest quest)
    {
        quests[quest.id] = quest;
    }
    
    public void StartQuest(string questId)
    {
        if (quests.ContainsKey(questId))
        {
            quests[questId].status = QuestStatus.InProgress;
        }
    }
    
    public void CompleteQuest(string questId, GameData gameData)
    {
        if (quests.ContainsKey(questId))
        {
            Quest quest = quests[questId];
            quest.status = QuestStatus.Completed;
            
            // Даем награду расположения
            foreach (string charId in quest.relatedCharacters)
            {
                gameData.ChangeAffection(charId, quest.rewardAffection);
            }
        }
    }
}

// 5️⃣ СИСТЕМА ДОСТИЖЕНИЙ
// ============================================

[System.Serializable]
public class Achievement
{
    public string id;
    public string title;
    public string description;
    public bool unlocked = false;
    public System.DateTime unlockedTime;
}

public class AchievementManager : MonoBehaviour
{
    private Dictionary<string, Achievement> achievements = new Dictionary<string, Achievement>();
    
    public void RegisterAchievement(string id, string title, string description)
    {
        achievements[id] = new Achievement 
        { 
            id = id, 
            title = title, 
            description = description 
        };
    }
    
    public void UnlockAchievement(string achievementId)
    {
        if (achievements.ContainsKey(achievementId) && !achievements[achievementId].unlocked)
        {
            achievements[achievementId].unlocked = true;
            achievements[achievementId].unlockedTime = System.DateTime.Now;
            Debug.Log($"🏆 Достижение разблокировано: {achievements[achievementId].title}");
        }
    }
    
    public List<Achievement> GetUnlockedAchievements()
    {
        List<Achievement> unlocked = new List<Achievement>();
        foreach (var ach in achievements.Values)
        {
            if (ach.unlocked)
                unlocked.Add(ach);
        }
        return unlocked;
    }
}

// 6️⃣ СИСТЕМА ТАЙМЕРА ДЛЯ ОТВЕТОВ
// ============================================
// Примечание: Используйте новую систему JSON диалогов (DialogueScript)
// Параметры таймера можно добавить в JSON при необходимости

public class DialogueTimer : MonoBehaviour
{
    private float timeLimit = 10f;
    private float elapsedTime = 0f;
    private bool isActive = false;
    
    public System.Action OnTimerExpired;
    
    public void StartTimer(float duration)
    {
        timeLimit = duration;
        elapsedTime = 0f;
        isActive = true;
    }
    
    private void Update()
    {
        if (isActive)
        {
            elapsedTime += Time.deltaTime;
            
            if (elapsedTime >= timeLimit)
            {
                isActive = false;
                OnTimerExpired?.Invoke();
            }
        }
    }
    
    public float GetRemainingTime()
    {
        return Mathf.Max(0, timeLimit - elapsedTime);
    }
    
    public float GetProgress()
    {
        return elapsedTime / timeLimit;
    }
}

// 7️⃣ СИСТЕМА ЭФФЕКТОВ ТЕКСТА (ТИППИНГ)
// ============================================

public class TextTypingEffect : MonoBehaviour
{
    private string fullText;
    private float typingSpeed = 0.05f;
    private float elapsedTime = 0f;
    private int displayedCharacters = 0;
    private TMPro.TextMeshProUGUI textComponent;
    
    public void StartTyping(string text, float speed = 0.05f)
    {
        fullText = text;
        typingSpeed = speed;
        elapsedTime = 0f;
        displayedCharacters = 0;
        textComponent.text = "";
    }
    
    private void Update()
    {
        if (displayedCharacters < fullText.Length)
        {
            elapsedTime += Time.deltaTime;
            
            while (elapsedTime >= typingSpeed && displayedCharacters < fullText.Length)
            {
                textComponent.text = fullText.Substring(0, ++displayedCharacters);
                elapsedTime -= typingSpeed;
            }
        }
    }
    
    public void SkipToEnd()
    {
        textComponent.text = fullText;
        displayedCharacters = fullText.Length;
    }
    
    public bool IsComplete()
    {
        return displayedCharacters >= fullText.Length;
    }
}

// 8️⃣ СИСТЕМА ПЕРЕВОДОВ (ЛОКАЛИЗАЦИЯ)
// ============================================

[System.Serializable]
public class LocalizationData
{
    public Dictionary<string, Dictionary<string, string>> translations 
        = new Dictionary<string, Dictionary<string, string>>();
}

public class LocalizationManager : MonoBehaviour
{
    private LocalizationData localizationData;
    private string currentLanguage = "ru";
    
    public void LoadLocalization(string jsonPath)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);
        localizationData = JsonUtility.FromJson<LocalizationData>(jsonFile.text);
    }
    
    public string GetText(string key)
    {
        if (localizationData?.translations.ContainsKey(currentLanguage) ?? false)
        {
            var langDict = localizationData.translations[currentLanguage];
            if (langDict.ContainsKey(key))
                return langDict[key];
        }
        
        return key; // Возвращаем ключ если перевод не найден
    }
    
    public void SetLanguage(string language)
    {
        if (localizationData?.translations.ContainsKey(language) ?? false)
        {
            currentLanguage = language;
        }
    }
    
    public List<string> GetAvailableLanguages()
    {
        if (localizationData?.translations != null)
        {
            return new List<string>(localizationData.translations.Keys);
        }
        return new List<string>();
    }
}

// 9️⃣ СИСТЕМА ЗВУКОВ
// ============================================

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    
    private Dictionary<string, AudioClip> soundCache = new Dictionary<string, AudioClip>();
    
    public void PlaySFX(string soundName, float volume = 1f)
    {
        if (!soundCache.ContainsKey(soundName))
        {
            AudioClip clip = Resources.Load<AudioClip>($"Sounds/{soundName}");
            soundCache[soundName] = clip;
        }
        
        if (soundCache[soundName] != null)
        {
            sfxSource.PlayOneShot(soundCache[soundName], volume);
        }
    }
    
    public void PlayMusic(string musicName, float fadeTime = 0f)
    {
        AudioClip clip = Resources.Load<AudioClip>($"Music/{musicName}");
        if (clip != null)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }
    
    public void StopMusic(float fadeTime = 1f)
    {
        StartCoroutine(FadeOut(fadeTime));
    }
    
    private System.Collections.IEnumerator FadeOut(float duration)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.volume = startVolume;
    }
}

// 🔟 СИСТЕМА СТАТИСТИКИ
// ============================================

[System.Serializable]
public class GameStats
{
    public int totalDialoguesRead = 0;
    public int totalChoicesMade = 0;
    public int totalTimePlayed = 0; // в секундах
    public Dictionary<string, int> personalityStats = new Dictionary<string, int>();
}

public class StatsManager : MonoBehaviour
{
    private GameStats stats = new GameStats();
    private float sessionStartTime;
    
    private void Start()
    {
        sessionStartTime = Time.time;
    }
    
    public void RecordDialogueRead()
    {
        stats.totalDialoguesRead++;
    }
    
    public void RecordChoice(string choiceType)
    {
        stats.totalChoicesMade++;
        if (!stats.personalityStats.ContainsKey(choiceType))
            stats.personalityStats[choiceType] = 0;
        stats.personalityStats[choiceType]++;
    }
    
    public int GetSessionTime()
    {
        return (int)(Time.time - sessionStartTime);
    }
    
    public GameStats GetStats()
    {
        stats.totalTimePlayed = GetSessionTime();
        return stats;
    }
}

// Используй эти системы для расширения базовой функциональности!
