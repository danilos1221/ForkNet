using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

public class GameLoadManager : MonoBehaviour
{
    public static GameLoadManager Instance;

    [SerializeField] private string gameSceneName = "Game";

    private GameSave pendingSave;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void LoadGame(GameSave save)
    {
        pendingSave = save;

        if (SceneManager.GetActiveScene().name == gameSceneName)
        {
            ApplyPendingSave();
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            // sceneLoaded вызывается ПОСЛЕ Awake(), но ДО Start() объектов новой сцены —
            // SaveSystem.gameData инициализируется в Start(), поэтому применяем
            // сохранение через кадр, когда Start() уже гарантированно отработал.
            StartCoroutine(ApplyPendingSaveNextFrame());
        }
    }

    private IEnumerator ApplyPendingSaveNextFrame()
    {
        yield return null;
        ApplyPendingSave();
    }

    private void ApplyPendingSave()
    {
        if (pendingSave == null)
            return;

        SaveSystem saveSystem = FindAnyObjectByType<SaveSystem>();

        if (saveSystem == null)
        {
            Debug.LogError("SaveSystem не найден!");
            return;
        }

        saveSystem.ApplyLoadedSave(pendingSave);

        // В сцене может существовать несколько GalleryManager (например, случайный компонент
        // на другом окне). Обновляем все активные экземпляры, чтобы не зависеть от того,
        // какой именно вернёт FindAnyObjectByType.
        GalleryManager[] galleryManagers = FindObjectsByType<GalleryManager>();

        for (int i = 0; i < galleryManagers.Length; i++)
        {
            GalleryManager manager = galleryManagers[i];
            if (manager == null || !manager.isActiveAndEnabled)
                continue;

            manager.RefreshAfterLoad();
        }

        pendingSave = null;

        Debug.Log("Сохранение успешно применено");
    }
}