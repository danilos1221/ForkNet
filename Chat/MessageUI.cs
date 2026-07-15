using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Компонент сообщения для Telegram-подобного чата
/// Поддерживает 2 режима:
/// 1. Приватный чат - только текст + время (без имени)
/// 2. Групповой чат - текст + имя + время
/// </summary>
public class MessageUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Image messageBackground;
    [SerializeField] private HorizontalLayoutGroup horizontalLayout;
    [SerializeField] private Image arrowIcon;  // Стрелка, указывающая на отправителя
    [SerializeField] private Image imageContent;  // Image компонент для отображения изображений сообщений
    [SerializeField] private Button imageButton;  // Кнопка для открытия полноэкранной галереи

    [Header("Colors")]
    [SerializeField] private Color otherMessageColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color playerMessageColor = new Color(0.2f, 0.8f, 0.2f, 1f);

    private bool isMessage = true;
    private string currentImageId;  // ID текущего изображения

    public void SetIsMessage(bool value)
    {
        isMessage = value;
    }
    
    /// <summary>
    /// Установить ID изображения для открытия в полноэкранной галерее
    /// </summary>
    public void SetImageId(string imageId)
    {
        currentImageId = imageId;
    }
    
    /// <summary>
    /// Открыть изображение в полноэкранной галерее
    /// </summary>
    private void OnImageButtonClicked()
    {
        if (string.IsNullOrEmpty(currentImageId))
        {
            Debug.LogWarning("No image ID set!");
            return;
        }
        
        // Загружаем спрайт
        Sprite sprite = Resources.Load<Sprite>($"Images/Gallery/{currentImageId}");
        if (sprite == null)
        {
            Debug.LogWarning($"Image not found: Images/Gallery/{currentImageId}");
            return;
        }
        
        // Находим GalleryManager и открываем полноэкранный вид
        GalleryManager galleryManager = FindAnyObjectByType<GalleryManager>();
        if (galleryManager != null)
        {
            galleryManager.OpenFullscreenViewForImage(sprite, currentImageId);
        }
        else
        {
            Debug.LogWarning("GalleryManager not found!");
        }
    }
    
    private void OnEnable()
    {
        // Подписываем кнопку на нажатие
        if (imageButton != null)
        {
            imageButton.onClick.AddListener(OnImageButtonClicked);
        }
        
        // Инициализация - по умолчанию скрываем имя
        if (nameText != null)
        {
            nameText.text = "";
        }
    }
    
    private void OnDisable()
    {
        // Отписываемся от события
        if (imageButton != null)
        {
            imageButton.onClick.RemoveListener(OnImageButtonClicked);
        }
    }

    /// <summary>
    /// ПРИВАТНЫЙ ЧАТ - Сообщение другого персонажа (слева, без имени)
    /// </summary>
    public void SetOtherMessage(string message)
    {
        HideNameText();
        messageText.text = message;
        messageText.alignment = TextAlignmentOptions.BottomLeft;
        timeText.alignment = TextAlignmentOptions.BottomRight;
        
        AlignToLeft();
        SetArrowPosition(isPlayer: false);
        SetTime();
    }

    /// <summary>
    /// ПРИВАТНЫЙ ЧАТ - Сообщение игрока (справа)
    /// </summary>
    public void SetPlayerMessage(string message)
    {
        HideNameText();
        messageText.text = message;
        messageText.alignment = TextAlignmentOptions.BottomRight;
        timeText.alignment = TextAlignmentOptions.BottomLeft;
        
        AlignToRight();
        SetArrowPosition(isPlayer: true);
        SetTime();
    }

    /// <summary>
    /// ГРУППОВОЙ ЧАТ - Сообщение другого персонажа (слева, с именем)
    /// </summary>
    public void SetGroupOtherMessage(string characterName, string message)
    {
        if (nameText != null)
        {
            nameText.gameObject.SetActive(true);
            nameText.text = characterName;
        }
        
        messageText.text = message;
        messageText.alignment = TextAlignmentOptions.BottomLeft;
        timeText.alignment = TextAlignmentOptions.BottomRight;
        
        AlignToLeft();
        SetArrowPosition(isPlayer: false);
        SetTime();
    }

    /// <summary>
    /// ГРУППОВОЙ ЧАТ - Сообщение игрока (справа)
    /// </summary>
    public void SetGroupPlayerMessage(string message)
    {
        HideNameText();
        messageText.text = message;
        messageText.alignment = TextAlignmentOptions.BottomRight;
        timeText.alignment = TextAlignmentOptions.BottomLeft;
        
        AlignToRight();
        SetArrowPosition(isPlayer: true);
        SetTime();
    }


    private void AlignToLeft()
    {
        if (horizontalLayout != null)
            horizontalLayout.childAlignment = TextAnchor.LowerLeft;
    }

    private void AlignToRight()
    {
        if (horizontalLayout != null)
            horizontalLayout.childAlignment = TextAnchor.LowerRight;
    }

    private void HideNameText()
    {
        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
            nameText.text = "";
        }
    }

    /// <summary>
    /// Позиционирует стрелку в зависимости от отправителя сообщения
    /// </summary>
    private void SetArrowPosition(bool isPlayer)
    {
        if (arrowIcon == null)
            return;

        if (isPlayer)
        {
            arrowIcon.transform.SetAsLastSibling();
            arrowIcon.transform.localRotation = Quaternion.Euler(0, 0, 180);
            if (isMessage)
            {
                messageBackground.transform.localScale = new Vector3(-1, 1, 1);
                messageText.transform.localScale = new Vector3(-1, 1, 1);
            }
        }
        else
        {
            arrowIcon.transform.SetAsFirstSibling();
            arrowIcon.transform.localRotation = Quaternion.Euler(0, 0, 0);
            if (isMessage)
            {
                messageBackground.transform.localScale = Vector3.one;
                messageText.transform.localScale = Vector3.one;
            }
        }
    }


    private void SetTime()
    {
        timeText.text = System.DateTime.Now.ToString("HH:mm");
    }
    
    /// <summary>
    /// Отобразить изображение в приватном чате
    /// </summary>
    public void SetPlayerImage(Sprite sprite, string imageId = "")
    {
        currentImageId = imageId;
        
        // Показываем кнопку если есть ID
        if (imageButton != null)
        {
            imageButton.gameObject.SetActive(!string.IsNullOrEmpty(imageId));
        }
        
        // Показываем изображение
        if (imageContent != null)
        {
            imageContent.gameObject.SetActive(true);
            imageContent.sprite = sprite;
        }
        
        // Выравниваем вправо (сообщение от игрока)
        isMessage = false;
        AlignToRight();
        SetArrowPosition(isPlayer: true);
    }
    
    /// <summary>
    /// Отобразить изображение от другого персонажа в приватном чате
    /// </summary>
    public void SetOtherImage(Sprite sprite, string imageId = "")
    {
        currentImageId = imageId;
        
        // Показываем кнопку если есть ID
        if (imageButton != null)
        {
            imageButton.gameObject.SetActive(!string.IsNullOrEmpty(imageId));
        }
        
        // Показываем изображение
        if (imageContent != null)
        {
            imageContent.gameObject.SetActive(true);
            imageContent.sprite = sprite;
        }
        
        // Выравниваем влево (сообщение от другого)
        isMessage = false;
        AlignToLeft();
        SetArrowPosition(isPlayer: false);
    }
    
    /// <summary>
    /// Отобразить изображение в групповом чате от игрока
    /// </summary>
    public void SetGroupPlayerImage(Sprite sprite, string imageId = "")
    {
        currentImageId = imageId;
        
        // Показываем кнопку если есть ID
        if (imageButton != null)
        {
            imageButton.gameObject.SetActive(!string.IsNullOrEmpty(imageId));
        }
        
        // Показываем изображение
        if (imageContent != null)
        {
            imageContent.gameObject.SetActive(true);
            imageContent.sprite = sprite;
        }
        
        // Выравниваем вправо
        isMessage = false;
        AlignToRight();
        SetArrowPosition(isPlayer: true);
    }
    
    /// <summary>
    /// Отобразить изображение в групповом чате от другого персонажа
    /// </summary>
    public void SetGroupOtherImage(string characterName, Sprite sprite, string imageId = "")
    {
        currentImageId = imageId;
        
        // Показываем кнопку если есть ID
        if (imageButton != null)
        {
            imageButton.gameObject.SetActive(!string.IsNullOrEmpty(imageId));
        }
        
        // Скрываем текст
        messageText.gameObject.SetActive(false);
        
        // Показываем изображение
        if (imageContent != null)
        {
            imageContent.gameObject.SetActive(true);
            imageContent.sprite = sprite;
        }
        
        // Выравниваем влево
        isMessage = false;
        AlignToLeft();
        SetArrowPosition(isPlayer: false);
    }
    
    /// <summary>
    /// Очистить сообщение для переиспользования в объектном пуле
    /// </summary>
    public void Clear()
    {
        // Очищаем текст
        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(true);
        }
        
        // Очищаем имя
        if (nameText != null)
        {
            nameText.text = "";
            nameText.gameObject.SetActive(false);
        }
        
        // Очищаем время
        if (timeText != null)
        {
            timeText.text = "";
        }
        
        // Очищаем изображение
        if (imageContent != null)
        {
            imageContent.sprite = null;
            imageContent.gameObject.SetActive(false);
        }
        
        // Сбрасываем трансформации
        if (messageBackground != null)
        {
            messageBackground.transform.localScale = new Vector3(1, 1, 1);
        }
        if (messageText != null)
        {
            messageText.transform.localScale = new Vector3(1, 1, 1);
        }
    }
}