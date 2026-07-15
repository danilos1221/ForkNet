using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TerminalMinigame : MonoBehaviour
{
    [SerializeField] private TerminalUI terminalUI;
    
    private TerminalSequence currentSequence;
    private int currentCommandIndex = 0;
    private bool isWaitingForInput = false;
    private bool isProcessing = false;
    private GameManager gameManager;
    private System.Action onComplete;
    
    private void Start()
    {
        gameManager = GameManager.Instance;
        
        if (terminalUI == null)
        {
            terminalUI = FindAnyObjectByType<TerminalUI>();
            //Debug.Log("[Terminal] TerminalUI найден: " + (terminalUI != null));
        }
        
        if (terminalUI != null)
        {
            terminalUI.OnInputSubmitted += OnPlayerInput;
            //Debug.Log("[Terminal] Подписано на OnInputSubmitted");
        }
        else
        {
            Debug.LogError("[Terminal] TerminalUI не найден!");
        }
    }
    
    private void OnDestroy()
    {
        if (terminalUI != null)
        {
            terminalUI.OnInputSubmitted -= OnPlayerInput;
        }
    }
    
    /// <summary>
    /// Начать терминальную последовательность
    /// </summary>
    public void StartTerminal(string terminalId, System.Action onComplete = null)
    {
        Debug.Log("[Terminal] Начало запуска терминала: " + terminalId);
        
        TerminalDatabase database = TerminalDatabase.LoadFromJSON();
        currentSequence = database.GetTerminal(terminalId);
        
        if (currentSequence == null)
        {
            Debug.LogError("Терминал " + terminalId + " не найден");
            return;
        }
        
        Debug.Log("[Terminal] Загруженный терминал: " + currentSequence.title + " с " + currentSequence.commands.Count + " командами");
        
        this.onComplete = onComplete;
        currentCommandIndex = 0;
        isProcessing = false;
        isWaitingForInput = false;
        
        // Открываем UI терминала
        if (terminalUI != null)
        {
            terminalUI.Show();
            Debug.Log("[Terminal] UI показано");
        }
        else
        {
            Debug.LogError("[Terminal] terminalUI == null!");
            return;
        }
        
        // Начинаем с первой команды
        Debug.Log("[Terminal] Вызываем ShowNextCommand() перед вызовом");
        ShowNextCommand();
        Debug.Log("[Terminal] ShowNextCommand() завершился, currentCommandIndex=" + currentCommandIndex + ", isWaitingForInput=" + isWaitingForInput);
    }
    
    private void ShowNextCommand()
    {
        Debug.Log("[Terminal] ShowNextCommand() вызвана, currentCommandIndex=" + currentCommandIndex + ", всего команд=" + (currentSequence != null ? currentSequence.commands.Count : -1));
        
        if (currentSequence == null)
        {
            Debug.LogError("[Terminal] currentSequence == null!");
            return;
        }
        
        if (currentCommandIndex >= currentSequence.commands.Count)
        {
            // Все команды выполнены
            Debug.Log("[Terminal] Все команды выполнены");
            CompleteTerminal();
            return;
        }
        
        TerminalCommand command = currentSequence.commands[currentCommandIndex];
        
        if (command == null)
        {
            Debug.LogError("[Terminal] command == null!");
            return;
        }
        
        Debug.Log("[Terminal] Показываем команду " + (currentCommandIndex + 1) + "/" + currentSequence.commands.Count + ": " + command.prompt);
        
        // Показываем приглашение
        if (terminalUI != null)
        {
            terminalUI.ShowPrompt(command.prompt);
        }
        else
        {
            Debug.LogError("[Terminal] terminalUI == null в ShowNextCommand!");
        }
        
        isWaitingForInput = true;
        isProcessing = false;
        Debug.Log("[Terminal] isWaitingForInput = true, ожидаем ввода");
    }
    
    private void OnPlayerInput(string input)
    {
        if (!isWaitingForInput || isProcessing)
        {
            Debug.Log("[Terminal] Input игнорирован: isWaitingForInput=" + isWaitingForInput + " isProcessing=" + isProcessing);
            return;
        }
        
        Debug.Log("[Terminal] Обработка ввода: '" + input + "'");
        
        isWaitingForInput = false;
        isProcessing = true;
        
        TerminalCommand command = currentSequence.commands[currentCommandIndex];
        
        //Показываем введённый текст
        terminalUI.AddInputLine(input);
        
        // Проверяем, правильна ли команда
        bool isCorrect = input.Trim() == command.prompt.Trim();
        Debug.Log("[Terminal] Правильность команды: " + isCorrect + " (ожидалось: '" + command.prompt + "', получено: '" + input + "')");
        
        if (isCorrect)
        {
            // Правильная команда
            StartCoroutine(ProcessCorrectCommand(command));
        }
        else
        {
            // Неправильная команда
            StartCoroutine(ProcessWrongCommand(command));
        }
    }
    
    private IEnumerator ProcessCorrectCommand(TerminalCommand command)
    {
        // Показываем "обработку"
        yield return new WaitForSeconds(command.delay);
        
        // Показываем ответ
        terminalUI.AddSystemLine(command.response);
        
        yield return new WaitForSeconds(0.5f);
        
        // Переходим к следующей команде
        currentCommandIndex++;
        ShowNextCommand();
    }
    
    private IEnumerator ProcessWrongCommand(TerminalCommand command)
    {
        // Показываем ошибку
        terminalUI.AddErrorLine("Error: Incorrect command");
        
        yield return new WaitForSeconds(0.5f);
        
        if (command.skipIfWrong)
        {
            // Пропускаем и переходим к следующей
            currentCommandIndex++;
            ShowNextCommand();
        }
        else
        {
            // Показываем приглашение заново
            terminalUI.ShowPrompt(command.prompt);
            isWaitingForInput = true;
            isProcessing = false;
        }
    }
    
    private void CompleteTerminal()
    {
        // Показываем завершающее сообщение
        if (!string.IsNullOrEmpty(currentSequence.onCompleteMessage))
        {
            terminalUI.AddSystemLine(currentSequence.onCompleteMessage);
        }
        
        terminalUI.AddSystemLine("> COMPLETE");
        
        // Обработаем награду
        if (currentSequence.reward != null)
        {
            ProcessReward(currentSequence.reward);
        }
        
        // Закрываем терминал после задержки
        StartCoroutine(CloseTerminalAfterDelay());
    }
    
    private void ProcessReward(TerminalReward reward)
    {
        switch (reward.type)
        {
            case TerminalReward.RewardType.Image:
                gameManager.UnlockGalleryItem(reward.id);
                terminalUI.AddSystemLine($"[UNLOCKED: {reward.id}]");
                break;
            
            case TerminalReward.RewardType.Message:
                terminalUI.AddSystemLine(reward.text);
                break;
            
            case TerminalReward.RewardType.Music:
                // Здесь можно добавить воспроизведение музыки
                terminalUI.AddSystemLine("[AUDIO UNLOCKED]");
                break;
            
            case TerminalReward.RewardType.Unlock:
                terminalUI.AddSystemLine($"[UNLOCKED: {reward.id}]");
                break;
        }
    }
    
    private IEnumerator CloseTerminalAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        
        terminalUI.Hide();
        
        if (onComplete != null)
        {
            onComplete.Invoke();
        }
    }
}
