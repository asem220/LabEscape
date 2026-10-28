using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Only menu behaviour lives here; the layout is editable in MainMenu.unity.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameScene = "Laborotory";
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private UnityEngine.UI.Slider volumeSlider;
    [SerializeField] private UnityEngine.UI.Toggle fullscreenToggle;
    private bool loading;

    private void Start()
    {
        Time.timeScale = 1f;
        volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 0.8f));
        AudioListener.volume = volumeSlider.value;
        bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        fullscreenToggle.SetIsOnWithoutNotify(fullscreen);
        if (!Application.isEditor) Screen.fullScreen = fullscreen;
        ShowSettings(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && settingsPanel.activeSelf)
            ShowSettings(false);
    }

    public void Play()
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(gameScene))
        {
            Debug.LogError("Add the gameplay scene to Build Settings: " + gameScene);
            return;
        }
        loading = true;
        playButton.interactable = false;
        SceneManager.LoadSceneAsync(gameScene);
    }

    public void ShowSettings(bool show)
    {
        mainPanel.SetActive(!show);
        settingsPanel.SetActive(show);
        EventSystem.current.SetSelectedGameObject(show ? backButton.gameObject : playButton.gameObject);
    }

    public void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
