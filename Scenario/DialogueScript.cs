using System.Collections.Generic;

/// <summary>
/// Одна строка диалога
/// </summary>
[System.Serializable]
public class DialogueLine
{
    public string character;      // Имя персонажа ("ГГ", "Настя", и т.д.)
    public string text;           // Текст реплики
    public float delayBefore;     // Задержка перед показом (сек)
    public string characterColor; // Цвет имени в hex (#FF6B6B)
}

/// <summary>
/// Вариант выбора в диалоге
/// </summary>
[System.Serializable]
public class DialogueChoiceOption
{
    public string choiceText;     // Текст на кнопке
    public string resultChatId;   // В какой чат перейти
}

/// <summary>
/// Полный диалог/сценарий
/// </summary>
[System.Serializable]
public class DialogueScript
{
    public string id;                              // Уникальный ID (например, "nastya_intro")
    public string name;                            // Название (для удобства)
    public string groupChatId;                     // ID группового чата, где происходит диалог
    public List<DialogueLine> lines = new List<DialogueLine>();
    public List<DialogueChoiceOption> choices = new List<DialogueChoiceOption>();
}

/// <summary>
/// Контейнер для JSON (массив диалогов)
/// </summary>
[System.Serializable]
public class DialogueScriptCollection
{
    public List<DialogueScript> scripts = new List<DialogueScript>();
}
