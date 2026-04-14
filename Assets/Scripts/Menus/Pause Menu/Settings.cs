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
        RefreshSlidersFromSavedValues();
        ApplyVolumes();
    }

    public void SetMusicVolume(float value)
    {
        if (isRefreshingUI)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (isRefreshingUI)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(value);
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

    private void ApplyVolumes()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 1f));
            AudioManager.Instance.SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 1f));
        }
    }
}