using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Загружает и управляет диалогами из JSON файла
/// </summary>
public class DialogueScriptDatabase
{
    private static DialogueScriptCollection loadedScripts;

    public static DialogueScript GetDialogue(string id)
    {
        LoadScriptsIfNeeded();
        
        foreach (var script in loadedScripts.scripts)
        {
            if (script.id == id)
                return script;
        }
        
        Debug.LogWarning($"Dialogue '{id}' not found!");
        return null;
    }

    public static DialogueScript GetRandomDialogue()
    {
        LoadScriptsIfNeeded();
        if (loadedScripts.scripts.Count == 0)
            return null;
        
        return loadedScripts.scripts[Random.Range(0, loadedScripts.scripts.Count)];
    }

    public static List<DialogueScript> GetAllDialogues()
    {
        LoadScriptsIfNeeded();
        return loadedScripts.scripts;
    }

    private static void LoadScriptsIfNeeded()
    {
        if (loadedScripts != null)
            return;

        // Загружаем JSON из Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("Dialogues/dialogues");
        
        if (jsonFile == null)
        {
            Debug.LogError("Dialogues JSON file not found at 'Resources/Dialogues/dialogues.json'!");
            loadedScripts = new DialogueScriptCollection();
            return;
        }

        try
        {
            loadedScripts = JsonUtility.FromJson<DialogueScriptCollection>(jsonFile.text);
            Debug.Log($"Loaded {loadedScripts.scripts.Count} dialogues");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing dialogues JSON: {e.Message}");
            loadedScripts = new DialogueScriptCollection();
        }
    }

    public static void ReloadScripts()
    {
        loadedScripts = null;
        LoadScriptsIfNeeded();
    }
}
