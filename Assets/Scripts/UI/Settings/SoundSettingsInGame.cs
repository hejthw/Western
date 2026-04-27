using UnityEngine;
using UnityEngine.UI;   
using System.Collections.Generic;
using TMPro;

public class SoundSettingsInGame : MonoBehaviour
{
    [Header("UI")] 
    [SerializeField] private Slider soundSlider;
    [SerializeField] private Slider musicSlider;
    
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
    }

    private void SetVolume(float volume)
    {
        PlayerPrefs.SetFloat("SoundVolume", volume);
    }
    
    private void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }
}