using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Позволяет перемещать окна приложений мышью (как в Windows)
/// Добавьте этот скрипт на Canvas окна (ChatWindow, MusicWindow, GalleryWindow)
/// Или на заголовок окна для более удобного захвата
/// </summary>
public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    
    private Vector2 offset;
    private bool isDragging = false;
    
    [SerializeField] private float dragAlpha = 0.8f;  // Прозрачность при перемещении
    [SerializeField] private bool stayOnTop = true;   // Поднимать окно на передний план при перемещении
    
    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = FindAnyObjectByType<Canvas>();
        
        // Добавляем CanvasGroup если его нет (для изменения прозрачности)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    /// <summary>
    /// Начало перемещения (зажата ЛКМ)
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        
        // Вычисляем смещение от центра элемента до позиции курсора
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );
        
        offset = (Vector2)rectTransform.anchoredPosition - localPointerPosition;
        
        // Снижаем прозрачность при перемещении
        canvasGroup.alpha = dragAlpha;
        
        // Поднимаем окно в иерархии (чтобы оно было сверху)
        if (stayOnTop)
        {
            rectTransform.SetAsLastSibling();
        }
        
        Debug.Log($"Начало перемещения окна: {gameObject.name}");
    }
    
    /// <summary>
    /// Перемещение (ЛКМ зажата и двигается)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // Получаем позицию курсора в локальных координатах Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );
        
        // Применяем смещение
        Vector2 newPosition = localPointerPosition + offset;
        
        // Ограничиваем позицию в границах Canvas
        newPosition = ClampPositionToCanvas(newPosition);
        
        // Устанавливаем ограниченную позицию
        rectTransform.anchoredPosition = newPosition;
    }
    
    /// <summary>
    /// Ограничить позицию окна в границах Canvas
    /// </summary>
    private Vector2 ClampPositionToCanvas(Vector2 position)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        
        // Получаем размеры Canvas
        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;
        
        // Получаем размеры текущего окна
        float windowWidth = rectTransform.rect.width;
        float windowHeight = rectTransform.rect.height;
        
        // Вычисляем min и max позиции (центр окна должен быть в пределах Canvas)
        float minX = -canvasWidth / 2 + windowWidth / 2;
        float maxX = canvasWidth / 2 - windowWidth / 2;
        float minY = -canvasHeight / 2 + windowHeight / 2;
        float maxY = canvasHeight / 2 - windowHeight / 2;
        
        // Применяем ограничения
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        
        return position;
    }
    
    /// <summary>
    /// Конец перемещения (отпущена ЛКМ)
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // Возвращаем полную прозрачность
        canvasGroup.alpha = 1f;
        
        Debug.Log($"Конец перемещения окна: {gameObject.name} на позиции {rectTransform.anchoredPosition}");
    }
    
    /// <summary>
    /// Утилита: получить текущую позицию окна (для сохранения)
    /// </summary>
    public Vector2 GetPosition()
    {
        return rectTransform.anchoredPosition;
    }
    
    /// <summary>
    /// Утилита: установить позицию окна (для загрузки)
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }
}
