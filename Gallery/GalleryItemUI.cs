using UnityEngine;
using UnityEngine.UI;
using System;

public class GalleryItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image thumbnail;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private Sprite lockedPlaceholderSprite;
    [SerializeField] private Color lockedTint = Color.red;
    
    private Action<string> onSelected;
    private string itemId;
    private bool isUnlocked;
    
    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    public void Initialize(string id, Action<string> callback)
    {
        itemId = id;
        onSelected = callback;
    }

    public void UpdateVisual(bool unlocked, Sprite unlockedSprite, Sprite lockedSprite = null)
    {
        isUnlocked = unlocked;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked);

        if (button != null)
            button.interactable = unlocked;

        if (thumbnail == null)
            return;

        if (unlocked)
        {
            thumbnail.sprite = unlockedSprite;
            thumbnail.color = Color.white;
            thumbnail.preserveAspect = true;

            if (unlockedSprite == null)
                Debug.LogWarning($"Gallery sprite not found: Images/Gallery/{itemId}");
            return;
        }

        Sprite placeholder = lockedSprite != null ? lockedSprite : lockedPlaceholderSprite;
        thumbnail.sprite = placeholder;
        thumbnail.color = placeholder != null ? Color.red : lockedTint;
        thumbnail.preserveAspect = true;
    }
    
    private void OnClicked()
    {
        if (!isUnlocked)
            return;

        onSelected?.Invoke(itemId);
    }
}

