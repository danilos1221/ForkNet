using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatItem : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI previewText;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject unreadIndicator;

    private string chatId;
    private System.Action<string> onSelectCallback;

    private void Start()
    {
        selectButton.onClick.AddListener(OnSelect);
        HideUnreadIndicator();
    }

    public void SetupChat(string id, string chatName, Sprite avatar, System.Action<string> onSelect)
    {
        chatId = id;
        nameText.text = chatName;
        previewText.text = "";

        if (avatar != null)
            avatarImage.sprite = avatar;

        onSelectCallback = onSelect;
    }

    public void ShowUnreadIndicator()
    {
        if (unreadIndicator != null)
            unreadIndicator.SetActive(true);
    }

    public void HideUnreadIndicator()
    {
        if (unreadIndicator != null)
            unreadIndicator.SetActive(false);
    }

    private void OnSelect()
    {
        onSelectCallback?.Invoke(chatId);
    }
    public void UpdatePreview(string lastMessage)
    {
        previewText.text = lastMessage;
    }
}