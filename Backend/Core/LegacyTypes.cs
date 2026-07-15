/// <summary>
/// Легаси-классы, сохранённые для обратной совместимости файлов сохранения.
/// Не использовать в новом коде — используйте JSON-систему через DialogueScriptDatabase.
/// </summary>

[System.Serializable]
public class Character
{
    public string id;
    public string name;
    public string color;

    public Character() { }
    public Character(string id, string name, string color)
    {
        this.id    = id;
        this.name  = name;
        this.color = color;
    }
}

[System.Serializable]
[System.Obsolete("Используйте новую систему JSON диалогов через DialogueScriptDatabase")]
public class Dialogue
{
    public string id;
    public string title;
}
