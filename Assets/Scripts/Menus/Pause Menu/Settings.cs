using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("UI")]
    public Slider musicSlider;
    public Slider sfxSlider;

    private bool isRefreshingUI;

    private void OnEnable()
    {
        if (musicSlider == null || sfxSlider == null)
            ReconnectSliders();

        RefreshSlidersFromSavedValues();
    }

    private void ReconnectSliders()
    {
        Slider[] sliders = GetComponentsInChildren<Slider>(true);

        foreach (Slider s in sliders)
        {
            if (s.name == "BGM Slider") musicSlider = s;
            else if (s.name == "SFX Slider") sfxSlider = s;
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        Debug.Log("[Settings] MusicSlider: " + (musicSlider != null ? "OK" : "NULL"));
        Debug.Log("[Settings] SFXSlider: " + (sfxSlider != null ? "OK" : "NULL"));
    }

    public void SetMusicVolume(float value)
    {
        if (isRefreshingUI) return;
        AudioManager.Instance?.SetMusicVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (isRefreshingUI) return;
        AudioManager.Instance?.SetSFXVolume(value);
    }

    private void RefreshSlidersFromSavedValues()
    {
        isRefreshingUI = true;
        if (musicSlider != null)
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        isRefreshingUI = false;
    }
}