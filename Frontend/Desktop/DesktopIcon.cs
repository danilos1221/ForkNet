using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DesktopIcon : MonoBehaviour
{
    public enum AppType
    {
        Chat,
        Gallery,
        Music,
        Settings,
        Terminal,
        GraphicKey,
        Sudoku
    }
    
    [SerializeField] private AppType appType;
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image iconImage;
    
    private DesktopManager desktopManager;
    
    private void Start()
    {
        desktopManager = GetComponentInParent<DesktopManager>();
        button.onClick.AddListener(OnIconClicked);
    }
    
    public void SetupIcon(AppType type, string label, Sprite icon)
    {
        appType = type;
        labelText.text = label;
        iconImage.sprite = icon;
    }
    
    private void OnIconClicked()
    {
        desktopManager.OpenApp(appType);
    }
}
