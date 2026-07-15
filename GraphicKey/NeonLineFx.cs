using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Добавляет неоновое свечение и анимацию "рисования" (рост от 0 до полной длины)
/// для UI-линий пути (см. PathPuzzleController.CreateLine / UpdateCursorLine).
///
/// Предполагается, что RectTransform линии использует pivot = (0, 0.5) —
/// то есть левый край примыкает к точке-началу сегмента, а сама линия растёт
/// вдоль локальной оси X после поворота (см. PathPuzzleController.SetLineTransform).
/// Это стандартная схема для UI-линий "точка-точка" и обычно уже настроена в linePrefab.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class NeonLineFx : MonoBehaviour
{
    [Header("Idle glow pulse")]
    public float idlePulseAmount = 0.12f;
    public float idlePulseSpeed = 2.2f;

    private static Sprite _glowSprite;

    private RectTransform _self;
    private RectTransform _glow;
    private Image _glowImage;
    private Color _baseGlowColor;
    private float _glowPadding = 14f;
    private float _idlePulseSeed;
    private Coroutine _drawRoutine;

    void Awake()
    {
        _self = (RectTransform)transform;
        _idlePulseSeed = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        if (_glowImage == null) return;

        // Лёгкое "дыхание" свечения — тот же приём, что и у PatternDot,
        // чтобы линии и точки визуально были одним стилем.
        float pulse = Mathf.Sin((Time.time + _idlePulseSeed) * idlePulseSpeed) * idlePulseAmount;
        Color c = _baseGlowColor;
        c.a = Mathf.Clamp01(_baseGlowColor.a + pulse);
        _glowImage.color = c;
    }

    /// <summary>Настраивает цвет линии и её свечения. Вызывать сразу после Instantiate,
    /// когда RectTransform линии уже выставлен в финальное положение/размер.</summary>
    public void Setup(Color color, float glowPadding = 14f, float glowAlpha = 0.45f)
    {
        _glowPadding = glowPadding;

        var img = GetComponent<Image>();
        if (img != null) img.color = color;

        EnsureGlow();

        _baseGlowColor = color;
        _baseGlowColor.a = glowAlpha;
        _glowImage.color = _baseGlowColor;

        SyncGlowSize();
    }

    /// <summary>
    /// Анимирует рост линии от 0 до её текущей ширины (sizeDelta.x на момент вызова
    /// считается финальной длиной сегмента — выставьте её через SetLineTransform ДО вызова).
    /// </summary>
    public void PlayDrawIn(float duration)
    {
        if (_drawRoutine != null) StopCoroutine(_drawRoutine);
        _drawRoutine = StartCoroutine(DrawRoutine(_self.sizeDelta.x, duration));
    }

    /// <summary>
    /// Мгновенно подгоняет свечение под текущий размер линии — используется для
    /// курсорной линии, которая меняет длину каждый кадр без анимации роста.
    /// </summary>
    public void SyncNow()
    {
        SyncGlowSize();
    }

    IEnumerator DrawRoutine(float targetWidth, float duration)
    {
        float height = _self.sizeDelta.y;
        _self.sizeDelta = new Vector2(0f, height);
        SyncGlowSize();

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            _self.sizeDelta = new Vector2(Mathf.Lerp(0f, targetWidth, k), height);
            SyncGlowSize();
            yield return null;
        }

        _self.sizeDelta = new Vector2(targetWidth, height);
        SyncGlowSize();
    }

    void EnsureGlow()
    {
        if (_glow != null) return;

        var go = new GameObject("Glow", typeof(RectTransform));
        _glow = go.GetComponent<RectTransform>();
        _glow.SetParent(_self, false);
        _glow.SetAsFirstSibling(); // свечение рисуется ЗА основной линией
        _glow.anchorMin = new Vector2(0f, 0.5f);
        _glow.anchorMax = new Vector2(0f, 0.5f);
        _glow.pivot = new Vector2(0f, 0.5f);

        _glowImage = go.AddComponent<Image>();
        _glowImage.sprite = GetGlowSprite();
        _glowImage.raycastTarget = false;
    }

    void SyncGlowSize()
    {
        if (_glow == null) return;

        float w = _self.sizeDelta.x;
        float h = _self.sizeDelta.y;

        // Свечение чуть больше самой линии со всех сторон (padding),
        // и поскольку это дочерний объект линии — поворот наследуется автоматически.
        _glow.anchoredPosition = new Vector2(-_glowPadding, 0f);
        _glow.sizeDelta = new Vector2(w + _glowPadding * 2f, h + _glowPadding * 2f);
    }

    static Sprite GetGlowSprite()
    {
        if (_glowSprite != null) return _glowSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        // Сэмплится с растяжением (Image.type = Simple по умолчанию), поэтому
        // круглый градиент на прямоугольнике линии превращается в мягкий овальный ореол —
        // это нормальный и часто используемый вид неонового "свечения линии".
        _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _glowSprite;
    }
}
