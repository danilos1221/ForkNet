using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Панель переименования персонажей/чатов.
/// Имя применяется как display name чата (вместо @name),
/// оригинальное имя из сценария при этом не теряется.
/// </summary>
public class CharacterRenamePanelController : MonoBehaviour
{
    [System.Serializable]
    public class CharacterRenameTarget
    {
        [Tooltip("chat id или thread key (например: private_nastya или nastya)")]
        public string chatIdOrThreadKey;
        [Tooltip("Текст имени персонажа на главной панели")]
        public TMP_Text nameLabel;
    }

    [Header("Main Panel")]
    [SerializeField] private List<CharacterRenameTarget> renameTargets = new();

    [Header("Edit Panel (Sibling)")]
    [SerializeField] private GameObject editPanel;
    [SerializeField] private TMP_InputField editNameInputField;
    [SerializeField] private TMP_Text selectedCharacterLabel;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button cancelButton;

    [Header("Optional")]
    [SerializeField] private PhoneChatController phoneChatController;
    [SerializeField] private MenuManager menuManager;

    private int activeTargetIndex = -1;

    private void Awake()
    {
        if (phoneChatController == null)
            phoneChatController = FindAnyObjectByType<PhoneChatController>(FindObjectsInactive.Include);

        if (menuManager == null)
            menuManager = FindAnyObjectByType<MenuManager>(FindObjectsInactive.Include);

        BindUiButtons();
    }

    private void OnDestroy()
    {
        UnbindUiButtons();
    }

    private void OnEnable()
    {
        ForceCloseEditPanelWithoutHistory();
        RefreshAllFields();
    }

    public void RefreshAllFields()
    {
        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;

        for (int i = 0; i < renameTargets.Count; i++)
        {
            CharacterRenameTarget target = renameTargets[i];
            if (target == null || string.IsNullOrWhiteSpace(target.chatIdOrThreadKey))
                continue;

            if (!TryResolveChatKeyAndOriginalName(target.chatIdOrThreadKey, out string chatKey, out string originalName))
                continue;

            string displayName = GameData.ResolveChatDisplayName(gameData, chatKey, originalName);

            if (target.nameLabel != null)
                target.nameLabel.text = displayName;
        }

        RefreshActiveEditorFields();
    }

    public void OpenEditorByIndex(int index)
    {
        if (index < 0 || index >= renameTargets.Count)
            return;

        CharacterRenameTarget target = renameTargets[index];
        if (target == null || string.IsNullOrWhiteSpace(target.chatIdOrThreadKey))
            return;

        activeTargetIndex = index;
        OpenEditPanel();
        RefreshActiveEditorFields();

        if (editNameInputField != null)
            editNameInputField.ActivateInputField();
    }

    public void OpenEditorByChatKey(string chatIdOrThreadKey)
    {
        if (string.IsNullOrWhiteSpace(chatIdOrThreadKey))
            return;

        int index = FindTargetIndexByKey(chatIdOrThreadKey);
        if (index >= 0)
            OpenEditorByIndex(index);
    }

    public void ConfirmActiveRename()
    {
        if (activeTargetIndex < 0 || activeTargetIndex >= renameTargets.Count)
            return;

        if (editNameInputField == null)
            return;

        CharacterRenameTarget target = renameTargets[activeTargetIndex];
        if (target == null)
            return;

        if (!TryResolveChatKeyAndOriginalName(target.chatIdOrThreadKey, out string chatKey, out _))
            return;

        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        if (gameData == null)
            return;

        string newName = editNameInputField.text;
        if (string.IsNullOrWhiteSpace(newName))
            gameData.ResetCustomChatName(chatKey);
        else
            gameData.SetCustomChatName(chatKey, newName);

        phoneChatController?.RefreshChatNames();
        RefreshAllFields();

        CloseEditorPanel();
    }

    public void ResetActiveToOriginal()
    {
        if (activeTargetIndex < 0 || activeTargetIndex >= renameTargets.Count)
            return;

        CharacterRenameTarget target = renameTargets[activeTargetIndex];
        if (target == null)
            return;

        if (!TryResolveChatKeyAndOriginalName(target.chatIdOrThreadKey, out string chatKey, out string originalName))
            return;

        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        if (gameData == null)
            return;

        gameData.ResetCustomChatName(chatKey);

        phoneChatController?.RefreshChatNames();
        RefreshAllFields();

        if (editNameInputField != null)
            editNameInputField.text = originalName;

        CloseEditorPanel();
    }

    public void CancelActiveEdit()
    {
        CloseEditorPanel();
    }

    // Legacy wrappers for already configured OnClick events in scene.
    public void BeginEditByIndex(int index) => OpenEditorByIndex(index);
    public void BeginEditByChatKey(string chatIdOrThreadKey) => OpenEditorByChatKey(chatIdOrThreadKey);
    public void ConfirmRenameByIndex(int index)
    {
        OpenEditorByIndex(index);
        ConfirmActiveRename();
    }

    public void ResetRenameByIndex(int index)
    {
        OpenEditorByIndex(index);
        ResetActiveToOriginal();
    }

    // Legacy single-target workflow retained for compatibility.
    public void SelectCharacterToRename(string chatIdOrThreadKey)
    {
        OpenEditorByChatKey(chatIdOrThreadKey);
    }

    public void ConfirmRename()
    {
        ConfirmActiveRename();
    }

    public void ResetToOriginalName()
    {
        ResetActiveToOriginal();
    }

    public void ConfirmAllRenames()
    {
        // В новой логике подтверждаем активную запись через общую edit panel.
        ConfirmActiveRename();
    }

    public void ResetAllToOriginal()
    {
        // В новой логике сбрасываем активную запись через общую edit panel.
        ResetActiveToOriginal();
    }

    private bool TryResolveChatKeyAndOriginalName(string chatIdOrThreadKey, out string chatKey, out string originalName)
    {
        chatKey = string.Empty;
        originalName = "Chat";

        if (GameManager.Instance == null || GameManager.Instance.ChatDatabase == null)
            return false;

        ChatDatabase database = GameManager.Instance.ChatDatabase;
        Chat matchedById = null;

        for (int i = 0; i < database.chats.Count; i++)
        {
            Chat chat = database.chats[i];
            if (chat == null)
                continue;

            if (chat.id == chatIdOrThreadKey)
            {
                matchedById = chat;
                break;
            }
        }

        if (matchedById != null)
        {
            chatKey = !string.IsNullOrWhiteSpace(matchedById.threadId) ? matchedById.threadId : matchedById.id;
            originalName = string.IsNullOrWhiteSpace(matchedById.name) ? "Chat" : matchedById.name;
            return true;
        }

        // Если передали threadKey напрямую.
        for (int i = 0; i < database.chats.Count; i++)
        {
            Chat chat = database.chats[i];
            if (chat == null)
                continue;

            string threadKey = !string.IsNullOrWhiteSpace(chat.threadId) ? chat.threadId : chat.id;
            if (threadKey != chatIdOrThreadKey)
                continue;

            chatKey = threadKey;
            originalName = string.IsNullOrWhiteSpace(chat.name) ? "Chat" : chat.name;
            return true;
        }

        return false;
    }

    private void RefreshActiveEditorFields()
    {
        if (activeTargetIndex < 0 || activeTargetIndex >= renameTargets.Count)
            return;

        CharacterRenameTarget target = renameTargets[activeTargetIndex];
        if (target == null)
            return;

        if (!TryResolveChatKeyAndOriginalName(target.chatIdOrThreadKey, out string chatKey, out string originalName))
            return;

        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        string displayName = GameData.ResolveChatDisplayName(gameData, chatKey, originalName);

        if (editNameInputField != null)
            editNameInputField.text = displayName;

        if (selectedCharacterLabel != null)
            selectedCharacterLabel.text = originalName;
    }

    private void OpenEditPanel()
    {
        if (editPanel == null)
            return;

        if (menuManager != null)
            menuManager.OpenPanel(editPanel);
        else
            editPanel.SetActive(true);
    }

    private void CloseEditorPanel()
    {
        if (menuManager != null)
        {
            menuManager.ClosePanel();
        }
        else if (editPanel != null)
        {
            editPanel.SetActive(false);
        }

        activeTargetIndex = -1;
    }

    private void ForceCloseEditPanelWithoutHistory()
    {
        if (editPanel != null)
            editPanel.SetActive(false);

        activeTargetIndex = -1;
    }

    private int FindTargetIndexByKey(string chatIdOrThreadKey)
    {
        for (int i = 0; i < renameTargets.Count; i++)
        {
            CharacterRenameTarget target = renameTargets[i];
            if (target == null || string.IsNullOrWhiteSpace(target.chatIdOrThreadKey))
                continue;

            if (target.chatIdOrThreadKey == chatIdOrThreadKey)
                return i;
        }

        return -1;
    }

    private void BindUiButtons()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmActiveRename);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetActiveToOriginal);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelActiveEdit);
    }

    private void UnbindUiButtons()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(ConfirmActiveRename);

        if (resetButton != null)
            resetButton.onClick.RemoveListener(ResetActiveToOriginal);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelActiveEdit);
    }
}
