﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// This script sets up the graphics from the settings previously used
/// </summary>
public class ViewInitializer : MonoBehaviour
{
    public AudioMixer audioMixer;
    private const string musicVolumeKey = "MusicVolume";

    void Start()
    {
        float volume = OptionsPreferencesManager.GetVolume();
        audioMixer.SetFloat(musicVolumeKey, volume);

        int width = OptionsPreferencesManager.GetResolutionWidth();
        int height = OptionsPreferencesManager.GetResolutionHeight();
        bool isFullScreen = OptionsPreferencesManager.GetFullScreen();
        Screen.SetResolution(width, height, isFullScreen);

        int qualityIndex = OptionsPreferencesManager.GetQuality();
        QualitySettings.SetQualityLevel(qualityIndex);
    }
}
