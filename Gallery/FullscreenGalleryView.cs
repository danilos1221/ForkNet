using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FullscreenGalleryView : MonoBehaviour
{
    [SerializeField] private Image fullscreenImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI imageNameText;
    [SerializeField] private CanvasGroup canvasGroup;

    private System.Action onCloseCallback;

    private void Awake()
    {
        // Подписываемся один раз, а не в OnEnable, т.к. объект больше не пересоздаётся
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (GetComponent<Button>() != null)
            GetComponent<Button>().onClick.AddListener(Close);
    }

    public void Initialize(Sprite sprite, string itemId, System.Action onClose)
    {
        if (sprite != null)
        {
            fullscreenImage.sprite = sprite;
            fullscreenImage.preserveAspect = true;
            Debug.Log($"Displaying fullscreen gallery: {itemId}");
        }
        else
        {
            Debug.LogWarning($"Sprite is null for item: {itemId}");
        }

        if (imageNameText != null)
            imageNameText.text = itemId;

        onCloseCallback = onClose;

        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void Close()
    {
        StartCoroutine(FadeOutAndClose());
    }

    private System.Collections.IEnumerator FadeOutAndClose()
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);   // <-- вместо Destroy(gameObject)
        onCloseCallback?.Invoke();
    }
}