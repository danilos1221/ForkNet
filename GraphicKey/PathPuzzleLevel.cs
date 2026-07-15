using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Данные одного уровня мини-игры "путь по точкам": размер сетки, старт/финиш,
/// точки-блокираторы и стены. Хранится как ассет в проекте — создаётся через
/// ПКМ в Project → Create → Path Puzzle → Level, редактируется в инспекторе
/// как обычный объект, без необходимости лезть в сцену.
///
/// Назначается либо напрямую в PatternGridGenerator.level (один фиксированный
/// уровень), либо через PathPuzzleLevelSelector — тогда игрок выбирает уровень
/// из списка при запуске, и генератор перестраивает сетку под него в рантайме
/// (см. PatternGridGenerator.ApplyLevel).
/// </summary>
[CreateAssetMenu(fileName = "NewPathPuzzleLevel", menuName = "Path Puzzle/Level")]
public class PathPuzzleLevel : ScriptableObject
{
    [Header("Инфо")]
    [Tooltip("Отображаемое имя уровня — используется, например, в списке выбора уровней.")]
    public string levelName = "Level";

    [Header("Сетка")]
    public int rows = 3;
    public int columns = 10;

    [Header("Старт / Финиш")]
    [Tooltip("Индекс точки-старта (0-based, слева-направо, сверху-вниз: id = row * columns + col).")]
    public int startIndex;
    [Tooltip("Индекс точки-финиша.")]
    public int endIndex;

    [Header("Блокираторы")]
    [Tooltip("Индексы точек-блокираторов (работают как стены — их нельзя касаться и нельзя проходить рядом с ними ближе blockerTouchRadius).")]
    public List<int> blockerIndices = new();

    [Header("Стены")]
    [Tooltip("Стены между парами точек — заполняются индексами, как in PathPuzzleController.walls.")]
    public List<PathPuzzleController.Wall> walls = new();

#if UNITY_EDITOR
    /// <summary>
    /// Быстрая проверка данных уровня прямо в инспекторе — не заменяет
    /// PatternGridGenerator.ValidateIndices() (та проверяет актуальный
    /// blockerIndices генератора после применения уровня), но помогает
    /// заметить опечатку сразу при заполнении ассета.
    /// </summary>
    void OnValidate()
    {
        rows = Mathf.Max(1, rows);
        columns = Mathf.Max(1, columns);

        int cellCount = rows * columns;
        if (startIndex < 0 || startIndex >= cellCount)
            Debug.LogWarning($"PathPuzzleLevel '{levelName}': startIndex={startIndex} вне диапазона сетки {rows}x{columns}.");
        if (endIndex < 0 || endIndex >= cellCount)
            Debug.LogWarning($"PathPuzzleLevel '{levelName}': endIndex={endIndex} вне диапазона сетки {rows}x{columns}.");
    }
#endif
}
