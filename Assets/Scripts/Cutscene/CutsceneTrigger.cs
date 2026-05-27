using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneData cutsceneData;

    [Tooltip("Se true, dispara automaticamente no Start")]
    public bool playOnStart = true;

    private void Start()
    {
        if (playOnStart && cutsceneData != null)
        {
            bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();

            if (hasSave)
                GameUI.Instance?.ShowImmediate();
            else
                Play();
        }
        else
        {
            GameUI.Instance?.ShowImmediate();
        }
    }

    public void Play()
    {
        if (CutsceneManager.Instance != null)
            CutsceneManager.Instance.PlayCutscene(cutsceneData);
        else
            Debug.LogWarning("[CutsceneTrigger] CutsceneManager.Instance não encontrado.");
    }
}