using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TerminalCommand
{
    public string prompt;           // То, что нужно напечатать
    public string response;         // Ответ системы
    public float delay = 1f;        // Задержка перед ответом
    public bool skipIfWrong = false; // Пропустить ошибку и продолжить
}

[System.Serializable]
public class TerminalReward
{
    public enum RewardType { Image, Message, Music, Unlock }
    
    public RewardType type;
    public string id;               // ID предмета галереи, сообщения и т.д.
    public string text;             // Текст сообщения если нужно
}

[System.Serializable]
public class TerminalSequence
{
    public string id;
    public string title;            // Заголовок "Archive Decryption"
    public string description;      // Описание для NPC
    public List<TerminalCommand> commands = new List<TerminalCommand>();
    public TerminalReward reward;
    public string onCompleteMessage; // Сообщение при завершении
}

[System.Serializable]
public class TerminalDatabase
{
    public List<TerminalSequence> terminals = new List<TerminalSequence>();
    
    private static TerminalDatabase instance;
    
    public static TerminalDatabase LoadFromJSON()
    {
        if (instance != null)
            return instance;
        
        TextAsset jsonFile = Resources.Load<TextAsset>("Terminals/terminals");
        if (jsonFile == null)
        {
            Debug.LogError("Файл terminals.json не найден в Resources/Terminals/");
            return new TerminalDatabase();
        }
        
        string json = jsonFile.text;
        TerminalDatabase database = JsonUtility.FromJson<TerminalDatabase>(json);
        instance = database;
        
        return database;
    }
    
    public TerminalSequence GetTerminal(string id)
    {
        foreach (var terminal in terminals)
        {
            if (terminal.id == id)
                return terminal;
        }
        
        Debug.LogWarning("Терминал с ID " + id + " не найден");
        return null;
    }
}
