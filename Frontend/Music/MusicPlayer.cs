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

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Start()
    {
        if (MusicManager.Instance == null)
        {
            Debug.LogError("MusicManager не найден! Добавьте его на GameManager.");
            return;
        }

        playButton.onClick.AddListener(TogglePlay);
        nextButton.onClick.AddListener(PlayNext);
        prevButton.onClick.AddListener(PlayPrevious);
        progressSlider.onValueChanged.AddListener(OnProgressChanged);

        InitializeTrackList();
    }

    private void OnEnable()
    {
        if (MusicManager.Instance == null) return;
        MusicManager.Instance.OnTrackChanged     += HandleTrackChanged;
        MusicManager.Instance.OnPlayStateChanged += HandlePlayStateChanged;

        // Синхронизируем UI с текущим состоянием при открытии окна
        SyncUIWithCurrentState();
    }

    private void OnDisable()
    {
        if (MusicManager.Instance == null) return;
        MusicManager.Instance.OnTrackChanged     -= HandleTrackChanged;
        MusicManager.Instance.OnPlayStateChanged -= HandlePlayStateChanged;
    }
    
    private void InitializeTrackList()
    {
        List<AudioClip> playlist = MusicManager.Instance.GetPlaylist();
        if (playlist.Count == 0) return;
        CreateTrackList(playlist);
        SyncUIWithCurrentState();
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
        // UI обновится через события OnTrackChanged + OnPlayStateChanged
    }
    
    // ──────────────────────────────────────────────
    // Обработчики событий MusicManager
    // ──────────────────────────────────────────────

    private void HandleTrackChanged(AudioClip clip, int index)
    {
        songNameText.text = clip != null ? clip.name : string.Empty;
        UpdateTrackHighlight(index);
    }

    private void HandlePlayStateChanged(bool isPlaying)
    {
        UpdatePlayButtonUI(isPlaying);
    }

    private void SyncUIWithCurrentState()
    {
        if (MusicManager.Instance == null) return;
        AudioClip clip = MusicManager.Instance.GetCurrentClip();
        songNameText.text = clip != null ? clip.name : string.Empty;
        UpdateTrackHighlight(MusicManager.Instance.GetCurrentTrackIndex());
        UpdatePlayButtonUI(MusicManager.Instance.IsPlaying());
    }

    // ──────────────────────────────────────────────
    // Update — только прогресс-бар (плавная анимация)
    // ──────────────────────────────────────────────

    private void Update()
    {
        if (MusicManager.Instance == null) return;
        AudioClip clip = MusicManager.Instance.GetCurrentClip();
        if (clip != null && MusicManager.Instance.IsPlaying())
        {
            progressSlider.value = MusicManager.Instance.GetCurrentTime() / clip.length;
            UpdateTimeDisplay();
        }
    }

    // ──────────────────────────────────────────────
    // Кнопки управления
    // ──────────────────────────────────────────────
    
    public void TogglePlay()
    {
        if (MusicManager.Instance.GetPlaylist().Count == 0) return;
        if (MusicManager.Instance.IsPlaying())
            MusicManager.Instance.Pause();
        else
            MusicManager.Instance.Play();
        // UI обновится через событие OnPlayStateChanged
    }

    public void PlayNext()
    {
        MusicManager.Instance.PlayNext();
        // UI обновится через события OnTrackChanged
    }

    public void PlayPrevious()
    {
        MusicManager.Instance.PlayPrevious();
        // UI обновится через событие OnTrackChanged
    }
    
    private void UpdateTrackHighlight(int currentIndex)
    {
        for (int i = 0; i < trackItems.Count; i++)
        {
            if (i == currentIndex) trackItems[i].SetSelected();
            else                   trackItems[i].SetNormal();
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
    
    private void UpdatePlayButtonUI(bool isPlaying)
    {
        // Здесь можно сменить спрайт кнопки в зависимости от состояния
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
