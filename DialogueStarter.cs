using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Запускает диалоги с помощью новой системы JSON
/// </summary>
public class DialogueStarter : MonoBehaviour
{
    public ScenarioManager scenarioManager;
    
    private void Start()
    {
        if (scenarioManager == null)
        {
            scenarioManager = FindAnyObjectByType<ScenarioManager>();
        }
    }
    
    /// <summary>
    /// Стартовая демонстрация диалога Насти
    /// </summary>
    public void StartNastyaDialogue()
    {
        if (scenarioManager != null)
        {
            scenarioManager.PlayDialogue("nastya_intro");
        }
        else
        {
            Debug.LogError("ScenarioManager не найден!");
        }
        
        GetComponent<Button>().interactable = false;
    }
    
    /// <summary>
    /// Запустить любой диалог по ID
    /// </summary>
    public void StartDialogue(string dialogueId)
    {
        if (scenarioManager != null)
        {
            scenarioManager.PlayDialogue(dialogueId);
        }
        else
        {
            Debug.LogError("ScenarioManager не найден!");
        }
    }
}