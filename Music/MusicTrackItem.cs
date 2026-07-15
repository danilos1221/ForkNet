using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MusicTrackItem : MonoBehaviour
{
    [SerializeField] private Button trackButton;
    [SerializeField] private TextMeshProUGUI trackNameText;
    [SerializeField] private Image backgroundImage;
    
    [SerializeField] private Color selectedColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    private int trackIndex;
    private System.Action<int> onSelectCallback;
    
    private void Start()
    {
        trackButton.onClick.AddListener(OnClicked);
    }
    
    public void SetupTrack(int index, string trackName, System.Action<int> onSelect)
    {
        trackIndex = index;
        trackNameText.text = trackName;
        onSelectCallback = onSelect;
        
        // По умолчанию нормальный цвет
        SetNormal();
    }
    
    public void SetSelected()
    {
        backgroundImage.color = selectedColor;
    }
    
    public void SetNormal()
    {
        backgroundImage.color = normalColor;
    }
    
    private void OnClicked()
    {
        onSelectCallback?.Invoke(trackIndex);
    }
}
