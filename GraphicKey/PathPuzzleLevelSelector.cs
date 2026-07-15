using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Экран выбора уровня для мини-игры "путь по точкам". Строит одну кнопку
/// на каждый уровень из списка levels; при нажатии просит PatternGridGenerator
/// перестроить сетку под выбранный уровень (ApplyLevel) и скрывает панель
/// выбора (если она назначена).
///
/// Использование:
/// 1) Создайте нужные ассеты уровней: ПКМ в Project → Create → Path Puzzle → Level,
///    заполните rows/columns/startIndex/endIndex/blockerIndices/walls.
/// 2) На PatternGridGenerator выключите generateOnAwake (чтобы сетка не появлялась
///    раньше выбора) — либо оставьте включённым, если хотите, чтобы какой-то
///    уровень был виден сразу, а этот компонент лишь позволял его сменить.
/// 3) На этом компоненте назначьте generator, levelButtonPrefab, buttonsParent
///    и (опционально) selectorPanel — панель с самим экраном выбора, которая
///    скрывается после того, как игрок нажал на уровень.
/// </summary>
public class PathPuzzleLevelSelector : MonoBehaviour
{
    [Header("Уровни")]
    [Tooltip("Список доступных уровней — перетащите сюда нужные ассеты PathPuzzleLevel.")]
    public List<PathPuzzleLevel> levels = new();

    [Header("Ссылки")]
    public PatternGridGenerator generator;

    [Header("Навигация (телефон)")]
    [Tooltip("Ссылка на PhoneNavigationManager сцены. Если назначена вместе с gameScreen — " +
             "при выборе уровня экран переключается через неё (OpenScreen), что кладёт текущий " +
             "экран в историю и позволяет вернуться назад системной кнопкой back.")]
    public PhoneNavigationManager navManager;
    [Tooltip("Экран самой игры (например, BackgroundGame) — на него переключаемся при выборе уровня, " +
             "если назначены и navManager, и этот объект.")]
    public GameObject gameScreen;

    [Header("UI")]
    [Tooltip("Префаб кнопки одного уровня. Должен содержать компонент Button; если внутри есть " +
             "TextMeshProUGUI — в него будет подставлено имя уровня.")]
    public Button levelButtonPrefab;
    [Tooltip("Родитель для кнопок уровней (например, Content внутри ScrollView).")]
    public RectTransform buttonsParent;
    [Tooltip("Сам экран выбора уровня — если назначен, скрывается сразу после выбора игроком.")]
    public GameObject selectorPanel;

    [Header("Автозагрузка")]
    [Tooltip("Индекс уровня в списке levels, который нужно загрузить сразу при старте, " +
             "минуя показ экрана выбора. -1 — не грузить автоматически, ждать выбора игрока.")]
    public int autoLoadLevelIndex = -1;

    void Start()
    {
        BuildButtons();

        if (autoLoadLevelIndex >= 0 && autoLoadLevelIndex < levels.Count)
            SelectLevel(autoLoadLevelIndex);
    }

    void BuildButtons()
    {
        if (levelButtonPrefab == null || buttonsParent == null) return;

        for (int i = buttonsParent.childCount - 1; i >= 0; i--)
            Destroy(buttonsParent.GetChild(i).gameObject);

        for (int i = 0; i < levels.Count; i++)
        {
            PathPuzzleLevel level = levels[i];

            Button button = Instantiate(levelButtonPrefab, buttonsParent);
            button.name = level != null ? $"LevelButton_{level.levelName}" : $"LevelButton_{i}";

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = level != null ? level.levelName : $"Уровень {i + 1}";

            // Захватываем индекс в локальную переменную — если использовать
            // напрямую переменную цикла i, все кнопки замкнутся на её финальное
            // значение после цикла (классическая ловушка с closures в C#).
            int capturedIndex = i;
            button.onClick.AddListener(() => SelectLevel(capturedIndex));
        }
    }

    public void SelectLevel(int index)
    {
        if (index < 0 || index >= levels.Count)
        {
            Debug.LogWarning($"PathPuzzleLevelSelector: индекс уровня {index} вне диапазона (0..{levels.Count - 1}).");
            return;
        }

        SelectLevel(levels[index]);
    }

    public void SelectLevel(PathPuzzleLevel level)
    {
        if (generator == null)
        {
            Debug.LogWarning("PathPuzzleLevelSelector: не назначен generator.");
            return;
        }
        if (level == null)
        {
            Debug.LogWarning("PathPuzzleLevelSelector: попытка выбрать пустой (null) уровень.");
            return;
        }

        // Важно: сначала переключаем экран (активируем BackgroundGame), и только
        // потом вызываем ApplyLevel() → Generate(). GridLayoutGroup/LayoutRebuilder
        // считают геометрию корректно только для активной иерархии — если применить
        // уровень раньше, размеры точек посчитаются на неактивном объекте и будут нулевыми.
        if (navManager != null && gameScreen != null)
        {
            navManager.OpenScreen(gameScreen);
        }
        else if (selectorPanel != null)
        {
            // Старое поведение — для сцен без PhoneNavigationManager.
            selectorPanel.SetActive(false);
        }

        generator.ApplyLevel(level);
    }

    /// <summary>Показать экран выбора уровня снова — например, по кнопке "Другой уровень" в самой игре.</summary>
    public void ShowSelector()
    {
        if (selectorPanel != null)
            selectorPanel.SetActive(true);
    }
}