/// <summary>
/// Реализуется любым UI-менеджером, который умеет показывать панель действий
/// (Сохранить / Загрузить / Удалить) для выбранного слота сохранения.
///
/// Нужен, чтобы SaveSlotUI/SavePanelController не были жёстко привязаны к
/// конкретному классу MenuManager и могли одинаково работать как в игровой
/// сцене (MenuManager), так и в главном меню (MainMenuManager).
/// </summary>
public interface ISlotActionHost
{
    void OpenSlotActionPanel(int slotIndex);
}
