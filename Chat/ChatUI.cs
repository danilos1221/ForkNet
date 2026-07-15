using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Отвечает ТОЛЬКО за визуальное отображение чата
/// </summary>
public class ChatUI : MonoBehaviour
{
    [Header("Chat List")]
    [SerializeField] private Transform chatListContainer;
    [SerializeField] private GameObject chatItemPrefab;
    
    [Header("Message Display")]
    [SerializeField] private Transform messageContainer;
    [SerializeField] private GameObject messagePrefab;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private ScrollRect scrollView;
    
    [Header("Input Area")]
    [SerializeField] private TextMeshProUGUI inputPromptText;
    [SerializeField] private Button submitButton;
    
    [Header("Chat Header")]
    [SerializeField] private Image chatHeaderAvatar;
    [SerializeField] private TextMeshProUGUI chatHeaderName;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("Visual Elements")]
    [SerializeField] private GameObject typingStatusPrefab;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonsContainer;
    
    [Header("Audio")]
    [SerializeField] private string messageSoundName = "message_appear";
    [SerializeField] private float messageSoundVolume = 0.7f;
    
    // Состояние UI
    private List<GameObject> activeMessages = new List<GameObject>();
    private GameObject activeTypingIndicator;
    private List<GameObject> activeChoiceButtons = new List<GameObject>();
    private string currentChatId;
    
    // События
    public System.Action OnSubmitPressed;
    public System.Action<string> OnChatSelected;
    
    private void Start()
    {
        if (submitButton != null)
            submitButton.onClick.AddListener(() => OnSubmitPressed?.Invoke());
    }
    
    public void CreateChatItem(string chatId, string name, Sprite avatar)
    {
        GameObject itemGO = Instantiate(chatItemPrefab, chatListContainer);
        ChatItem chatItem = itemGO.GetComponent<ChatItem>();
        
        if (chatItem != null)
        {
            chatItem.SetupChat(chatId, name, avatar, (id) => 
            {
                currentChatId = id;
                OnChatSelected?.Invoke(id);
            });
        }
    }
    
    public void SetActiveChat(string chatId, string name, Sprite avatar)
    {
        currentChatId = chatId;
        chatHeaderName.text = name;
        if (avatar != null) chatHeaderAvatar.sprite = avatar;
        ClearMessages();
    }
    
    public void AddTextMessage(string text, bool isPlayer, string senderName = "")
    {
        GameObject msgGO = Instantiate(messagePrefab, messageContainer);
        MessageUI messageUI = msgGO.GetComponent<MessageUI>();
        
        if (messageUI != null)
        {
            if (isPlayer)
                messageUI.SetPlayerMessage(text);
            else if (string.IsNullOrEmpty(senderName))
                messageUI.SetOtherMessage(text);
            else
                messageUI.SetGroupOtherMessage(senderName, text);
        }
        
        activeMessages.Add(msgGO);
        PlaySound();
        ScrollToBottom();
    }
    
    public void AddImageMessage(Sprite image, bool isPlayer, string imageId = "", string senderName = "")
    {
        if (image == null) return;
        
        GameObject imgGO = Instantiate(imagePrefab, messageContainer);
        MessageUI messageUI = imgGO.GetComponent<MessageUI>();
        
        if (messageUI != null)
        {
            if (isPlayer)
                messageUI.SetPlayerImage(image, imageId);
            else if (string.IsNullOrEmpty(senderName))
                messageUI.SetOtherImage(image, imageId);
            else
                messageUI.SetGroupOtherImage(senderName, image, imageId);
        }
        
        activeMessages.Add(imgGO);
        PlaySound();
        ScrollToBottom();
    }
    
    public void ShowTypingIndicator(string text = "пишет")
    {
        if (activeTypingIndicator == null)
            activeTypingIndicator = Instantiate(typingStatusPrefab, messageContainer);
        
        var tmp = activeTypingIndicator.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
        
        ScrollToBottom();
    }
    
    public void UpdateTypingIndicator(string text)
    {
        if (activeTypingIndicator != null)
        {
            var tmp = activeTypingIndicator.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
    }
    
    public void HideTypingIndicator()
    {
        if (activeTypingIndicator != null)
        {
            Destroy(activeTypingIndicator);
            activeTypingIndicator = null;
        }
    }
    
    public void ShowInputPrompt()
    {
        if (inputPromptText != null)
            inputPromptText.gameObject.SetActive(true);
    }
    
    public void HideInputPrompt()
    {
        if (inputPromptText != null)
            inputPromptText.gameObject.SetActive(false);
    }
    
    public void SetStatusText(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
    
    public IEnumerator ShowChoiceButtons(List<ChatChoice> choices, System.Action<int> onSelected)
    {
        ClearChoiceButtons();
        
        for (int i = 0; i < choices.Count; i++)
        {
            int index = i;
            GameObject btnGO = Instantiate(choiceButtonPrefab, choiceButtonsContainer);
            
            TextMeshProUGUI tmp = btnGO.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = choices[i].text;
            
            Button btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    onSelected?.Invoke(index);
                    ClearChoiceButtons();
                });
            }
            
            activeChoiceButtons.Add(btnGO);
        }
        
        yield return new WaitUntil(() => activeChoiceButtons.Count == 0);
    }
    
    public void ClearMessages()
    {
        foreach (GameObject msg in activeMessages)
            Destroy(msg);
        activeMessages.Clear();
        HideTypingIndicator();
    }
    
    private void ClearChoiceButtons()
    {
        foreach (GameObject btn in activeChoiceButtons)
            Destroy(btn);
        activeChoiceButtons.Clear();
    }
    
    private void PlaySound()
    {
        if (SFXManager.Instance != null && !string.IsNullOrEmpty(messageSoundName))
            SFXManager.Instance.PlaySound(messageSoundName, messageSoundVolume);
    }
    
    private void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        scrollView.verticalNormalizedPosition = 0f;
    }
}