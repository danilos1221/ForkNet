using UnityEngine;

/// <summary>
/// Тип условия, при котором срабатывает игровое событие.
/// Расширяйте этот enum по мере появления новых видов условий (квесты, время суток и т.д.).
/// </summary>
public enum GameEventTriggerType
{
    /// <summary>Срабатывает один раз при старте сцены (с задержкой delay).</summary>
    OnGameStart,

    /// <summary>Срабатывает, когда установлен флаг requiredFlag (через GameEventManager.SetFlag).</summary>
    OnFlagSet,

    /// <summary>Срабатывает, когда диалог чата requiredChatId помечен завершённым.</summary>
    OnChatCompleted,
}

/// <summary>Что делает событие при срабатывании.</summary>
[System.Serializable]
public class GameEventAction
{
    [Tooltip("В каком чате доставить сообщение(я). Текст берётся из сценария чата (.txt) — здесь ничего вводить не нужно.")]
    public string targetChatId;

    [Tooltip("Необязательно: флаг, который выставляется сразу после срабатывания события")]
    public string setFlagOnComplete;
}

/// <summary>
/// Одно событие пайплайна: условие + действие.
/// Настраивается в инспекторе GameEventManager.
/// </summary>
[System.Serializable]
public class GameEvent
{
    [Tooltip("Уникальный id события, нужен чтобы одноразовые события не срабатывали повторно после загрузки сохранения")]
    public string eventId;

    public GameEventTriggerType triggerType = GameEventTriggerType.OnGameStart;

    [Tooltip("Задержка в секундах перед выполнением действия после выполнения условия")]
    public float delay = 0f;

    [Tooltip("Используется для триггера OnFlagSet")]
    public string requiredFlag;

    [Tooltip("Используется для триггера OnChatCompleted")]
    public string requiredChatId;

    [Tooltip("Если true — событие срабатывает только один раз за игру (сохраняется в GameData)")]
    public bool oneTime = true;

    public GameEventAction action;
}
