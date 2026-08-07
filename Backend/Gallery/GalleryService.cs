using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Backend-сервис галереи.
/// Отвечает за состояние разблокировки предметов и кэш спрайтов.
/// Не содержит никакой UI-логики — только данные и события.
/// GalleryManager подписывается на события для обновления UI.
/// </summary>
public class GalleryService : MonoBehaviour
{
    public static GalleryService Instance { get; private set; }

    private const string SpritesPath = "Images/Gallery";

    private GameData gameData;
    private readonly Dictionary<string, Sprite> spriteCache = new();

    // ──────────────────────────────────────────────
    // События
    // ──────────────────────────────────────────────

    /// <summary>Вызывается когда предмет галереи разблокирован впервые.</summary>
    public event System.Action<string> OnItemUnlocked;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        gameData = GameManager.Instance.GameData;
    }

    // ──────────────────────────────────────────────
    // Публичный API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Разблокировать предмет. Возвращает true если разблокирован впервые.
    /// При успехе вызывает событие OnItemUnlocked.
    /// </summary>
    public bool UnlockItem(string itemId)
    {
        EnsureData();
        bool changed = gameData.UnlockGalleryItem(itemId);
        if (changed)
            OnItemUnlocked?.Invoke(itemId);
        return changed;
    }

    public bool IsUnlocked(string itemId)
    {
        EnsureData();
        return gameData.IsGalleryItemUnlocked(itemId);
    }

    public List<GalleryImageData> GetAllItems()
    {
        EnsureData();
        return gameData.GetGalleryItems();
    }

    public GalleryImageData GetItem(string itemId)
    {
        EnsureData();
        return gameData.GetGalleryItem(itemId);
    }

    /// <summary>
    /// Загружает спрайт из Resources с кэшированием.
    /// Возвращает null если спрайт не найден.
    /// </summary>
    public Sprite GetSprite(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (spriteCache.TryGetValue(itemId, out Sprite cached)) return cached;

        Sprite loaded = Resources.Load<Sprite>($"{SpritesPath}/{itemId}");
        spriteCache[itemId] = loaded;
        return loaded;
    }

    public void ClearSpriteCache() => spriteCache.Clear();

    // ──────────────────────────────────────────────
    // Приватные вспомогательные
    // ──────────────────────────────────────────────

    private void EnsureData()
    {
        if (gameData == null && GameManager.Instance != null)
            gameData = GameManager.Instance.GameData;
    }
}
