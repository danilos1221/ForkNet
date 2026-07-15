using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Мини-игра: провести линию от точки Start к точке End по сетке точек,
/// огибая точки-блокираторы (Blocker). Линия НЕ может проходить через
/// блокиратор — ни как соседняя точка, ни как "средняя" точка при
/// прыжке через клетку (аналог диагонали в PatternLock).
/// </summary>
public class PathPuzzleController : MonoBehaviour
{
    public static PathPuzzleController Instance;

    public Camera uiCamera;
    private Camera canvasCamera;

    [Header("Layering (nested Canvas sorting)")]
    [Tooltip("Фоновая графика (Image) позади всей мини-игры — как правило, лежит на этом же объекте " +
             "(общий родитель linesParent/gridParent) или назначается явно сюда. Ей автоматически " +
             "выставляется override Canvas с backgroundSortingOrder — самым нижним слоем игры.\n\n" +
             "Это нужно потому, что nested Canvas с overrideSorting не 'вставляется между соседями' — он " +
             "выдёргивает всё своё поддерево целиком ПЕРЕД или ПОСЛЕ ВСЕГО обычного (без Canvas) батча " +
             "канваса. Раз glow (см. PatternDot.glowSortingOrder) и стены (wallSortingOrder) используют " +
             "отрицательный override, чтобы уйти под точки — они утянут себя и под фон, если у фона нет " +
             "своего явного слоя. Явный Canvas на фоне с числом МЕНЬШЕ, чем у glow, чинит это: тогда " +
             "сравнение идёт explicit-число к explicit-числу, а не explicit к целому неразрывному батчу.")]
    public Graphic backgroundGraphic;
    public int backgroundSortingOrder = -100;

    [Tooltip("Sorting Order для стен (см. DrawWalls()). Должен быть БОЛЬШЕ backgroundSortingOrder и " +
             "БОЛЬШЕ PatternDot.glowSortingOrder (стена рисуется над фоном и glow), но МЕНЬШЕ 0 — сами " +
             "точки остаются без override-канваса и рисуются как обычно, то есть поверх абсолютно всего.")]
    public int wallSortingOrder = -10;
    [Tooltip("Sorting Order для линий пути и курсорной линии. Должен быть БОЛЬШЕ wallSortingOrder, " +
             "чтобы путь читался поверх стен, и МЕНЬШЕ dotSortingOrder у PatternDot, чтобы точки " +
             "оставались на переднем плане.")]
    public int pathSortingOrder = -5;

    [Header("Grid")]
    public PatternDot[] dots;              // назначить в инспекторе, каждой точке выставить type
    public int gridColumns = 10;           // ширина сетки (сколько точек в ряду), напр. 10 для сетки 3x10
    public RectTransform linesParent;
    public RectTransform linePrefab;

    [Header("Walls Visualization (neon)")]
    public RectTransform wallLinePrefab;   // если не назначен — используется linePrefab
    // Янтарно-оранжевый — читается как "стена под напряжением", отличается от
    // пурпурного Blocker, хотя оба про "нельзя касаться/пересекать".
    public Color wallLineColor = new Color(1f, 0.55f, 0.1f, 1f);
    public float wallLineThickness = 12f;
    public float wallLineGlowPadding = 10f;
    public float wallLineGlowAlpha = 0.5f;
    [Tooltip("Скорость мерцания стены — обычно быстрее и резче, чем \"дыхание\" обычной линии пути.")]
    public float wallFlickerSpeed = 5f;
    public float wallFlickerAmount = 0.2f;
    [Tooltip("Анимировать появление стен при инициализации уровня — стена за стеной, а не все сразу.")]
    public bool animateWallsOnInit = true;
    public float wallDrawDuration = 0.18f;
    public float wallDrawStagger = 0.05f;

    [Header("Path Line Style (neon)")]
    // Цвет и толщина отрезков пройденного пути (не курсорной линии-подсказки).
    public Color lineColor = new Color(0.2f, 0.9f, 1f, 1f);
    public float lineThickness = 20f;
    // Сколько времени занимает анимация "рисования" нового отрезка от точки до точки.
    public float drawAnimationDuration = 0.12f;
    // На сколько пикселей свечение выступает за пределы самой линии.
    public float lineGlowPadding = 14f;
    public float lineGlowAlpha = 0.45f;
    // Курсорная линия (та, что тянется за мышью) обычно чуть менее яркая, чем зафиксированный путь.
    public float cursorLineGlowAlpha = 0.3f;

    [Header("Error Feedback")]
    [Tooltip("Цвет, в который на короткое время окрашивается курсорная линия при нарушении правил (стена, запретная точка, пересечение пути).")]
    public Color errorLineColor = new Color(1f, 0.15f, 0.15f, 1f); // тревожный неоновый красный
    [Tooltip("Сколько секунд курсорная линия держит цвет ошибки, прежде чем вернуться к обычному.")]
    public float errorFlashDuration = 0.25f;

    [Header("Auto-capture")]
    // Если линия от последней точки до курсора проходит ближе этого расстояния
    // (в локальных единицах linesParent) от ещё не выбранной точки — точка
    // подключается автоматически, даже если курсор не заходил прямо в неё.
    // Нужно, когда игрок быстро тянет мышь и "перепрыгивает" точки.
    public float autoCaptureRadius = 40f;

    [Header("Blocker Points")]
    // Точки типа Blocker работают как стены: если рисуемая линия проходит
    // ближе этого расстояния от такой точки — соединение полностью
    // запрещается, даже если сама точка не является ни концом сегмента,
    // ни "средней" точкой при прыжке через клетку.
    public float blockerTouchRadius = 30f;

    [Header("UI")]
    public TMPro.TextMeshProUGUI statusText;

    [Header("Events")]
    public UnityEvent onWin;
    public UnityEvent onFail;

    private List<RectTransform> lines = new();
    private List<RectTransform> wallLines = new();
    private List<PatternDot> selectedDots = new();
    private RectTransform cursorLine;
    private Coroutine errorFlashRoutine;

    private PatternDot startDot;
    private PatternDot endDot;

    private bool isDrawing = false;
    private bool locked = false; // блокируем ввод после победы/поражения, пока не Reset()
    private bool isRejecting = false; // блокируем ввод на время вспышки ошибки и последующего сброса пути

    private bool initialized = false;
    [System.Serializable]
    public struct Wall
    {
        public int from;
        public int to;
    }

    public List<Wall> walls = new();

    // Уже нарисованные отрезки текущего пути — нужны, чтобы новая линия
    // не могла пересечь ранее проведённую (даже если формально идёт
    // между разрешёнными точками).
    private struct Segment
    {
        public PatternDot a;
        public PatternDot b;
    }
    private List<Segment> pathSegments = new();

    void Awake()
    {
        Instance = this;
        EnsureCanvasCamera();

        // Если точки уже назначены вручную в инспекторе (без генератора) — инициализируемся сразу.
        // Если точки спавнит PatternGridGenerator, он вызовет Initialize() сам после генерации,
        // т.к. порядок Awake() между разными скриптами не гарантирован.
        if (dots != null && dots.Length > 0)
            Initialize();
    }

    /// <summary>
    /// Настраивает canvasCamera, если это ещё не сделано. Вызывается и из Awake(),
    /// и из Initialize() — последнее нужно на случай, если PatternGridGenerator
    /// вызовет Initialize() раньше, чем отработает Awake() этого компонента.
    /// </summary>
    void EnsureCanvasCamera()
    {
        if (canvasCamera != null || linesParent == null) return;
        Canvas canvas = linesParent.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCamera;
    }

    /// <summary>
    /// Вызывается либо самим Awake (ручная сетка), либо PatternGridGenerator
    /// (сгенерированная сетка). Может вызываться повторно — например, при
    /// смене уровня в рантайме через PatternGridGenerator.ApplyLevel — поэтому
    /// первым делом полностью стирает состояние предыдущего прохождения
    /// (путь, курсорную линию, старые стены и перенесённые Glow-объекты),
    /// чтобы они не накапливались как мусор в linesParent.
    /// </summary>
    public void Initialize()
    {
        EnsureCanvasCamera();
        ResetRuntimeState();

        startDot = null;
        endDot = null;

        foreach (var dot in dots)
        {
            if (dot.type == PatternDot.DotType.Start) startDot = dot;
            if (dot.type == PatternDot.DotType.End) endDot = dot;
        }

        if (startDot == null || endDot == null)
            Debug.LogWarning("PathPuzzleController: не назначены Start или End точки в массиве dots.");

        // Явно закрепляем фон как самый нижний слой (см. EnsureBackgroundLayering) —
        // иначе override Canvas у glow/стен "провалит" их под фон целиком, а не
        // только под точки, потому что фон физически — часть того же
        // неразделённого обычного (без Canvas) батча канваса.
        EnsureBackgroundLayering();

        // Glow каждой точки создаётся самой точкой в PatternDot.Awake() (см.
        // PatternDot.EnsureGlow()) и лежит у неё внутри — контроллеру больше не
        // нужно переносить его в linesParent, достаточно просто нарисовать стены.
        DrawWalls();

        initialized = true;
        SetStatus("Начните с зелёной точки");
        Debug.Log($"Start = {startDot}, End = {endDot}");
    }

    /// <summary>
    /// Стирает всё, что относится к предыдущему прохождению/уровню, не
    /// трогая сами объекты точек (dots) — на момент вызова из Initialize()
    /// они уже либо новые (после смены уровня), либо те же самые (обычный
    /// первый запуск), поэтому обращаться к старым PatternDot тут не нужно.
    /// </summary>
    void ResetRuntimeState()
    {
        StopErrorFlash();
        DestroyCursorLine();

        foreach (var line in lines) if (line != null) Destroy(line.gameObject);
        lines.Clear();

        foreach (var wall in wallLines) if (wall != null) Destroy(wall.gameObject);
        wallLines.Clear();

        selectedDots.Clear();
        pathSegments.Clear();

        locked = false;
        isDrawing = false;
        isRejecting = false;
        initialized = false;
    }

    /// <summary>
    /// Выставляет фоновой графике (backgroundGraphic, либо Image/Graphic на этом же
    /// объекте, если явно не назначено) собственный override Canvas с самым низким
    /// sortingOrder в игре. Без этого фон остаётся частью обычного (без Canvas) батча
    /// канваса — и тогда override Canvas у glow/стен, у которых sortingOrder меньше 0,
    /// "проваливает" их под фон целиком, а не только под точки: override-поддерево
    /// всегда целиком раньше/позже ВСЕГО неразделённого батча, а не вставляется в
    /// произвольное место внутри него. Явный Canvas на фоне превращает сравнение
    /// "фон vs glow vs стена" в обычное число-к-числу между явными override-канвасами.
    /// </summary>
    void EnsureBackgroundLayering()
    {
        if (backgroundGraphic == null)
            backgroundGraphic = GetComponent<Graphic>(); // фон обычно лежит на этом же объекте — общем родителе linesParent/gridParent
        if (backgroundGraphic == null) return;

        // Фон не должен перехватывать ввод — клики должны попадать в точки.
        backgroundGraphic.raycastTarget = false;

        Canvas canvas = backgroundGraphic.GetComponent<Canvas>();
        if (canvas == null) canvas = backgroundGraphic.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = backgroundSortingOrder;
    }

    void Update()
    {
        if (locked || !initialized) return;

        if (IsPrimaryPointerPressed() && isDrawing)
        {
            UpdateCursorLine();
            CheckAutoCapture();
        }

        if (WasPrimaryPointerReleasedThisFrame() && isDrawing)
        {
            isDrawing = false;
            DestroyCursorLine();
            EvaluatePath();
        }
    }

    // Вызывается точкой (например, из OnPointerEnter/OnMouseEnter на PatternDot)
    public void TrySelect(PatternDot dot, bool forceFromPointerDown = false)
    {
        Debug.Log(dot.name);
        if (locked || isRejecting) return;
        if (!forceFromPointerDown && !IsPrimaryPointerPressed()) return;

        
        // Разрешаем взять в работу только если это старт (первая точка) либо мы уже рисуем
        if (selectedDots.Count == 0)
        {
            if (dot != startDot) return; // рисовать можно только начиная со старта
            AddDot(dot, null);
            isDrawing = true;
            return;
        }

        if (selectedDots.Contains(dot)) return;

        // Блокиратор нельзя выбрать вообще
        if (dot.type == PatternDot.DotType.Blocker)
        {
            Reject("Эту точку нельзя касаться!");
            return;
        }

        PatternDot last = selectedDots[^1];

        // Если между last и dot есть "средняя" точка (прыжок через клетку) —
        // реальные сегменты пути это last->between и between->dot,
        // а не last->dot напрямую. Стены и блокираторы нужно проверять
        // именно на этих сегментах.
        PatternDot between = GetDotBetween(last, dot);
        if (between != null)
        {
            if (IsWall(last, between) || IsWall(between, dot))
            {
                Reject("Путь перекрыт!");
                return;
            }

            if (between.type == PatternDot.DotType.Blocker)
            {
                Reject("Путь перекрыт — нельзя касаться этой точки!");
                return;
            }

            bool betweenAlreadySelected = selectedDots.Contains(between);

            // Если "средняя" точка прыжка уже есть в пути, но не является
            // непосредственным предшественником last (то есть путь пришёл в
            // last не через between последним шагом) — курсор пытается
            // "срезать" через чужой, более ранний узел уже нарисованного пути.
            // Раньше в этом случае проверки пересечения на сегменте
            // last->between вообще пропускались (см. ниже), что позволяло
            // тянуть новую линию как бы "из" старой точки, минуя реальную
            // геометрию: путь визуально ветвился в between, а отрезок
            // last->between не рисовался и не проверялся вовсе. Запрещаем
            // такой ход целиком — легитимен только прыжок через клетку,
            // где between — точка, из которой путь и правда пришёл в last.
            if (betweenAlreadySelected)
            {
                int lastIdx = selectedDots.Count - 1;
                bool isDirectPredecessor = lastIdx > 0 && selectedDots[lastIdx - 1] == between;
                if (!isDirectPredecessor)
                {
                    Reject("Нельзя пересекать уже пройденный путь!");
                    return;
                }
            }

            // Проверяем пересечение с уже нарисованными линиями пути
            // и со стенами-барьерами на обоих сегментах — теперь всегда,
            // а не только когда between выбирается впервые.
            if (WouldCrossExistingLine(last, between))
            {
                Reject("Линия пересекает уже нарисованную!");
                return;
            }
            if (WouldCrossWall(last, between))
            {
                Reject("Линия задевает стену!");
                return;
            }
            if (WouldTouchBlocker(last, between))
            {
                Reject("Линия задевает точку, которую нельзя касаться!");
                return;
            }
            if (WouldCrossExistingLine(between, dot))
            {
                Reject("Линия пересекает уже нарисованную!");
                return;
            }
            if (WouldCrossWall(between, dot))
            {
                Reject("Линия задевает стену!");
                return;
            }
            if (WouldTouchBlocker(between, dot))
            {
                Reject("Линия задевает точку, которую нельзя касаться!");
                return;
            }

            if (!betweenAlreadySelected)
                AddDot(between, last);
            last = between;
        }
        else
        {
            if (IsWall(last, dot))
            {
                Reject("Путь перекрыт!");
                return;
            }

            if (WouldCrossExistingLine(last, dot))
            {
                Reject("Линия пересекает уже нарисованную!");
                return;
            }

            if (WouldCrossWall(last, dot))
            {
                Reject("Линия задевает стену!");
                return;
            }

            if (WouldTouchBlocker(last, dot))
            {
                Reject("Линия задевает точку, которую нельзя касаться!");
                return;
            }
        }

        AddDot(dot, last);

        // Если дотянулись до финиша — сразу завершаем успехом
        if (dot == endDot)
        {
            isDrawing = false;
            DestroyCursorLine();
            EvaluatePath();
        }
    }

    void AddDot(PatternDot dot, PatternDot from)
    {
        if (from != null)
        {
            CreateLine(from, dot);
            pathSegments.Add(new Segment { a = from, b = dot });
        }
        selectedDots.Add(dot);
        dot.Select();
    }

    PatternDot GetDotBetween(PatternDot from, PatternDot to)
    {
        int fromRow = from.id / gridColumns, fromCol = from.id % gridColumns;
        int toRow = to.id / gridColumns, toCol = to.id % gridColumns;
        int dRow = toRow - fromRow, dCol = toCol - fromCol;

        if (dRow % 2 != 0 || dCol % 2 != 0) return null;

        int midRow = fromRow + dRow / 2;
        int midCol = fromCol + dCol / 2;
        if (midCol < 0 || midCol >= gridColumns) return null;

        int midId = midRow * gridColumns + midCol;
        if (midId < 0 || midId >= dots.Length) return null;

        return dots[midId];
    }

    // ───────────── Пересечение линий пути ─────────────

    Vector2 GridPos(PatternDot d) => new Vector2(d.id % gridColumns, d.id / gridColumns);

    /// <summary>Пересечёт ли новый отрезок (from→to) какую-либо уже нарисованную линию пути?</summary>
    bool WouldCrossExistingLine(PatternDot from, PatternDot to)
    {
        Vector2 p1 = GridPos(from);
        Vector2 p2 = GridPos(to);

        foreach (var seg in pathSegments)
        {
            Vector2 p3 = GridPos(seg.a);
            Vector2 p4 = GridPos(seg.b);

            if (SegmentsCrossIllegally(p1, p2, p3, p4))
                return true;
        }
        return false;
    }

    /// <summary>
    /// true, если отрезки (p1,p2) и (p3,p4) пересекаются где-либо, кроме
    /// одной общей вершины (обычное продолжение пути из одной точки в другую).
    /// Ловит: пересечение в середине, наложение одного отрезка на другой,
    /// повторное проведение той же самой линии.
    /// </summary>
    static bool SegmentsCrossIllegally(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        bool sameA = Mathf.Approximately(p1.x, p3.x) && Mathf.Approximately(p1.y, p3.y);
        bool sameB = Mathf.Approximately(p1.x, p4.x) && Mathf.Approximately(p1.y, p4.y);
        bool sameC = Mathf.Approximately(p2.x, p3.x) && Mathf.Approximately(p2.y, p3.y);
        bool sameD = Mathf.Approximately(p2.x, p4.x) && Mathf.Approximately(p2.y, p4.y);
        int sharedCount = (sameA ? 1 : 0) + (sameB ? 1 : 0) + (sameC ? 1 : 0) + (sameD ? 1 : 0);

        if (sharedCount >= 2)
            return true; // это тот же самый отрезок, проведённый повторно

        float d1 = Cross(p3, p4, p1);
        float d2 = Cross(p3, p4, p2);
        float d3 = Cross(p1, p2, p3);
        float d4 = Cross(p1, p2, p4);

        bool properIntersect = ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
                                ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        if (properIntersect)
            return true; // пересеклись строго внутри обоих отрезков

        if (sharedCount == 1)
            return false; // касание ровно в одной общей вершине — нормальное продолжение пути

        // Наложение на одной прямой (коллинеарные отрезки), без общей вершины
        if (Mathf.Approximately(d1, 0) && OnSegment(p3, p1, p4)) return true;
        if (Mathf.Approximately(d2, 0) && OnSegment(p3, p2, p4)) return true;
        if (Mathf.Approximately(d3, 0) && OnSegment(p1, p3, p2)) return true;
        if (Mathf.Approximately(d4, 0) && OnSegment(p1, p4, p2)) return true;

        return false;
    }

    static float Cross(Vector2 o, Vector2 a, Vector2 b) =>
        (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

    /// <summary>Предполагая, что p,q,r коллинеарны — лежит ли q на отрезке pr?</summary>
    static bool OnSegment(Vector2 p, Vector2 q, Vector2 r) =>
        Mathf.Min(p.x, r.x) - 0.0001f <= q.x && q.x <= Mathf.Max(p.x, r.x) + 0.0001f &&
        Mathf.Min(p.y, r.y) - 0.0001f <= q.y && q.y <= Mathf.Max(p.y, r.y) + 0.0001f;

    /// <summary>Пересечёт ли новый отрезок (from→to) какую-либо из нарисованных стен-барьеров?</summary>
    bool WouldCrossWall(PatternDot from, PatternDot to)
    {
        if (walls == null || dots == null) return false;

        Vector2 p1 = GridPos(from);
        Vector2 p2 = GridPos(to);

        foreach (var wall in walls)
        {
            if (wall.from < 0 || wall.from >= dots.Length ||
                wall.to < 0 || wall.to >= dots.Length)
                continue;

            PatternDot wa = dots[wall.from];
            PatternDot wb = dots[wall.to];
            if (wa == null || wb == null) continue;

            if (SegmentsCrossIllegally(p1, p2, GridPos(wa), GridPos(wb)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Пересечёт ли новый отрезок (from→to) точку типа Blocker — то есть
    /// пройдёт ли ближе blockerTouchRadius от неё? Сама точка from/to
    /// (являющаяся концом сегмента) не учитывается — этот случай уже
    /// отдельно блокируется прямой проверкой типа точки при выборе.
    /// </summary>
    bool WouldTouchBlocker(PatternDot from, PatternDot to)
    {
        if (dots == null) return false;

        Vector2 p1 = WorldToLinesParentLocal(from.GetComponent<RectTransform>());
        Vector2 p2 = WorldToLinesParentLocal(to.GetComponent<RectTransform>());

        foreach (var d in dots)
        {
            if (d == null || d.type != PatternDot.DotType.Blocker) continue;
            if (d == from || d == to) continue;

            Vector2 bp = WorldToLinesParentLocal(d.GetComponent<RectTransform>());
            if (DistancePointToSegment(bp, p1, p2) <= blockerTouchRadius)
                return true;
        }
        return false;
    }

    // ───────────── Визуализация стен ─────────────

    /// <summary>
    /// Рисует статичную линию-стену прямо между точками для каждой стены из списка walls
    /// (цвет и толщина настраиваются через wallLineColor / wallLineThickness).
    /// Вызывается из Initialize(), т.к. стены — часть геометрии уровня и не должны
    /// пересоздаваться при ClearAll()/ResetPuzzle().
    /// </summary>
    void DrawWalls()
    {
        foreach (var w in wallLines)
            if (w != null) Destroy(w.gameObject);
        wallLines.Clear();

        if (walls == null || walls.Count == 0) return;

        RectTransform prefab = wallLinePrefab != null ? wallLinePrefab : linePrefab;
        if (prefab == null || linesParent == null || dots == null) return;

        var createdFx = new List<NeonLineFx>();

        foreach (var wall in walls)
        {
            if (wall.from < 0 || wall.from >= dots.Length ||
                wall.to < 0 || wall.to >= dots.Length)
            {
                Debug.LogWarning($"PathPuzzleController: некорректная стена {wall.from}-{wall.to}");
                continue;
            }

            PatternDot from = dots[wall.from];
            PatternDot to = dots[wall.to];
            if (from == null || to == null) continue;

            Vector2 start = WorldToLinesParentLocal(from.GetComponent<RectTransform>());
            Vector2 end = WorldToLinesParentLocal(to.GetComponent<RectTransform>());

            RectTransform barrier = Instantiate(prefab, linesParent);
            barrier.name = $"Wall_{wall.from}_{wall.to}";
            barrier.SetAsLastSibling();

            // Явный слой для стен — порядок больше не зависит от порядка детей.
            ApplySortingOrder(barrier, wallSortingOrder);

            SetLineTransform(barrier, start, end, wallLineThickness);

            var fx = barrier.GetComponent<NeonLineFx>();
            if (fx == null) fx = barrier.gameObject.AddComponent<NeonLineFx>();
            // У стен более резкое и быстрое мерцание, чем "дыхание" обычной линии пути —
            // это читается как "под напряжением", а не как спокойный неон маршрута.
            fx.idlePulseSpeed = wallFlickerSpeed;
            fx.idlePulseAmount = wallFlickerAmount;
            fx.Setup(wallLineColor, wallLineGlowPadding, wallLineGlowAlpha);

            wallLines.Add(barrier);
            createdFx.Add(fx);
        }

        if (animateWallsOnInit)
            StartCoroutine(AnimateWallsIn(createdFx));
    }

    /// <summary>
    /// Проигрывает анимацию "рисования" для каждой стены по очереди (а не все сразу),
    /// чтобы появление уровня выглядело как последовательная активация барьеров,
    /// а не мгновенный "снег" из линий.
    /// </summary>
    IEnumerator AnimateWallsIn(List<NeonLineFx> wallFx)
    {
        foreach (var fx in wallFx)
        {
            fx.PlayDrawIn(wallDrawDuration);
            if (wallDrawStagger > 0f)
                yield return new WaitForSeconds(wallDrawStagger);
        }
    }

    // ───────────── Результат ─────────────

    void EvaluatePath()
    {
        bool success = selectedDots.Count > 0 && selectedDots[^1] == endDot;

        if (success)
        {
            SetStatus("✓ Путь пройден!");
            locked = true;
            onWin?.Invoke();
        }
        else
        {
            SetStatus("✗ Не дошли до цели — начните заново");
            ClearAll();
            onFail?.Invoke();
        }
    }

    /// <summary>Вызывать извне (например, кнопкой "Заново") для сброса уровня.</summary>
    public void ResetPuzzle()
    {
        locked = false;
        ClearAll();
        SetStatus("Начните с зелёной точки");
    }

    // ───────────── Линии (как в PatternLock) ─────────────

    void UpdateCursorLine()
    {
        if (selectedDots.Count == 0) { DestroyCursorLine(); return; }

        bool isNew = cursorLine == null;
        if (isNew)
        {
            cursorLine = Instantiate(linePrefab, linesParent);
            ApplySortingOrder(cursorLine, pathSortingOrder);
        }

        cursorLine.SetAsLastSibling();

        Vector2 start = WorldToLinesParentLocal(
            selectedDots[^1].GetComponent<RectTransform>());

        SetLineTransform(cursorLine, start, GetMouseLocalPos(), lineThickness);

        var fx = cursorLine.GetComponent<NeonLineFx>();
        if (fx == null) fx = cursorLine.gameObject.AddComponent<NeonLineFx>();
        if (isNew)
            fx.Setup(lineColor, lineGlowPadding, cursorLineGlowAlpha);
        else
            fx.SyncNow(); // линия тянется за курсором каждый кадр — анимация роста тут не нужна, просто держим свечение в размере
    }

    Vector2 GetMouseLocalPos()
    {
        var pointer = Pointer.current;
        if (pointer == null)
            return Vector2.zero;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            linesParent, pointer.position.ReadValue(), canvasCamera, out Vector2 mouseLocal);
        return mouseLocal;
    }

    bool IsPrimaryPointerPressed()
    {
        var pointer = Pointer.current;
        return pointer != null && pointer.press.isPressed;
    }

    bool WasPrimaryPointerReleasedThisFrame()
    {
        var pointer = Pointer.current;
        return pointer != null && pointer.press.wasReleasedThisFrame;
    }

    /// <summary>
    /// Проверяет, не проходит ли текущая линия (от последней точки до курсора)
    /// достаточно близко от ещё не выбранных точек, и если да — подключает их
    /// по очереди (от ближайшей к последней выбранной), как будто игрок
    /// навёлся на них курсором. Нужно для быстрого протягивания линии мышью.
    /// </summary>
    void CheckAutoCapture()
    {
        if (selectedDots.Count == 0 || dots == null) return;

        Vector2 mouseLocal = GetMouseLocalPos();
        PatternDot last = selectedDots[^1];
        Vector2 lastPos = WorldToLinesParentLocal(last.GetComponent<RectTransform>());

        List<PatternDot> candidates = null;

        foreach (var dot in dots)
        {
            if (dot == null || dot == last || selectedDots.Contains(dot)) continue;
            if (dot.type == PatternDot.DotType.Blocker) continue;

            Vector2 dotPos = WorldToLinesParentLocal(dot.GetComponent<RectTransform>());
            if (DistancePointToSegment(dotPos, lastPos, mouseLocal) > autoCaptureRadius) continue;

            candidates ??= new List<PatternDot>();
            candidates.Add(dot);
        }

        if (candidates == null) return;

        // Сортируем от ближайшей к last точки к самой дальней, чтобы
        // подключение шло в правильном порядке вдоль линии.
        candidates.Sort((a, b) =>
            Vector2.Distance(lastPos, WorldToLinesParentLocal(a.GetComponent<RectTransform>()))
                .CompareTo(Vector2.Distance(lastPos, WorldToLinesParentLocal(b.GetComponent<RectTransform>()))));

        foreach (var dot in candidates)
        {
            if (selectedDots.Contains(dot)) continue; // могла быть подключена как "between" внутри TrySelect
            TrySelect(dot);
        }
    }

    static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lenSq = ab.sqrMagnitude;
        float t = lenSq > 0.0001f ? Vector2.Dot(p - a, ab) / lenSq : 0f;
        t = Mathf.Clamp01(t);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }

    void CreateLine(PatternDot from, PatternDot to)
    {
        RectTransform line = Instantiate(linePrefab, linesParent);
        ApplySortingOrder(line, pathSortingOrder);

        Vector2 start = WorldToLinesParentLocal(from.GetComponent<RectTransform>());
        Vector2 end = WorldToLinesParentLocal(to.GetComponent<RectTransform>());

        // Сначала выставляем финальные позицию/угол/длину как раньше...
        SetLineTransform(line, start, end, lineThickness);

        // ...а затем NeonLineFx "прячет" линию в 0 по длине и анимирует рост
        // до этой финальной длины — снаружи это выглядит как рисование от точки к точке.
        var fx = line.GetComponent<NeonLineFx>();
        if (fx == null) fx = line.gameObject.AddComponent<NeonLineFx>();
        fx.Setup(lineColor, lineGlowPadding, lineGlowAlpha);
        fx.PlayDrawIn(drawAnimationDuration);

        lines.Add(line);
    }

    void SetLineTransform(RectTransform line, Vector2 start, Vector2 end, float thickness = 32f)
    {
        Vector2 dir = end - start;
        line.anchoredPosition = start;
        line.sizeDelta = new Vector2(dir.magnitude, thickness);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
    }

    void ApplySortingOrder(RectTransform target, int sortingOrder)
    {
        if (target == null) return;

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null) canvas = target.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
    }

    public Vector2 WorldToLinesParentLocal(RectTransform rt)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, rt.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            linesParent, screenPoint, canvasCamera, out Vector2 localPoint);
        return localPoint;
    }

    void ClearAll()
    {
        StopErrorFlash();
        DestroyCursorLine();
        foreach (var dot in selectedDots) dot.ResetDot();
        selectedDots.Clear();
        foreach (var line in lines) Destroy(line.gameObject);
        lines.Clear();
        pathSegments.Clear();
    }

    void DestroyCursorLine()
    {
        StopErrorFlash();
        if (cursorLine != null) { Destroy(cursorLine.gameObject); cursorLine = null; }
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    /// <summary>
    /// Единая точка отказа при нарушении правил (стена, запретная точка,
    /// пересечение уже нарисованной линии и т.п.): останавливает рисование,
    /// на короткое время подсвечивает курсорную линию цветом ошибки, а затем
    /// стирает весь пройденный путь целиком — играть дальше можно только
    /// начав заново с зелёной точки.
    /// </summary>
    void Reject(string msg)
    {
        SetStatus(msg);

        // Останавливаем рисование сразу: Update() больше не будет тянуть
        // курсорную линию за мышью и не будет пытаться авто-захватывать
        // соседние точки, пока идёт вспышка и сброс.
        isDrawing = false;
        isRejecting = true;

        if (errorFlashRoutine != null) StopCoroutine(errorFlashRoutine);
        errorFlashRoutine = StartCoroutine(FlashErrorThenReset(cursorLine, msg));
    }

    /// <summary>
    /// Подсвечивает переданную курсорную линию (ту, что тянулась от последней
    /// точки к мыши в момент нарушения) цветом ошибки на errorFlashDuration
    /// секунд, после чего стирает весь нарисованный путь и возвращает статус
    /// "начните заново". Если курсорной линии не было (нарушение случилось
    /// до начала протягивания линии) — просто сразу сбрасываем путь.
    /// </summary>
    IEnumerator FlashErrorThenReset(RectTransform line, string reasonMsg)
    {
        if (line != null)
        {
            var fx = line.GetComponent<NeonLineFx>();
            if (fx != null)
                fx.Setup(errorLineColor, lineGlowPadding, cursorLineGlowAlpha);
        }

        yield return new WaitForSeconds(errorFlashDuration);

        // Обнуляем ссылку ДО ClearAll(): ClearAll вызывает StopErrorFlash(),
        // а мы сейчас как раз внутри этой самой корутины — если оставить
        // ссылку, StopErrorFlash попытается остановить сам себя изнутри
        // собственного выполнения, что лишнее и хрупкое.
        errorFlashRoutine = null;
        isRejecting = false;

        ClearAll();
        SetStatus("Начните с зелёной точки");
    }

    void StopErrorFlash()
    {
        if (errorFlashRoutine != null)
        {
            StopCoroutine(errorFlashRoutine);
            errorFlashRoutine = null;
        }
    }
    bool IsWall(PatternDot a, PatternDot b)
    {
        foreach (var wall in walls)
        {
            if ((wall.from == a.id && wall.to == b.id) ||
                (wall.from == b.id && wall.to == a.id))
                return true;
        }

        return false;
    }
}