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
    }
    
    private void Start()
    {

    }
    
    public void UnlockGalleryItem(string itemId)
    {
        gameData.UnlockGalleryItem(itemId);
    }
}
