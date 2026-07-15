using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PatternGridGenerator : MonoBehaviour
{
    [Header("Grid")]
    public int rows = 3;
    public int columns = 10;

    public RectTransform gridParent;
    public GridLayoutGroup gridLayout;
    public PatternDot dotPrefab;

    [Header("Cell")]
    public Vector2 cellSize = new(100, 100);
    public Vector2 spacing = new(10, 10);

    [Header("Level")]
    public int startIndex;
    public int endIndex;
    public List<int> blockerIndices = new();   // точки-блокираторы (работают как стены — их нельзя касаться)

    [Header("Controller")]
    public PathPuzzleController controller;

    [Header("Автозапуск")]
    [Tooltip("Генерировать сетку сразу в Awake(). Отключите, если уровень будет выбран игроком " +
             "через PathPuzzleLevelSelector — тогда сетка появится только после выбора.")]
    public bool generateOnAwake = true;

    private PatternDot[] generatedDots;
    private PathPuzzleLevel runtimeLevel;
    private Coroutine deferredInitRoutine;

    private void Awake()
    {
        Debug.Log($"gridParent.position = {gridParent.position}, rect = {gridParent.rect}, lossyScale = {gridParent.lossyScale}");
        if (generateOnAwake)
            Generate();
    }

    /// <summary>
    /// Переключает генератор на другой уровень и сразу перестраивает сетку.
    /// Вызывается, например, из PathPuzzleLevelSelector при выборе уровня игроком.
    /// </summary>
    public void ApplyLevel(PathPuzzleLevel newLevel)
    {
        runtimeLevel = newLevel;
        Generate();
    }

    /// <summary>
    /// Копирует параметры сетки из level в поля генератора, если ассет назначен.
    /// Если level == null — используются значения, выставленные вручную в инспекторе
    /// (старое поведение, для сцен без системы уровней).
    /// </summary>
    void ApplyLevelAsset(PathPuzzleLevel levelAsset)
    {
        if (levelAsset == null) return;

        rows = levelAsset.rows;
        columns = levelAsset.columns;
        startIndex = levelAsset.startIndex;
        endIndex = levelAsset.endIndex;
        blockerIndices = new List<int>(levelAsset.blockerIndices);
    }

    /// <summary>
    /// Предупреждает в консоли, если один и тот же индекс назначен более чем
    /// одной роли (например, одновременно Start/End и Blocker).
    /// Помогает сразу заметить опечатку в списках, заполненных в инспекторе.
    /// </summary>
    private void ValidateIndices()
    {
        foreach (int i in blockerIndices)
        {
            if (i == startIndex || i == endIndex)
                Debug.LogWarning($"PatternGridGenerator: индекс {i} одновременно в blockerIndices и Start/End.");
        }
    }

    public void Generate()
    {
        ApplyLevelAsset(runtimeLevel);
        ValidateIndices();

        // Настраиваем GridLayout автоматически
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = columns;
        gridLayout.cellSize = cellSize;
        gridLayout.spacing = spacing;

        // Удаляем старые точки
        for (int i = gridParent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(gridParent.GetChild(i).gameObject);
            else
#endif
                Destroy(gridParent.GetChild(i).gameObject);
        }

        generatedDots = new PatternDot[rows * columns];

        for (int i = 0; i < generatedDots.Length; i++)
        {
            PatternDot dot = Instantiate(dotPrefab, gridParent);

            dot.name = $"Dot_{i}";
            dot.id = i;

            if (i == startIndex)
                dot.type = PatternDot.DotType.Start;
            else if (i == endIndex)
                dot.type = PatternDot.DotType.End;
            else if (blockerIndices.Contains(i))
                dot.type = PatternDot.DotType.Blocker;
            else
                dot.type = PatternDot.DotType.Normal;

            dot.ApplyIdleColor();
            generatedDots[i] = dot;
        }

        if (controller != null)
        {
            controller.dots = generatedDots;
            controller.gridColumns = columns;

            // Стены берутся только из выбранного в рантайме level-ассета.
            // Если уровень не выбран — оставляем текущее состояние controller.walls,
            // чтобы не ломать старые сцены с ручной конфигурацией.
            if (runtimeLevel != null)
                controller.walls = new List<PathPuzzleController.Wall>(runtimeLevel.walls);

            // GridLayoutGroup расставляет детей асинхронно (обычно в конце кадра).
            // Если вызвать controller.Initialize() прямо сейчас, точки ещё будут
            // стоять в позиции по умолчанию (угол родителя) — и вся геометрия
            // (в частности, визуализация стен) посчитается неверно, с нулевой длиной.
            // Поэтому принудительно и синхронно пересчитываем layout перед Initialize().
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
            Canvas.ForceUpdateCanvases();

            if (deferredInitRoutine != null)
                StopCoroutine(deferredInitRoutine);
            deferredInitRoutine = StartCoroutine(InitializeControllerNextFrame());
        }
    }

    IEnumerator InitializeControllerNextFrame()
    {
        // При выборе уровня из UI-кнопки (в том же кадре) nested Canvas и layout
        // могут ещё не финализировать порядок отрисовки. На следующем кадре
        // повторно форсим layout/canvas и только потом рисуем стены.
        yield return null;

        if (gridParent != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
            Canvas.ForceUpdateCanvases();
        }

        if (controller != null)
            controller.Initialize();

        deferredInitRoutine = null;
    }
}