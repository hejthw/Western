using UnityEngine;
using UnityEngine.UI;   
using System.Collections.Generic;
using TMPro;

public class GraphicsSettings : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Button switchToSoundButton;
    
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject soundPanel;

    private Resolution[] resolutions; //
    
    private void Start()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        int currentIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "×" + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;

        resolutionDropdown.RefreshShownValue();
        fullscreenToggle.isOn = Screen.fullScreen;
        
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        switchToSoundButton.onClick.AddListener(SwitchToSound);
    }

    private void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    private void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetString("Resolution", res.width + "x" + res.height);
    }

    private void SwitchToSound()
    {
        graphicsPanel.SetActive(!graphicsPanel.activeSelf);
        soundPanel.SetActive(!soundPanel.activeSelf);
    }
    
}