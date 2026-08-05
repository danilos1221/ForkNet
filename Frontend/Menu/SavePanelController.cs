using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Управляет панелью со слотами сохранений/загрузки.
/// Ячейки (SaveSlotUI) создаются заранее в инспекторе — этот скрипт их
/// НЕ создаёт и не удаляет, только инициализирует и обновляет.
/// Сохранение/загрузка выполняются через MenuManager (панель действий над слотом).
/// </summary>
public class SavePanelController : MonoBehaviour
{
    [Tooltip("Заранее созданные в инспекторе ячейки слотов, по порядку (индекс = номер слота)")]
    [SerializeField] private List<SaveSlotUI> slots = new();

    private SaveSystem saveSystem;
    private ISlotActionHost slotActionHost;

    private void Start()
    {
        saveSystem = FindAnyObjectByType<SaveSystem>();
        slotActionHost = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude)
            .OfType<ISlotActionHost>()
            .FirstOrDefault();

        if (saveSystem == null)
        {
            Debug.LogError("SavePanelController: SaveSystem не найден!");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].Initialize(i, saveSystem.GetSlotInfo(i), slotActionHost);
        }
    }

    private void OnEnable()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        if (saveSystem == null) return;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null) continue;
            slots[i].Refresh(saveSystem.GetSlotInfo(i));
        }
    }
}

