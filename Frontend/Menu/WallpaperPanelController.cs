using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WallpaperPanelController : MonoBehaviour
{
    [Serializable]
    private class WallpaperOption
    {
        public string id;
        public string title;
        public Sprite sprite;
        public bool disposeOnClear;
    }

    [Header("References")]
    [SerializeField] private DesktopManager desktopManager;
    [SerializeField] private Transform buttonsContent;
    [SerializeField] private Button wallpaperButtonPrefab;

    [Header("Built-in Wallpapers")]
    [SerializeField] private List<Sprite> builtInWallpapers = new();

    [Header("User Wallpapers")]
    [SerializeField] private bool loadUserWallpapers = true;
    [SerializeField] private string userWallpapersFolderName = "Wallpapers";

    private readonly List<WallpaperOption> options = new();
    private readonly List<Button> spawnedButtons = new();

    private void Awake()
    {
        if (desktopManager == null)
            desktopManager = FindAnyObjectByType<DesktopManager>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        RebuildWallpapers();
    }

    private void OnDestroy()
    {
        ClearGeneratedButtons();
        ClearDynamicOptions();
    }

    public void RebuildWallpapers()
    {
        ClearGeneratedButtons();
        ClearDynamicOptions();

        BuildBuiltInOptions();
        if (loadUserWallpapers)
            BuildUserOptionsFromFolder();

        GenerateButtons();
        EnsureWallpaperAssignedWhenEmpty();
    }

    private void BuildBuiltInOptions()
    {
        for (int i = 0; i < builtInWallpapers.Count; i++)
        {
            Sprite sprite = builtInWallpapers[i];
            if (sprite == null)
                continue;

            options.Add(new WallpaperOption
            {
                id = $"builtin:{i}",
                title = sprite.name,
                sprite = sprite,
                disposeOnClear = false
            });
        }
    }

    private void BuildUserOptionsFromFolder()
    {
        string folderPath = GetUserWallpapersFolderPath();

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string[] files = Directory.GetFiles(folderPath);
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < files.Length; i++)
        {
            string filePath = files[i];
            if (!IsSupportedImageFile(filePath))
                continue;

            Sprite sprite = LoadSpriteFromFile(filePath);
            if (sprite == null)
                continue;

            string fileName = Path.GetFileName(filePath);
            options.Add(new WallpaperOption
            {
                id = $"user:{fileName}",
                title = Path.GetFileNameWithoutExtension(fileName),
                sprite = sprite,
                disposeOnClear = true
            });
        }
    }

    private void GenerateButtons()
    {
        if (buttonsContent == null || wallpaperButtonPrefab == null)
        {
            Debug.LogWarning("[WallpaperPanel] buttonsContent or wallpaperButtonPrefab is not assigned.");
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            WallpaperOption option = options[i];
            int capturedIndex = i;

            Button button = Instantiate(wallpaperButtonPrefab, buttonsContent, false);
            button.name = $"WallpaperButton_{option.title}";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnWallpaperButtonPressed(capturedIndex));

            ApplyPreviewToButton(button, option.sprite, option.title);
            spawnedButtons.Add(button);
        }
    }

    private void OnWallpaperButtonPressed(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= options.Count)
            return;

        WallpaperOption option = options[optionIndex];
        if (option == null || option.sprite == null)
            return;

        desktopManager?.SetDesktopWallpaper(option.sprite);
    }

    private void EnsureWallpaperAssignedWhenEmpty()
    {
        if (desktopManager == null || options.Count == 0)
            return;

        if (desktopManager.GetDesktopWallpaper() == null)
            desktopManager.SetDesktopWallpaper(options[0].sprite);
    }

    private void ApplyPreviewToButton(Button button, Sprite sprite, string title)
    {
        if (button == null || sprite == null)
            return;

        Image previewImage = button.targetGraphic as Image;

        if (previewImage == null)
            previewImage = button.GetComponent<Image>();

        if (previewImage == null)
        {
            Image[] images = button.GetComponentsInChildren<Image>(true);
            if (images.Length > 0)
                previewImage = images[0];
        }

        if (previewImage != null)
        {
            previewImage.sprite = sprite;
            previewImage.preserveAspect = true;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = title;
    }

    private void ClearGeneratedButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            Button button = spawnedButtons[i];
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedButtons.Clear();
    }

    private void ClearDynamicOptions()
    {
        for (int i = 0; i < options.Count; i++)
        {
            WallpaperOption option = options[i];
            if (option == null || !option.disposeOnClear)
                continue;

            if (option.sprite != null)
            {
                Texture2D texture = option.sprite.texture;
                Destroy(option.sprite);
                if (texture != null)
                    Destroy(texture);
            }
        }

        options.Clear();
    }

    private static bool IsSupportedImageFile(string filePath)
    {
        string ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }

    private static Sprite LoadSpriteFromFile(string filePath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(filePath);
            if (bytes == null || bytes.Length == 0)
                return null;

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WallpaperPanel] Failed to load image '{filePath}': {ex.Message}");
            return null;
        }
    }

    private string GetUserWallpapersFolderPath()
    {
        string rootPath = GetGameRootPath();
        return Path.Combine(rootPath, userWallpapersFolderName);
    }

    private static string GetGameRootPath()
    {
        try
        {
            string dataPath = Application.dataPath;
            if (string.IsNullOrWhiteSpace(dataPath))
                return Directory.GetCurrentDirectory();

            DirectoryInfo parent = Directory.GetParent(dataPath);
            if (parent == null)
                return Directory.GetCurrentDirectory();

            // Build: .../GameName_Data -> parent is game folder.
            // Editor: .../Project/Assets -> parent is project root.
            return parent.FullName;
        }
        catch
        {
            return Directory.GetCurrentDirectory();
        }
    }
}
