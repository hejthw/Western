using UnityEngine;
using UnityEngine.UI;   
using System.Collections.Generic;
using TMPro;

public class SoundSettings : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;
    
    [SerializeField] private Button switchToGraphicsButton;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject soundPanel;
    
    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
        soundSlider.value = savedVolume;
        SetVolume(savedVolume);
        
        float savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        musicSlider.value = savedMusicVolume;
        SetMusicVolume(savedMusicVolume);
        
        
        soundSlider.onValueChanged.AddListener(SetVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        switchToGraphicsButton.onClick.AddListener(SwitchToGraphics);
    }

    private void SetVolume(float volume)
    {
        PlayerPrefs.SetFloat("SoundVolume", volume);
    }
    
    private void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
    
    private void SwitchToGraphics()
    {
        graphicsPanel.SetActive(!graphicsPanel.activeSelf);
        soundPanel.SetActive(!soundPanel.activeSelf);
    }
}