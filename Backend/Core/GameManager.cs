using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ChatDatabase ChatDatabase { get; private set; }
    public static GameManager Instance { get; private set; }
    public string nickname = "Player";
    
    [SerializeField] private GameData gameData;
    
    public GameData GameData => gameData;
    
    private void Awake()
    {
        if (gameData == null)
        {
            gameData = new GameData();
        }
        ChatDatabase = ChatDatabase.Load();
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // DayFlowManager должен приходить из сцены Game, чтобы использовались
        // inspector-настройки (requiredChatsToEndDay и параметры перехода дня).
        // Автосоздание здесь может породить "пустой" singleton с дефолтами,
        // который переживёт загрузку сцены и затрет сценный экземпляр.
    }
    
    private void Start()
    {

    }
    
    public void UnlockGalleryItem(string itemId)
    {
        gameData.UnlockGalleryItem(itemId);
    }

    public int GetCurrentDay()
    {
        return gameData != null ? gameData.currentDay : 1;
    }

    public bool TryEndCurrentDay()
    {
        return DayFlowManager.Instance != null && DayFlowManager.Instance.TryEndDay();
    }
}
