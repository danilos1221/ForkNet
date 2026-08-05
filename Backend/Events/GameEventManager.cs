using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Игровой пайплайн событий: описывает КОГДА и ПРИ КАКИХ условиях приходят сообщения/запускаются чаты.
/// Сам PhoneChatController ничего не знает о причинах доставки сообщений — он только
/// умеет отображать/скрывать их и хранить непрочитанные. Вся логика "что и когда произошло"
/// живёт здесь, чтобы её было легко расширять (новые триггеры, квесты, флаги и т.д.).
/// </summary>
public class GameEventManager : MonoBehaviour
{
    public static GameEventManager Instance { get; private set; }

    [SerializeField] private ScenarioManager scenarioManager;

    [Header("События (условие → действие)")]
    [SerializeField] private List<GameEvent> events = new();

    private GameData gameData;

    private void Awake()
    {
        Instance = this;

        if (scenarioManager == null)
            scenarioManager = FindAnyObjectByType<ScenarioManager>();
    }

    private void Start()
    {
        gameData = GameManager.Instance.GameData ?? new GameData();

        foreach (GameEvent evt in events)
        {
            if (evt.triggerType == GameEventTriggerType.OnGameStart)
                StartCoroutine(RunDelayed(evt));
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ──────────────────────────────────────────────
    // Публичный API — вызывается извне (ScenarioManager, квесты, минигры и т.д.)
    // ──────────────────────────────────────────────

    /// <summary>Установить именованный флаг и запустить все события, ожидающие его.</summary>
    public void SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag) || gameData == null) return;

        gameData.SetStoryFlag(flag);

        foreach (GameEvent evt in events)
        {
            if (evt.triggerType == GameEventTriggerType.OnFlagSet && evt.requiredFlag == flag)
                StartCoroutine(RunDelayed(evt));
        }
    }

    /// <summary>Вызывается, когда сценарий чата дошёл до конца (isCompleted == true).</summary>
    public void NotifyChatCompleted(string chatId)
    {
        if (string.IsNullOrWhiteSpace(chatId)) return;

        foreach (GameEvent evt in events)
        {
            if (evt.triggerType == GameEventTriggerType.OnChatCompleted && evt.requiredChatId == chatId)
                StartCoroutine(RunDelayed(evt));
        }
    }

    // ──────────────────────────────────────────────
    // Внутренняя логика выполнения
    // ──────────────────────────────────────────────

    private IEnumerator RunDelayed(GameEvent evt)
    {
        if (HasAlreadyFired(evt))
            yield break;

        if (evt.delay > 0f)
            yield return new WaitForSeconds(evt.delay);

        Execute(evt);
    }

    private bool HasAlreadyFired(GameEvent evt) =>
        evt.oneTime && !string.IsNullOrEmpty(evt.eventId) && gameData != null && gameData.HasFiredEvent(evt.eventId);

    private void Execute(GameEvent evt)
    {
        if (HasAlreadyFired(evt))
            return;

        GameEventAction action = evt.action;
        if (action != null && !string.IsNullOrWhiteSpace(action.targetChatId))
        {
            scenarioManager?.DeliverBackgroundMessages(action.targetChatId);

            if (!string.IsNullOrWhiteSpace(action.setFlagOnComplete))
                SetFlag(action.setFlagOnComplete);
        }

        if (evt.oneTime && !string.IsNullOrEmpty(evt.eventId))
            gameData?.MarkEventFired(evt.eventId);
    }
}
