using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System;

public class GameLoadManager : MonoBehaviour
{
    public static GameLoadManager Instance;

    [SerializeField] private string gameSceneName = "GameScene";

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
            ApplyPendingSave();
        }
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

        pendingSave = null;

        Debug.Log("Сохранение успешно применено");
    }
}