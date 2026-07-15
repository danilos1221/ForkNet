using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class TerminalUI : MonoBehaviour
{
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private GameObject messagePrefab; // Префаб для сообщений
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [SerializeField] private Color systemColor = Color.green;
    [SerializeField] private Color inputColor = Color.white;
    [SerializeField] private Color errorColor = Color.red;
    [SerializeField] private Color promptColor = Color.cyan;
    
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private int maxMessages = 50; // Максимум сообщений на экране
    
    public event Action<string> OnInputSubmitted;
    
    private string currentPrompt = "> ";
    private int messageCount = 0;
    
    private void Start()
    {
        if (terminalPanel != null)
            terminalPanel.SetActive(false);
        
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputFieldSubmit);
        }
        
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }
    
    public void Show()
    {
        if (terminalPanel != null)
            terminalPanel.SetActive(true);
        
        if (canvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }
        
        
        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
            Debug.Log("[Terminal] InputField активирован, фокус установлен");
        }
        else
        {
            Debug.LogError("[Terminal] InputField не найден!");
        }
        
        ClearOutput();
        AddSystemLine("=== TERMINAL V1.0 ===");
        AddSystemLine("Type commands to decrypt archive...\n");
        Debug.Log("[Terminal] Терминал открыт");
    }
    
    public void Hide()
    {
        
        if (inputField != null)
            inputField.DeactivateInputField();
        
        if (canvasGroup != null)
        {
            StartCoroutine(FadeOut());
        }
    }
    
    public void ShowPrompt(string prompt)
    {
        currentPrompt = "> " + prompt;
        
        if (promptText != null)
        {
            promptText.text = currentPrompt;
            promptText.color = promptColor;
        }
        
        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField();
        }
    }
    
    public void AddInputLine(string text)
    {
        AddMessageLine($">> {text}", inputColor);
    }
    
    public void AddSystemLine(string text)
    {
        AddMessageLine(text, systemColor);
    }
    
    public void AddErrorLine(string text)
    {
        AddMessageLine(text, errorColor);
    }
    
    private void AddMessageLine(string text, Color color)
    {
        if (scrollView == null || scrollView.content == null)
        {
            Debug.LogError("[Terminal] ScrollView или Content не найдены!");
            return;
        }
        
        if (messagePrefab == null)
        {
            Debug.LogError("[Terminal] Message prefab не назначен!");
            return;
        }
        
        // Создаём объект сообщения из префаба
        GameObject messageGO = Instantiate(messagePrefab, scrollView.content);
        messageGO.name = "Message_" + messageCount;
        
        // Получаем TextMeshPro из префаба
        TextMeshProUGUI messageTMP = messageGO.GetComponentInChildren<TextMeshProUGUI>();
        if (messageTMP != null)
        {
            messageTMP.text = text;
            messageTMP.color = color;
        }
        
        messageCount++;
        
        // Удаляем старые сообщения если их слишком много
        if (scrollView.content.childCount > maxMessages)
        {
            Destroy(scrollView.content.GetChild(0).gameObject);
        }
        
        // Прокручиваем вниз
        Canvas.ForceUpdateCanvases();
        scrollView.verticalNormalizedPosition = 0f;
    }
    
    private void ClearOutput()
    {
        if (scrollView == null || scrollView.content == null)
            return;
        
        // Удаляем все дочерние сообщения
        foreach (Transform child in scrollView.content)
        {
            Destroy(child.gameObject);
        }
        
        messageCount = 0;
    }
    
    private void OnInputFieldSubmit(string input)
    {
        if (string.IsNullOrEmpty(input.Trim()))
            return;
        
        Debug.Log("[Terminal] Input submitted: " + input);
        OnInputSubmitted?.Invoke(input);
        
        if (inputField != null)
        {
            inputField.text = "";
            inputField.ActivateInputField(); // Возвращаем фокус
        }
    }
    
    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        
        if (terminalPanel != null)
            terminalPanel.SetActive(false);
    }
}
