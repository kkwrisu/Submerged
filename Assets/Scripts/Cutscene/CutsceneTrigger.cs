using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneData cutsceneData;

    [Tooltip("Se true, dispara automaticamente no Start")]
    public bool playOnStart = true;

    private void Start()
    {
        if (playOnStart && cutsceneData != null)
            Play();
        else
            GameUI.Instance?.ShowImmediate();
    }

    public void Play()
    {
        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.PlayCutscene(cutsceneData);
        else
            Debug.LogWarning("[CutsceneTrigger] CutsceneManager.Instance não encontrado.");
    }
}