using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Позволяет перемещать иконки на рабочем столе мышью (как Windows)
/// Просто добавьте этот скрипт на DesktopIcon префаб
/// </summary>
public class DraggableIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    
    private Vector2 offset;
    private bool isDragging = false;
    private Vector2 lastSavedPosition;  // Сохраняем последнюю позицию иконки
    
    [SerializeField] private float dragAlpha = 0.7f;  // Прозрачность при перемещении
    [SerializeField] private bool snapToGrid = true;  // Привязка к сетке
    [SerializeField] private int gridSize = 80;       // Размер ячейки сетки
    [SerializeField] private float edgePadding = 50f;  // Отступ от края контейнера (чтобы иконку можно было вытащить)
    
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
        
        // Запоминаем текущую позицию при старте
        lastSavedPosition = rectTransform.localPosition;
    }
    
    /// <summary>
    /// Восстанавливаем позицию когда иконка становится активной
    /// (защита от сброса позиции при переключении окон)
    /// </summary>
    private void OnEnable()
    {
        if (rectTransform != null && lastSavedPosition != Vector2.zero)
        {
            rectTransform.localPosition = lastSavedPosition;
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
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );
        
        offset = (Vector2)rectTransform.localPosition - localPointerPosition;
        
        // Снижаем прозрачность при перемещении
        canvasGroup.alpha = dragAlpha;
        
        // Поднимаем элемент в иерархии (чтобы он был сверху)
        rectTransform.SetAsLastSibling();
    }
    
    /// <summary>
    /// Перемещение (ЛКМ зажата и двигается)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        // Получаем позицию курсора в локальных координатах родителя
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );
        
        // Применяем смещение
        Vector2 newPosition = localPointerPosition + offset;
        
        // Ограничиваем позицию в границах контейнера (рабочий стол)
        newPosition = ClampPositionToParent(newPosition);
        
        // Устанавливаем ограниченную позицию
        rectTransform.localPosition = newPosition;
    }
    
    /// <summary>
    /// Ограничить позицию иконки в границах контейнера (рабочего стола)
    /// </summary>
    private Vector2 ClampPositionToParent(Vector2 position)
    {
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) return position;
        
        // Получаем размеры рабочего стола (контейнера)
        Rect parentRect_rect = parentRect.rect;
        float parentLeft = parentRect_rect.xMin;      // Левый край
        float parentRight = parentRect_rect.xMax;     // Правый край
        float parentBottom = parentRect_rect.yMin;    // Нижний край
        float parentTop = parentRect_rect.yMax;       // Верхний край
        
        // Ограничиваем с отступом от края (padding)
        float minX = parentLeft + edgePadding;
        float maxX = parentRight - edgePadding;
        float minY = parentBottom + edgePadding;
        float maxY = parentTop - edgePadding;
        
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
        
        // Если включена привязка к сетке - прилипаем к ней
        if (snapToGrid)
        {
            SnapToGrid();
        }
        
        // Сохраняем позицию (чтобы она не сбросилась при переключении окон)
        lastSavedPosition = rectTransform.localPosition;
    }
    
    /// <summary>
    /// Привязка к сетке для аккуратного расположения
    /// </summary>
    private void SnapToGrid()
    {
        Vector2 pos = rectTransform.localPosition;
        
        pos.x = Mathf.Round(pos.x / gridSize) * gridSize;
        pos.y = Mathf.Round(pos.y / gridSize) * gridSize;
        
        rectTransform.localPosition = pos;
    }
    
    /// <summary>
    /// Утилита: получить текущую позицию иконки (для сохранения)
    /// </summary>
    public Vector2 GetPosition()
    {
        return rectTransform.localPosition;
    }
    
    /// <summary>
    /// Утилита: установить позицию иконки (для загрузки)
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        rectTransform.localPosition = position;
    }
}
