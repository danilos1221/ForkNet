using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Менеджер звуковых эффектов (SFX)
/// Воспроизводит звуки при нажатии кнопок, появлении сообщений и т.д.
/// </summary>
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }
    
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private float masterVolume = 1f;
    
    // Кэш загруженных звуков
    private Dictionary<string, AudioClip> soundCache = new Dictionary<string, AudioClip>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Если AudioSource не назначен, создаём его
        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
        }
    }
    
    /// <summary>
    /// Воспроизвести звук по имени
    /// Ищет файл в Resources/Audio/SFX/
    /// </summary>
    public void PlaySound(string soundName, float volume = 1f)
    {
        if (string.IsNullOrEmpty(soundName))
        {
            Debug.LogWarning("Sound name is empty!");
            return;
        }
        
        // Пытаемся загрузить из кэша или Resources
        AudioClip clip = GetOrLoadSound(soundName);
        
        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, volume * masterVolume);
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found in Resources/Audio/SFX/");
        }
    }
    
    /// <summary>
    /// Загрузить звук из Resources или вернуть из кэша
    /// </summary>
    private AudioClip GetOrLoadSound(string soundName)
    {
        if (soundCache.ContainsKey(soundName))
        {
            return soundCache[soundName];
        }
        
        AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{soundName}");
        if (clip != null)
        {
            soundCache[soundName] = clip;
        }
        
        return clip;
    }
    
    /// <summary>
    /// Установить общую громкость всех эффектов
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        sfxAudioSource.volume = masterVolume;
    }
    
    /// <summary>
    /// Получить текущую громкость
    /// </summary>
    public float GetMasterVolume()
    {
        return masterVolume;
    }
    
    /// <summary>
    /// Остановить воспроизведение
    /// </summary>
    public void Stop()
    {
        sfxAudioSource.Stop();
    }
}
