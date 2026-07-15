using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class PatternDot : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    public enum DotType { Normal, Start, End, Blocker }

    public int id;
    public Image image;

    [Header("Type")]
    public DotType type = DotType.Normal;

    [Header("Colors (неоновые, будут \"гореть\" под Bloom)")]
    public Color normalColor = new Color(0.85f, 0.9f, 1f);
    public Color startColor = new Color(0.2f, 1f, 0.5f);       // неоновый зелёный
    public Color endColor = new Color(1f, 0.85f, 0.2f);        // неоновый жёлтый
    public Color blockerColor = new Color(0.85f, 0.1f, 0.9f);  // неоновый пурпурный
    public Color selectedColor = new Color(0.2f, 0.9f, 1f);    // неоновый циан

    [Header("Neon glow")]
    [Tooltip("Создаётся автоматически в Awake() через EnsureGlow() — не назначайте вручную.")]
    public Image glowImage;
    [Tooltip("Sorting Order отдельного (nested) Canvas на самой точке. Нужен, чтобы точка гарантированно " +
             "рисовалась выше стен (PathPuzzleController.wallSortingOrder), даже если фон использует override Canvas.")]
    public int dotSortingOrder = 0;
    [Tooltip("Sorting Order отдельного (nested) Canvas на glow-объекте. Glow физически лежит внутри " +
             "точки, поэтому без override-канваса рисовался бы вместе с точкой поверх стен.\n\n" +
             "Должен быть БОЛЬШЕ PathPuzzleController.backgroundSortingOrder (иначе glow провалится под " +
             "фон) и МЕНЬШЕ PathPuzzleController.wallSortingOrder (иначе окажется НАД стеной). Итоговый " +
             "порядок слоёв, от заднего к переднему: фон → glow → стена → сама точка (без Canvas).")]
    public int glowSortingOrder = -50;
    [Tooltip("Во сколько раз свечение больше самой точки.")]
    public float glowScale = 2.4f;
    [Tooltip("Базовая альфа свечения в состоянии покоя.")]
    public float glowBaseAlpha = 0.35f;
    [Tooltip("Амплитуда \"дыхания\" свечения в покое (0 = не пульсирует).")]
    public float idlePulseAmount = 0.15f;
    public float idlePulseSpeed = 1.6f;
    [Tooltip("Во сколько раз ярче становится свечение в момент выбора точки.")]
    public float selectGlowBoost = 1.6f;
    public float selectPulseDuration = 0.25f;

    [Header("Icons (Blocker)")]
    [Tooltip("Если не назначено — создастся автоматически как дочерний TMP-текст.")]
    public TextMeshProUGUI iconLabel;
    public string blockerIcon = "▦";
    public Color iconColor = new Color(1f, 1f, 1f, 0.9f);

    private static Sprite _glowSprite;
    private float _idlePulseSeed;
    private Coroutine _selectPulseRoutine;
    private bool _isSelected;

    void Awake()
    {
        // Смещаем фазу пульсации у каждой точки, чтобы вся сетка не "дышала" синхронно.
        _idlePulseSeed = Random.Range(0f, Mathf.PI * 2f);

        EnsureDotLayering();

        // Glow теперь живёт прямо внутри самой точки (см. EnsureGlow()), а не
        // в linesParent, поэтому ему больше не нужна мировая позиция точки —
        // можно создавать сразу в Awake(), не дожидаясь, пока GridLayoutGroup
        // расставит точки по местам. Нужный порядок отрисовки (glow → стена →
        // точка) теперь обеспечивается не иерархией, а отдельным Canvas с
        // overrideSorting на glow-объекте — см. glowSortingOrder.
        EnsureGlow();
        EnsureIcon();
        ApplyIdleColor();
    }

    void OnValidate()
    {
        // Свечение в редакторе не превьюим (оно живёт в linesParent, которого
        // может не быть или который недоступен из инспектора точки) — только
        // цвет самой точки и иконка блокиратора.
        if (image != null)
        {
            EnsureIcon();
            ApplyIdleColor();
        }
    }

    void Update()
    {
        if (_isSelected || glowImage == null) return;

        // Лёгкое "дыхание" свечения в покое — точка выглядит как горящий неон,
        // а не как статично залитый кружок.
        float pulse = Mathf.Sin((Time.time + _idlePulseSeed) * idlePulseSpeed) * idlePulseAmount;
        Color c = glowImage.color;
        c.a = Mathf.Clamp01(glowBaseAlpha + pulse);
        glowImage.color = c;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TrySelectSelf();
    }

    /// <summary>
    /// Нужен отдельно от OnPointerEnter: если мышь нажали, когда курсор уже
    /// стоял над стартовой точкой (не заходя в неё только что), OnPointerEnter
    /// не сработает вообще — Unity UI шлёт его только при пересечении границы
    /// коллайдера. Без этого обработчика пользователю приходилось нажимать
    /// мышь где-то рядом и только потом заводить курсор на стартовую точку.
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        TrySelectSelf(true);
    }

    void TrySelectSelf(bool forceFromPointerDown = false)
    {
        if (PathPuzzleController.Instance != null)
            PathPuzzleController.Instance.TrySelect(this, forceFromPointerDown);
        else
            TryLegacyPatternLockSelect();
    }

    void TryLegacyPatternLockSelect()
    {
        // Опциональная обратная совместимость: если в проекте есть старый PatternLock,
        // пробуем вызвать его без compile-time зависимости от типа.
        System.Type patternLockType = System.Type.GetType("PatternLock") ?? System.Type.GetType("PatternLock, Assembly-CSharp");
        if (patternLockType == null) return;

        PropertyInfo instanceProp = patternLockType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProp == null) return;

        object instance = instanceProp.GetValue(null);
        if (instance == null) return;

        MethodInfo trySelectMethod = patternLockType.GetMethod("TrySelect", BindingFlags.Public | BindingFlags.Instance);
        if (trySelectMethod == null) return;

        trySelectMethod.Invoke(instance, new object[] { this });
    }

    void EnsureDotLayering()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = dotSortingOrder;

        // Для nested Canvas нужен собственный GraphicRaycaster, иначе IPointerDown/IPointerEnter
        // на точке может не приходить от EventSystem (особенно после перевода точки на overrideSorting).
        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    public void Select()
    {
        _isSelected = true;
        image.color = selectedColor;

        if (glowImage != null)
        {
            if (_selectPulseRoutine != null) StopCoroutine(_selectPulseRoutine);
            _selectPulseRoutine = StartCoroutine(PulseOnSelect());
        }
    }

    public void ResetDot()
    {
        _isSelected = false;

        if (_selectPulseRoutine != null)
        {
            StopCoroutine(_selectPulseRoutine);
            _selectPulseRoutine = null;
        }

        if (image != null) image.transform.localScale = Vector3.one;

        ApplyIdleColor();
    }

    public void ApplyIdleColor()
    {
        if (image == null) return;

        Color color;
        switch (type)
        {
            case DotType.Start: color = startColor; break;
            case DotType.End: color = endColor; break;
            case DotType.Blocker: color = blockerColor; break;
            default: color = normalColor; break;
        }

        image.color = color;
        _isSelected = false;

        if (glowImage != null)
        {
            Color glowColor = color;
            glowColor.a = glowBaseAlpha;
            glowImage.color = glowColor;
        }

        UpdateIcon();
    }

    // ───────────── Свечение (текстура генерируется в коде, без внешних спрайтов) ─────────────

    /// <summary>
    /// Создаёт (или переиспользует, если уже существует и жив) квадрат свечения
    /// точки как ДОЧЕРНИЙ объект самой точки. В отличие от старой версии, ему
    /// больше не нужна мировая позиция точки (WorldToLinesParentLocal) и внешний
    /// parent (linesParent) — он просто центрируется внутри точки, поэтому его
    /// можно создавать сразу в Awake().
    ///
    /// Порядок отрисовки строится на явных override Canvas, а не на иерархии:
    /// nested Canvas с overrideSorting не "вставляется между соседями" — он
    /// выдёргивает всё своё поддерево целиком раньше/позже ВСЕГО обычного (без
    /// Canvas) батча, поэтому сравнивать его напрямую с несколькими разными
    /// implicit-элементами (например, и с фоном, и со стеной одновременно)
    /// нельзя одним числом. Поэтому у фона и у стены тоже есть свои explicit
    /// Canvas (см. PathPuzzleController.backgroundSortingOrder / wallSortingOrder) —
    /// тогда сравнение "фон vs glow vs стена" превращается в обычное число-к-числу.
    /// Итоговый порядок, от заднего к переднему: фон → glow → стена → сама точка
    /// (у точки своего override нет — она остаётся в обычном батче, т.е. поверх всего).
    /// </summary>
    public Image EnsureGlow()
    {
        // glowImage может быть "мёртвой" ссылкой на уже уничтоженный объект
        // (например, если Awake() почему-то отработал повторно) — Unity в этом
        // случае сравнение с null даёт true, и мы создадим новый.
        if (glowImage == null)
        {
            var go = new GameObject($"Glow_{name}", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(transform, false);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            RectTransform selfRt = image != null ? image.rectTransform : GetComponent<RectTransform>();
            Vector2 baseSize = selfRt != null ? selfRt.sizeDelta : new Vector2(100, 100);
            rt.sizeDelta = baseSize * glowScale;

            glowImage = go.AddComponent<Image>();
            glowImage.sprite = GetGlowSprite();
            glowImage.raycastTarget = false; // свечение не должно перехватывать клики/hover курсора

            // Отдельный Canvas + overrideSorting "выдёргивает" glow из обычного
            // порядка отрисовки (в котором он иначе рисовался бы как часть точки,
            // т.е. поверх стен) и кладёт его в самый низ независимо от места в иерархии.
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = glowSortingOrder;
        }

        glowImage.rectTransform.SetAsFirstSibling(); // ниже иконки блокиратора и т.п. внутри самой точки

        ApplyIdleColor(); // сразу выставить цвет/альфу свечения согласно текущему type

        return glowImage;
    }

    static Sprite GetGlowSprite()
    {
        if (_glowSprite != null) return _glowSprite;

        const int size = 128;
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
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 1.8f); // мягкое затухание к краю, яркое ядро в центре
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        _glowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _glowSprite;
    }

    IEnumerator PulseOnSelect()
    {
        float t = 0f;
        Color baseColor = selectedColor;

        while (t < selectPulseDuration)
        {
            t += Time.deltaTime;
            float k = t / selectPulseDuration;

            // Резкий всплеск яркости свечения + лёгкий scale-пульс самой точки,
            // затем плавный возврат к стабильному "выбранному" состоянию.
            float boost = Mathf.Lerp(selectGlowBoost, 1f, k);
            Color c = baseColor;
            c.a = Mathf.Clamp01(glowBaseAlpha * boost);
            glowImage.color = c;

            if (image != null)
                image.transform.localScale = Vector3.one * Mathf.Lerp(1.15f, 1f, k);

            yield return null;
        }

        Color finalColor = baseColor;
        finalColor.a = glowBaseAlpha * 1.2f; // выбранная точка светится чуть ярче, чем в покое
        glowImage.color = finalColor;
        if (image != null) image.transform.localScale = Vector3.one;
    }

    // ───────────── Иконка для Blocker ─────────────

    void EnsureIcon()
    {
        if (iconLabel != null) return;

        var go = new GameObject("Icon", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        iconLabel = go.AddComponent<TextMeshProUGUI>();
        iconLabel.alignment = TextAlignmentOptions.Center;
        iconLabel.enableAutoSizing = true;
        iconLabel.fontSizeMin = 10;
        iconLabel.fontSizeMax = 60;
        iconLabel.color = iconColor;
        iconLabel.raycastTarget = false;
    }

    void UpdateIcon()
    {
        if (iconLabel == null) return;

        switch (type)
        {
            case DotType.Blocker:
                iconLabel.text = blockerIcon;
                iconLabel.gameObject.SetActive(true);
                break;
            default:
                iconLabel.text = string.Empty;
                iconLabel.gameObject.SetActive(false);
                break;
        }
    }
}