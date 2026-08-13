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

    private bool wasPlaying;
    private bool isPaused;

    // ──────────────────────────────────────────────
    // События
    // ──────────────────────────────────────────────

    /// <summary>
    /// Трек сменился. Параметры: новый AudioClip, индекс трека.
    /// </summary>
    public event System.Action<AudioClip, int> OnTrackChanged;

    /// <summary>
    /// Состояние воспроизведения изменилось.
    /// </summary>
    public event System.Action<bool> OnPlayStateChanged;


    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

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

        // Если AudioSource не назначен, создаём его
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        LoadPlaylist();
    }


    private void LoadPlaylist()
    {
        // Загружаем все аудиоклипы из Resources/Audio
        AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio");

        playlist.AddRange(clips);
    }


    // ──────────────────────────────────────────────
    // Playlist
    // ──────────────────────────────────────────────

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
        if (index < 0 || index >= playlist.Count)
            return;

        currentTrackIndex = index;
        audioSource.clip = playlist[currentTrackIndex];
        isPaused = false;
    }


    // ──────────────────────────────────────────────
    // Playback
    // ──────────────────────────────────────────────

    public void PlayTrack(int index)
    {
        if (index < 0 || index >= playlist.Count)
            return;

        currentTrackIndex = index;

        audioSource.clip = playlist[currentTrackIndex];
        audioSource.Play();

        isPaused = false;

        OnTrackChanged?.Invoke(
            audioSource.clip,
            currentTrackIndex
        );

        OnPlayStateChanged?.Invoke(true);
    }


    public void Play()
    {
        if (playlist.Count == 0)
            return;

        // Если клип ещё не установлен
        if (audioSource.clip == null)
        {
            audioSource.clip = playlist[currentTrackIndex];
        }

        // Продолжает с текущей позиции после Pause()
        audioSource.UnPause();

        isPaused = false;

        OnPlayStateChanged?.Invoke(true);
    }


    public void Pause()
    {
        if (audioSource.clip == null)
            return;

        audioSource.Pause();

        isPaused = true;

        OnPlayStateChanged?.Invoke(false);
    }


    public void Stop()
    {
        audioSource.Stop();

        isPaused = false;
        wasPlaying = false;

        OnPlayStateChanged?.Invoke(false);
    }


    // ──────────────────────────────────────────────
    // Next / Previous
    // ──────────────────────────────────────────────

    public void PlayNext()
    {
        if (playlist.Count == 0)
            return;

        currentTrackIndex =
            (currentTrackIndex + 1) % playlist.Count;

        PlayTrack(currentTrackIndex);
    }


    public void PlayPrevious()
    {
        if (playlist.Count == 0)
            return;

        currentTrackIndex =
            (currentTrackIndex - 1 + playlist.Count) % playlist.Count;

        PlayTrack(currentTrackIndex);
    }


    // ──────────────────────────────────────────────
    // State
    // ──────────────────────────────────────────────

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
            audioSource.time = Mathf.Clamp(
                time,
                0,
                audioSource.clip.length
            );
        }
    }


    public float GetDuration()
    {
        if (audioSource.clip == null)
            return 0f;

        return audioSource.clip.length;
    }


    public AudioClip GetCurrentClip()
    {
        return audioSource.clip;
    }


    // ──────────────────────────────────────────────
    // Auto next track
    // ──────────────────────────────────────────────

    private void Update()
    {
        // Если трек только что играл,
        // но AudioSource перестал играть,
        // проверяем, действительно ли он закончился.

        if (wasPlaying &&
            !audioSource.isPlaying &&
            !isPaused &&
            audioSource.clip != null &&
            audioSource.time >= audioSource.clip.length - 0.1f)
        {
            PlayNext();
        }

        wasPlaying = audioSource.isPlaying;
    }
}

