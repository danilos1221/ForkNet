using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Отвечает ТОЛЬКО за визуальное отображение чата:
/// рендеринг сообщений, анимации кнопки, индикаторы.
/// Не знает ничего о GameData, ScenarioManager, ChatDatabase.
/// Используется ChatController как View-слой.
/// </summary>
public class ChatView : MonoBehaviour
{
    [Header("Шапка открытого чата")]
    [SerializeField] private Image chatHeaderAvatar;
    [SerializeField] private TextMeshProUGUI chatHeaderName;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Область сообщений")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private string imageBasePath = "Images/Gallery";

    [Header("Индикатор печатания")]
    [SerializeField] private GameObject typingStatusPrefab;

    [Header("Ввод игрока")]
    [SerializeField] private TextMeshProUGUI inputPromptText;
    [SerializeField] private Button submitMessageButton;

    [Header("Кнопки выбора")]
    [Tooltip("Панель с заранее созданными кнопками-вариантами. Кнопки не создаются и не удаляются — только показываются/скрываются панель и меняется текст.")]
    [SerializeField] private GameObject choicePanel;
    [Tooltip("Кнопки-варианты в порядке слева вверху — вниз, сколько вариантов поддерживается одновременно")]
    [SerializeField] private List<Button> choiceButtons = new();

    [Header("Настройки сообщений")]
    [SerializeField] private string messageAppearSoundName = "message_appear";
    [SerializeField] private float messageSoundVolume = 0.7f;
    [SerializeField] private int maxMessageHistorySize = 100;

    [Header("Анимация кнопки")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseScale = 1.1f;

    // ──────────────────────────────────────────────
    // События
    // ──────────────────────────────────────────────

    /// <summary>Вызывается когда игрок нажимает кнопку «Отправить».</summary>
    public System.Action OnSubmitPressed;

    // ──────────────────────────────────────────────
    // Приватное состояние
    // ──────────────────────────────────────────────

    private readonly List<GameObject> displayedMessages = new();
    private GameObject activeTypingStatusObject;
    private Coroutine pulseCoroutine;
    private Vector3 originalButtonScale;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        if (submitMessageButton != null)
            submitMessageButton.onClick.AddListener(() => OnSubmitPressed?.Invoke());
    }

    // ──────────────────────────────────────────────
    // Заголовок чата
    // ──────────────────────────────────────────────

    public void SetChatHeader(string name, Sprite avatar)
    {
        if (chatHeaderName != null) chatHeaderName.text = name;
        if (chatHeaderAvatar != null && avatar != null) chatHeaderAvatar.sprite = avatar;
    }

    public void ClearChatHeader()
    {
        if (chatHeaderName != null) chatHeaderName.text = string.Empty;
        if (chatHeaderAvatar != null) chatHeaderAvatar.sprite = null;
        if (statusText != null) statusText.text = string.Empty;
    }

    public void SetStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    // ──────────────────────────────────────────────
    // Рендеринг сообщений
    // ──────────────────────────────────────────────

    public void AddMessage(string text, bool isPlayer, string senderName, ChatType chatType)
    {
        Debug.Log($"[ChatView] AddMessage: '{text}' (isPlayer={isPlayer})");
        GameObject msgGO = Instantiate(messagePrefab, messageContainer);
        if (!msgGO.TryGetComponent<MessageUI>(out var messageUI)) return;

        if (chatType == ChatType.Private)
        {
            if (isPlayer) messageUI.SetPlayerMessage(text);
            else          messageUI.SetOtherMessage(text);
        }
        else
        {
            if (isPlayer) messageUI.SetGroupPlayerMessage(text);
            else          messageUI.SetGroupOtherMessage(senderName, text);
        }

        RegisterDisplayedMessage(msgGO);
    }

    public void AddImage(string imageId, bool isPlayer, string senderName, ChatType chatType)
    {
        Sprite sprite = Resources.Load<Sprite>($"{imageBasePath}/{imageId}");
        if (sprite == null)
        {
            Debug.LogWarning($"[ChatView] Изображение не найдено: {imageBasePath}/{imageId}");
            return;
        }
        if (imagePrefab == null)
        {
            Debug.LogWarning("[ChatView] imagePrefab не назначен!");
            return;
        }

        GameObject imageGO = Instantiate(imagePrefab, messageContainer);
        if (!imageGO.TryGetComponent<MessageUI>(out var messageUI)) return;

        if (isPlayer)
            messageUI.SetPlayerImage(sprite, imageId);
        else if (chatType == ChatType.Private)
            messageUI.SetOtherImage(sprite, imageId);
        else
            messageUI.SetGroupOtherImage(senderName, sprite, imageId);

        RegisterDisplayedMessage(imageGO);
    }

    public void ClearMessages()
    {
        foreach (var msg in displayedMessages)
        {
            if (msg != null) Destroy(msg);
        }
        displayedMessages.Clear();
        HideTypingIndicator();
    }

    // ──────────────────────────────────────────────
    // Индикатор печатания
    // ──────────────────────────────────────────────

    public void ShowTypingIndicator()
    {
        if (typingStatusPrefab == null) return;
        if (activeTypingStatusObject != null) return;

        activeTypingStatusObject = Instantiate(typingStatusPrefab, messageContainer);
        ScrollToBottom();
    }

    public void UpdateTypingIndicatorText(string text)
    {
        if (activeTypingStatusObject == null) return;
        var tmp = activeTypingStatusObject.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    public void HideTypingIndicator()
    {
        if (activeTypingStatusObject == null) return;
        Destroy(activeTypingStatusObject);
        activeTypingStatusObject = null;
    }

    // ──────────────────────────────────────────────
    // Подсказка ввода игрока
    // ──────────────────────────────────────────────

    public void ShowInputPrompt(string promptText = null)
    {
        if (inputPromptText == null) return;
        inputPromptText.text = promptText ?? "[Send message:]";
        inputPromptText.gameObject.SetActive(true);
        StartButtonPulse();
    }

    public void HideInputPrompt()
    {
        if (inputPromptText != null)
            inputPromptText.gameObject.SetActive(false);
        StopButtonPulse();
    }

    // ──────────────────────────────────────────────
    // Кнопки выбора
    // ──────────────────────────────────────────────

    public void SpawnChoiceButtons(List<ChatChoice> choices, System.Action<int> onSelected)
    {
        if (choicePanel == null || choiceButtons.Count == 0)
        {
            Debug.LogError("[ChatView] choicePanel или choiceButtons не заданы!");
            return;
        }

        if (choices.Count > choiceButtons.Count)
            Debug.LogWarning($"[ChatView] вариантов выбора ({choices.Count}) больше, чем готовых кнопок ({choiceButtons.Count}). Лишние варианты показаны не будут.");

        choicePanel.SetActive(true);

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            Button btn = choiceButtons[i];
            if (btn == null) continue;

            btn.onClick.RemoveAllListeners();

            if (i < choices.Count)
            {
                int capturedIndex = i;

                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = choices[i].text;
                else
                {
                    var txt = btn.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = choices[i].text;
                }

                btn.onClick.AddListener(() => onSelected?.Invoke(capturedIndex));
                btn.gameObject.SetActive(true);
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    public void ClearChoiceButtons()
    {
        foreach (var btn in choiceButtons)
        {
            if (btn == null) continue;
            btn.onClick.RemoveAllListeners();
        }

        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    // ──────────────────────────────────────────────
    // Приватные вспомогательные методы
    // ──────────────────────────────────────────────

    private void RegisterDisplayedMessage(GameObject msgGO)
    {
        displayedMessages.Add(msgGO);

        if (displayedMessages.Count > maxMessageHistorySize)
            RemoveOldestDisplayedMessage();

        PlayMessageSound();
        ScrollToBottom();
    }

    private void RemoveOldestDisplayedMessage()
    {
        if (displayedMessages.Count == 0) return;
        Destroy(displayedMessages[0]);
        displayedMessages.RemoveAt(0);
    }

    private void PlayMessageSound()
    {
        if (SFXManager.Instance != null && !string.IsNullOrEmpty(messageAppearSoundName))
            SFXManager.Instance.PlaySound(messageAppearSoundName, messageSoundVolume);
    }

    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (scrollView != null)
            scrollView.verticalNormalizedPosition = 0f;
    }

    // ──────────────────────────────────────────────
    // Анимация кнопки «Отправить»
    // ──────────────────────────────────────────────

    private void StartButtonPulse()
    {
        if (submitMessageButton == null) return;
        if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
        originalButtonScale = submitMessageButton.transform.localScale;
        pulseCoroutine = StartCoroutine(PulseRoutine());
    }

    private void StopButtonPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        if (submitMessageButton != null && originalButtonScale != Vector3.zero)
            submitMessageButton.transform.localScale = originalButtonScale;
    }

    private IEnumerator PulseRoutine()
    {
        Transform t = submitMessageButton.transform;
        while (true)
        {
            yield return ScaleTo(t, originalButtonScale * pulseScale, 0.5f);
            yield return ScaleTo(t, originalButtonScale, 0.5f);
        }
    }

    private IEnumerator ScaleTo(Transform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
            yield return null;
        }
        target.localScale = targetScale;
    }
}
