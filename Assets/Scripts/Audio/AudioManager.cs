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
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        Debug.Log($"[AudioManager] Awake | instance={GetInstanceID()} | registrando OnSceneLoaded");

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
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AudioManager] OnSceneLoaded: {scene.name} | instance={GetInstanceID()}");

        foreach (var entry in sceneMusics)
        {
            if (entry.sceneName == scene.name)
            {
                enemiesInChase = 0;
                currentClipVolume = entry.volume;

                if (musicSource.clip == entry.clip && musicSource.isPlaying)
                {
                    ApplyVolumes();
                    return;
                }

                // Clip já carregado no source, mas pausado/parado (ex: ficou preso
                // num PauseMusicForChase sem o Resume correspondente). Nesse caso
                // não precisa de fade, só retomar o play.
                if (musicSource.clip == entry.clip && !musicSource.isPlaying)
                {
                    musicSource.UnPause();
                    if (!musicSource.isPlaying)
                        musicSource.Play();
                    ApplyVolumes();
                    return;
                }

                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeToMusic(entry.clip, entry.fadeDuration));
                return;
            }
        }

        if (mode == LoadSceneMode.Additive)
            return;

        enemiesInChase = 0;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeToMusic(null, 1f));
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (scene.name == activeScene.name)
            return;

        foreach (var entry in sceneMusics)
        {
            if (entry.sceneName == activeScene.name)
            {
                currentClipVolume = entry.volume;

                if (musicSource.clip == entry.clip && musicSource.isPlaying)
                    return;

                if (musicSource.clip == entry.clip && !musicSource.isPlaying)
                {
                    musicSource.UnPause();
                    if (!musicSource.isPlaying)
                        musicSource.Play();
                    return;
                }

                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeToMusic(entry.clip, entry.fadeDuration));
                return;
            }
        }
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
        _fadeCoroutine = null;
    }

    public void PauseMusicForChase()
    {
        enemiesInChase++;
        if (musicSource == null || !musicSource.isPlaying) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
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
        if (buttonClickSFX != null) sfxSource.PlayOneShot(buttonClickSFX, sfxVolume);
    }

    public void PlayButtonHover()
    {
        if (buttonHoverSFX != null) sfxSource.PlayOneShot(buttonHoverSFX, sfxVolume);
    }

    public void PlayButtonDisabled()
    {
        if (buttonDisabledSFX != null) sfxSource.PlayOneShot(buttonDisabledSFX, sfxVolume);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume);
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