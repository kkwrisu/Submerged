using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneData cutsceneData;

    [Tooltip("Se true, dispara automaticamente no Start")]
    public bool playOnStart = true;

    [Header("Diálogo Pós-Cutscene")]
    [Tooltip("Se preenchido, este Interactable é usado para abrir um diálogo (via DialogueManager) imediatamente após o gameplay ser liberado ao fim desta cutscene. Útil para explicar comandos ao jogador.")]
    public Interactable postCutsceneDialogue;

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
            CutsceneManager.Instance.PlayCutscene(cutsceneData, postCutsceneDialogue);
        else
            Debug.LogWarning("[CutsceneTrigger] CutsceneManager.Instance não encontrado.");
    }
}