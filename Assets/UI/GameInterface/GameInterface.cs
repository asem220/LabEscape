using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class GameInterface : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private PlayerEnergy energy;
    [SerializeField] private UnityEngine.UI.Image foodFill;
    [SerializeField] private UnityEngine.UI.Image magicFill;
    [SerializeField] private TextMeshProUGUI foodText;
    [SerializeField] private TextMeshProUGUI magicText;
    [SerializeField] private TextMeshProUGUI foodHint;
    [Header("Pause")]
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private UnityEngine.UI.RawImage blurredBackground;
    [SerializeField] private UnityEngine.UI.Button resumeButton;
    [SerializeField] private UnityEngine.UI.Button settingsBackButton;
    [SerializeField] private UnityEngine.UI.Slider volumeSlider;
    [SerializeField] private UnityEngine.UI.Toggle fullscreenToggle;
    [SerializeField] private Material blurMaterial;
    private RenderTexture pauseTexture;
    private bool paused;
    private bool capturing;
    private bool leaving;
    private InputAction pauseAction;
    private InputAction eatAction;

    private void Awake()
    {
        pauseAction = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");
        pauseAction.performed += _ => TogglePause();
        eatAction = new InputAction("Eat", InputActionType.Button, "<Keyboard>/e");
        eatAction.performed += _ => EatNearbyRation();
    }

    private void OnEnable() { pauseAction.Enable(); eatAction.Enable(); }
    private void OnDisable() { pauseAction.Disable(); eatAction.Disable(); }

    private void EatNearbyRation()
    {
        if (paused) return;
        foreach (var ration in FoodPickup.Active)
            if (ration != null && ration.TryEat()) return;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        AudioListener.volume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        fullscreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
        pauseOverlay.SetActive(false);
    }

    private void Update()
    {
        foodFill.fillAmount = energy.FoodFraction;
        magicFill.fillAmount = energy.MagicFraction;
        foodText.text = "СЫТОСТЬ  " + Mathf.CeilToInt(energy.Food) + " / 100";
        magicText.text = "МАГИЯ    " + Mathf.CeilToInt(energy.Magic) + " / 100";
        bool nearby = false;
        foreach (var ration in FoodPickup.Active)
            if (ration != null && ration.IsNearby) { nearby = true; break; }
        foodHint.text = nearby ? "E — съесть паёк  (+35 еда / +45 магия)" : "Еда восстанавливает сытость и магию";
    }

    private void TogglePause()
    {
        if (capturing || leaving) return;
        if (!paused) Pause();
        else if (settingsPanel.activeSelf) ShowSettings(false);
        else Resume();
    }

    public void Pause()
    {
        if (paused || capturing || leaving) return;
        paused = true;
        Time.timeScale = 0f;
        StartCoroutine(CapturePauseBackground());
    }

    // Один снимок на открытие паузы. Во время паузы размытие не считается каждый кадр.
    private IEnumerator CapturePauseBackground()
    {
        capturing = true;
        yield return new WaitForEndOfFrame();
        var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        int width = Mathf.Max(1, screenshot.width / 2);
        int height = Mathf.Max(1, screenshot.height / 2);
        pauseTexture = new RenderTexture(width, height, 0);
        var temporary = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(screenshot, pauseTexture);
        for (int pass = 0; pass < 3; pass++)
        {
            blurMaterial.SetVector("_Direction", new Vector4(1f / width, 0, 0, 0));
            Graphics.Blit(pauseTexture, temporary, blurMaterial);
            blurMaterial.SetVector("_Direction", new Vector4(0, 1f / height, 0, 0));
            Graphics.Blit(temporary, pauseTexture, blurMaterial);
        }
        RenderTexture.ReleaseTemporary(temporary);
        Destroy(screenshot);
        blurredBackground.texture = pauseTexture;
        pauseOverlay.SetActive(true);
        ShowSettings(false);
        capturing = false;
    }

    public void Resume()
    {
        if (capturing || leaving) return;
        paused = false;
        pauseOverlay.SetActive(false);
        Time.timeScale = 1f;
        EventSystem.current.SetSelectedGameObject(null);
        ReleasePauseTexture();
    }

    public void ShowSettings(bool show)
    {
        pausePanel.SetActive(!show);
        settingsPanel.SetActive(show);
        EventSystem.current.SetSelectedGameObject(show ? settingsBackButton.gameObject : resumeButton.gameObject);
    }

    public void MainMenu()
    {
        if (leaving) return;
        leaving = true;
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("MainMenu");
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

    private void ReleasePauseTexture()
    {
        if (blurredBackground != null) blurredBackground.texture = null;
        if (pauseTexture == null) return;
        pauseTexture.Release();
        Destroy(pauseTexture);
        pauseTexture = null;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        pauseAction.Dispose();
        eatAction.Dispose();
        ReleasePauseTexture();
    }
}
