using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент для воспроизведения звука при нажатии кнопки
/// Добавьте этот скрипт на Button объект
/// </summary>
[RequireComponent(typeof(Button))]
public class SFXButton : MonoBehaviour
{
    [SerializeField] private string clickSoundName = "button_click";  // Имя звука без расширения
    [SerializeField] private float volume = 1f;  // Громкость от 0 до 1
    
    private Button button;
    
    private void Start()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlayClickSound);
        }
    }
    
    private void PlayClickSound()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlaySound(clickSoundName, volume);
        }
    }
    
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
}
