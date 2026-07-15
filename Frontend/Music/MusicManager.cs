using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет музыкой на уровне приложения (AudioSource на GameManager)
/// Музыка продолжает играть, даже если окно приложения скрыто
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    
    [SerializeField] private AudioSource audioSource;
    
    private List<AudioClip> playlist = new List<AudioClip>();
    private int currentTrackIndex = 0;

    
    private void Awake()
    {
        // Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Если AudioSource не назначен, создаем его
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        LoadPlaylist();
    }
    
    private void Start()
    {
        //LoadPlaylist();
    }
    
    private void LoadPlaylist()
    {
        // Загружаем все аудиоклипы из папки Resources/Audio
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");
        playlist.AddRange(clips);
        Debug.Log($"Загружено {playlist.Count} треков в плейлист.");
    }
    
    public List<AudioClip> GetPlaylist()
    {
        return playlist;
    }
    
    public int GetCurrentTrackIndex()
    {
        return currentTrackIndex;
    }
    
    public void SetCurrentTrack(int index)
    {
        if (index < 0 || index >= playlist.Count) return;
        
        currentTrackIndex = index;
        audioSource.clip = playlist[currentTrackIndex];
    }
    
    public void PlayTrack(int index)
    {
        if (index < 0 || index >= playlist.Count) return;
        
        currentTrackIndex = index;
        audioSource.clip = playlist[currentTrackIndex];
        audioSource.Play();
    }
    
    public void Play()
    {
        if (playlist.Count == 0) return;
        
        if (audioSource.clip == null)
        {
            audioSource.clip = playlist[currentTrackIndex];
        }
        
        audioSource.Play();
    }
    
    public void Pause()
    {
        audioSource.Pause();
    }
    
    public void Stop()
    {
        audioSource.Stop();
    }
    
    public void PlayNext()
    {
        currentTrackIndex = (currentTrackIndex + 1) % playlist.Count;
        PlayTrack(currentTrackIndex);
    }
    
    public void PlayPrevious()
    {
        currentTrackIndex = (currentTrackIndex - 1 + playlist.Count) % playlist.Count;
        PlayTrack(currentTrackIndex);
    }
    
    public bool IsPlaying()
    {
        return audioSource.isPlaying;
    }
    
    public float GetCurrentTime()
    {
        return audioSource.time;
    }
    
    public void SetCurrentTime(float time)
    {
        if (audioSource.clip != null)
        {
            audioSource.time = Mathf.Clamp(time, 0, audioSource.clip.length);
        }
    }
    
    public float GetDuration()
    {
        if (audioSource.clip == null) return 0f;
        return audioSource.clip.length;
    }
    
    public AudioClip GetCurrentClip()
    {
        return audioSource.clip;
    }
    
    private void Update()
    {
        // Если трек закончился, переходим на следующий
        if (audioSource.isPlaying && !audioSource.isPlaying && audioSource.clip != null)
        {
            PlayNext();
        }
    }
}
