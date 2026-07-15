using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Управляет UI музыкального плеера
/// Фактическое воспроизведение музыки управляется MusicManager на GameManager
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button playButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private TextMeshProUGUI songNameText;
    [SerializeField] private TextMeshProUGUI timeText;
    
    [SerializeField] private Transform trackListContainer;
    [SerializeField] private GameObject trackItemPrefab;
    
    private List<MusicTrackItem> trackItems = new List<MusicTrackItem>();
    
    private void Start()
    {
        // Проверяем наличие MusicManager
        Debug.Log("Инициализация MusicPlayer...");
        if (MusicManager.Instance == null)
        {
            Debug.LogError("MusicManager не найден! Добавьте его на GameManager.");
            return;
        }
        
        InitializeUI();
        
        playButton.onClick.AddListener(TogglePlay);
        nextButton.onClick.AddListener(PlayNext);
        prevButton.onClick.AddListener(PlayPrevious);
        progressSlider.onValueChanged.AddListener(OnProgressChanged);
    }
    
    private void InitializeUI()
    {
        List<AudioClip> playlist = MusicManager.Instance.GetPlaylist();
        Debug.Log($"Инициализация UI музыкального плеера с {playlist.Count} треками.");
        if (playlist.Count > 0)
        {
            CreateTrackList(playlist);
            UpdateTrackDisplay();
        }
    }
    
    private void CreateTrackList(List<AudioClip> playlist)
    {
        // Очищаем старый список
        foreach (Transform child in trackListContainer)
        {
            Destroy(child.gameObject);
        }
        trackItems.Clear();
        
        // Создаём новые элементы
        for (int i = 0; i < playlist.Count; i++)
        {
            GameObject itemGO = Instantiate(trackItemPrefab, trackListContainer);
            MusicTrackItem trackItem = itemGO.GetComponent<MusicTrackItem>();
            
            if (trackItem != null)
            {
                trackItem.SetupTrack(i, playlist[i].name, SelectTrack);
                trackItems.Add(trackItem);
            }
        }
        
        // Выделяем первый трек
        if (trackItems.Count > 0)
        {
            trackItems[0].SetSelected();
        }
    }
    
    private void SelectTrack(int index)
    {
        MusicManager.Instance.PlayTrack(index);
        UpdateTrackDisplay();
        UpdateTrackHighlight();
        UpdatePlayButtonUI();
    }
    
    private void Update()
    {
        // Обновляем прогресс
        AudioClip clip = MusicManager.Instance.GetCurrentClip();
        if (clip != null && MusicManager.Instance.IsPlaying())
        {
            progressSlider.value = MusicManager.Instance.GetCurrentTime() / clip.length;
            UpdateTimeDisplay();
        }
    }
    
    public void TogglePlay()
    {
        if (MusicManager.Instance.GetPlaylist().Count == 0) return;
        
        if (MusicManager.Instance.IsPlaying())
        {
            MusicManager.Instance.Pause();
        }
        else
        {
            MusicManager.Instance.Play();
            UpdateTrackHighlight();
        }
        
        UpdatePlayButtonUI();
    }
    
    public void PlayNext()
    {
        MusicManager.Instance.PlayNext();
        UpdateTrackDisplay();
        UpdateTrackHighlight();
        UpdatePlayButtonUI();
    }
    
    public void PlayPrevious()
    {
        MusicManager.Instance.PlayPrevious();
        UpdateTrackDisplay();
        UpdateTrackHighlight();
        UpdatePlayButtonUI();
    }
    
    private void UpdateTrackHighlight()
    {
        int currentIndex = MusicManager.Instance.GetCurrentTrackIndex();
        
        // Обновляем выделение всех треков
        for (int i = 0; i < trackItems.Count; i++)
        {
            if (i == currentIndex)
            {
                trackItems[i].SetSelected();
            }
            else
            {
                trackItems[i].SetNormal();
            }
        }
    }
    
    private void UpdateTrackDisplay()
    {
        AudioClip clip = MusicManager.Instance.GetCurrentClip();
        if (clip != null)
        {
            songNameText.text = clip.name;
        }
    }
    
    private void UpdateTimeDisplay()
    {
        AudioClip clip = MusicManager.Instance.GetCurrentClip();
        if (clip != null)
        {
            float currentTime = MusicManager.Instance.GetCurrentTime();
            float duration = clip.length;
            
            string current = FormatTime(currentTime);
            string total = FormatTime(duration);
            
            timeText.text = $"{current} / {total}";
        }
    }
    
    private void UpdatePlayButtonUI()
    {
        // Можно изменить спрайт кнопки в зависимости от состояния
    }
    
    private void OnProgressChanged(float value)
    {
        AudioClip clip = MusicManager.Instance.GetCurrentClip();
        if (clip != null)
        {
            MusicManager.Instance.SetCurrentTime(value * clip.length);
        }
    }
    
    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }
}
