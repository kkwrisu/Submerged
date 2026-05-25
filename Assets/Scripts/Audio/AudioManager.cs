using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Volumes")]
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("SFX Clips")]
    public AudioClip buttonClickSFX;
    public AudioClip buttonHoverSFX;
    public AudioClip buttonDisabledSFX;

    [Header("Music Per Scene")]
    public SceneMusic[] sceneMusics;

    [System.Serializable]
    public class SceneMusic
    {
        public string sceneName;
        public AudioClip clip;
        [Range(0f, 2f)] public float fadeDuration = 1f;
        [Range(0f, 1f)] public float volume = 1f;
    }

    private bool isFading = false;
    private float currentClipVolume = 1f;
    private int enemiesInChase = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadVolumes();
        ApplyVolumes();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        enemiesInChase = 0;

        foreach (var entry in sceneMusics)
        {
            if (entry.sceneName == scene.name)
            {
                currentClipVolume = entry.volume;
                PlayMusic(entry.clip, entry.fadeDuration);
                return;
            }
        }
        StopMusic();
    }

    private void PlayMusic(AudioClip clip, float fadeDuration)
    {
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(FadeToMusic(clip, fadeDuration));
    }

    private void StopMusic(float fadeDuration = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(FadeToMusic(null, fadeDuration));
    }

    private IEnumerator FadeToMusic(AudioClip newClip, float duration)
    {
        isFading = true;
        float targetVolume = musicVolume * currentClipVolume;

        float startVol = musicSource.volume;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }

        musicSource.Stop();

        if (newClip == null)
        {
            musicSource.volume = targetVolume;
            isFading = false;
            yield break;
        }

        musicSource.clip = newClip;
        musicSource.Play();

        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t / duration);
            yield return null;
        }
        musicSource.volume = targetVolume;
        isFading = false;
    }

    public void PauseMusicForChase()
    {
        enemiesInChase++;

        if (musicSource == null || !musicSource.isPlaying) return;
        StopAllCoroutines();
        isFading = false;
        musicSource.Pause();
    }

    public void ResumeMusicAfterChase()
    {
        enemiesInChase = Mathf.Max(0, enemiesInChase - 1);

        if (enemiesInChase > 0) return;
        if (musicSource == null || musicSource.isPlaying) return;
        musicSource.UnPause();
    }

    public void PlayButtonClick()
    {
        if (buttonClickSFX != null)
            sfxSource.PlayOneShot(buttonClickSFX, sfxVolume);
    }

    public void PlayButtonHover()
    {
        if (buttonHoverSFX != null)
            sfxSource.PlayOneShot(buttonHoverSFX, sfxVolume);
    }

    public void PlayButtonDisabled()
    {
        if (buttonDisabledSFX != null)
            sfxSource.PlayOneShot(buttonDisabledSFX, sfxVolume);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
        ApplyVolumes();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat("SFXVolume", value);
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (!isFading && musicSource != null)
            musicSource.volume = musicVolume * currentClipVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    private void LoadVolumes()
    {
        if (PlayerPrefs.HasKey("MusicVolume"))
            musicVolume = PlayerPrefs.GetFloat("MusicVolume");

        if (PlayerPrefs.HasKey("SFXVolume"))
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume");
    }
}