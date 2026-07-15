using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Панель повествователя внизу экрана
/// Показывает описание окружения, мысли ГГ и монологи
/// </summary>
public class NarratorPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI narratorText;
    [SerializeField] private CanvasGroup canvasGroup; // для fade in/out
    [SerializeField] private float typingSpeed = 0.05f; // Скорость печати текста
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    
    private void Start()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        // Начинаем с прозрачным текстом
        canvasGroup.alpha = 0f;
    }
    
    /// <summary>
    /// Показать текст нарратора с эффектом печати
    /// </summary>
    public void ShowNarration(string text)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        typingCoroutine = StartCoroutine(ShowNarrationRoutine(text));
    }
    
    /// <summary>
    /// Показать текст нарратора мгновенно (без эффекта печати)
    /// </summary>
    public void ShowNarrationInstant(string text)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        narratorText.text = text;
        isTyping = false;
        
        if (canvasGroup.alpha < 1f)
            StartCoroutine(FadeIn());
    }
    
    /// <summary>
    /// Скрыть панель нарратора
    /// </summary>
    public void HideNarration()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        
        StartCoroutine(FadeOut());
    }
    
    /// <summary>
    /// Пропустить эффект печати и показать весь текст
    /// </summary>
    public void SkipTyping()
    {
        if (isTyping && typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            narratorText.text = narratorText.text; // Показываем весь текст
            isTyping = false;
        }
    }
    
    private IEnumerator ShowNarrationRoutine(string text)
    {
        // Сначала фейдим в
        if (canvasGroup.alpha < 1f)
            yield return StartCoroutine(FadeIn());
        
        // Печатаем текст
        narratorText.text = "";
        isTyping = true;
        int displayedCharacters = 0;
        
        while (displayedCharacters < text.Length)
        {
            narratorText.text = text.Substring(0, ++displayedCharacters);
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
    }
    
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        narratorText.text = "";
    }
    
    public bool IsTyping => isTyping;
    
    public string CurrentText => narratorText.text;
}
