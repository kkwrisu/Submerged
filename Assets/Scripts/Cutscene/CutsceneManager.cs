using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager Instance;

    [Header("Câmera do Player")]
    [Tooltip("Transform da câmera do player (filho do player, onde está o PlayerLook)")]
    public Transform playerCameraTransform;

    [Header("Camera Look Targets")]
    [Tooltip("GameObjects de cena para onde a câmera pode olhar. Referenciados pelo índice no CutsceneData")]
    public Transform[] cameraTargets;

    [Header("Gameplay Lock")]
    public MonoBehaviour playerMovementScript;
    public MonoBehaviour playerLookScript;
    public PlayerInput playerInput;
    public GameObject[] uiElementsToHide;

    [Header("Inimigos")]
    [Tooltip("Deixe vazio para pausar todos os inimigos da cena automaticamente")]
    public Inimigo[] inimigosParaPausar;

    [Header("Áudio")]
    public AudioSource audioSource;

    private CutsceneData currentData;
    private Interactable pendingPostCutsceneDialogue;
    private bool isActive;
    private Coroutine cameraRoutine;

    private AudioClip currentVoiceClip;
    private float voiceClipPausedTime;

    private Inimigo[] _inimigosCache;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // -------------------------------------------------------------------------
    // API pública
    // -------------------------------------------------------------------------

    public void PlayCutscene(CutsceneData data, Interactable postCutsceneDialogue = null)
    {
        if (isActive || data == null || data.steps == null || data.steps.Length == 0) return;

        currentData = data;
        pendingPostCutsceneDialogue = postCutsceneDialogue;
        isActive = true;

        LockGameplay();
        StartCoroutine(PlayStepsRoutine());
    }

    // -------------------------------------------------------------------------
    // Rotina principal
    // -------------------------------------------------------------------------

    private IEnumerator PlayStepsRoutine()
    {
        if (currentData.introDelay > 0f)
            yield return new WaitForSecondsRealtime(currentData.introDelay);

        for (int i = 0; i < currentData.steps.Length; i++)
            yield return StartCoroutine(PlayStep(currentData.steps[i]));

        EndCutscene();
    }

    private IEnumerator PlayStep(CutsceneData.CutsceneStep step)
    {
        // Câmera
        if (step.cameraTargetIndex >= 0 && step.cameraTargetIndex < cameraTargets.Length)
        {
            if (cameraRoutine != null) StopCoroutine(cameraRoutine);
            cameraRoutine = StartCoroutine(RotateCameraTo(cameraTargets[step.cameraTargetIndex], step.cameraRotateSpeed));
        }

        // Áudio de voz
        if (step.voiceClip != null && audioSource != null)
        {
            if (step.voiceClip == currentVoiceClip)
            {
                audioSource.clip = step.voiceClip;
                audioSource.time = voiceClipPausedTime;
                audioSource.Play();
            }
            else
            {
                currentVoiceClip = step.voiceClip;
                voiceClipPausedTime = 0f;
                audioSource.clip = step.voiceClip;
                audioSource.time = 0f;
                audioSource.Play();
            }
        }

        // Diálogo via DialogueManager
        if (step.showDialogue && !string.IsNullOrEmpty(step.dialogueText))
        {
            DialogueManager.Instance.ShowCutsceneDialogue(
                step.dialogueText,
                step.typewriterTickClip,
                step.tickVolume,
                step.tickEveryNChars
            );

            yield return new WaitUntil(() => !DialogueManager.Instance.IsTyping);
        }
        else
        {
            DialogueManager.Instance.HideCutsceneDialogue();
        }

        // Pausa o áudio ao terminar o typing
        if (audioSource != null && audioSource.isPlaying)
        {
            voiceClipPausedTime = audioSource.time;
            audioSource.Stop();
        }

        if (step.holdTimeAfterTyping > 0f)
            yield return new WaitForSecondsRealtime(step.holdTimeAfterTyping);
    }

    // -------------------------------------------------------------------------
    // Câmera
    // -------------------------------------------------------------------------

    private IEnumerator RotateCameraTo(Transform target, float rotateSpeed)
    {
        if (playerCameraTransform == null) yield break;

        while (true)
        {
            Quaternion targetRot = Quaternion.LookRotation(
                target.position - playerCameraTransform.position
            );

            playerCameraTransform.rotation = Quaternion.RotateTowards(
                playerCameraTransform.rotation,
                targetRot,
                rotateSpeed * 60f * Time.unscaledDeltaTime
            );

            if (Quaternion.Angle(playerCameraTransform.rotation, targetRot) < 0.5f)
                yield break;

            yield return null;
        }
    }

    // -------------------------------------------------------------------------
    // Fim
    // -------------------------------------------------------------------------

    private void EndCutscene()
    {
        if (cameraRoutine != null)
        {
            StopCoroutine(cameraRoutine);
            cameraRoutine = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        currentVoiceClip = null;
        voiceClipPausedTime = 0f;

        DialogueManager.Instance.HideCutsceneDialogue();
        StartCoroutine(EndDelayRoutine());
    }

    private IEnumerator EndDelayRoutine()
    {
        yield return new WaitForSecondsRealtime(currentData.endDelay);

        if (PauseMenu.Instance != null)
            PauseMenu.Instance.BlockPauseUntilEscReleased();

        foreach (var el in uiElementsToHide)
            if (el != null) el.SetActive(true);

        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (playerLookScript != null) playerLookScript.enabled = true;
        if (playerInput != null)
        {
            playerInput.ActivateInput();
            playerInput.actions.FindActionMap("UI")?.Enable();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Reativa os inimigos ao fim da cutscene
        if (_inimigosCache != null)
        {
            foreach (var inimigo in _inimigosCache)
            {
                if (inimigo == null) continue;
                inimigo.enabled = true;
                if (inimigo.agent != null) inimigo.agent.isStopped = false;
            }
        }

        if (GameUI.Instance != null)
            yield return StartCoroutine(GameUI.Instance.FadeIn(currentData.uiFadeDuration));

        isActive = false;

        // ── Diálogo pós-cutscene ────────────────────────────────────────────
        // Roda DEPOIS que o gameplay foi totalmente liberado e a UI já fez
        // fade-in. O próprio DialogueManager.StartDialogue vai re-travar o
        // player, esconder a UI e pausar o jogo de novo — então a ordem aqui
        // é importante: primeiro libera tudo, depois o diálogo assume.
        if (pendingPostCutsceneDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(pendingPostCutsceneDialogue);
            pendingPostCutsceneDialogue = null;
        }
    }

    // -------------------------------------------------------------------------
    // Gameplay lock
    // -------------------------------------------------------------------------

    private void LockGameplay()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (playerLookScript != null) playerLookScript.enabled = false;
        if (playerInput != null)
        {
            playerInput.DeactivateInput();
            playerInput.actions.FindActionMap("UI")?.Disable();
        }

        foreach (var el in uiElementsToHide)
            if (el != null) el.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Usa o array manual se preenchido no Inspector; senão busca todos na cena
        _inimigosCache = (inimigosParaPausar != null && inimigosParaPausar.Length > 0)
            ? inimigosParaPausar
            : FindObjectsByType<Inimigo>(FindObjectsSortMode.None);

        foreach (var inimigo in _inimigosCache)
        {
            if (inimigo == null) continue;
            inimigo.enabled = false;
            if (inimigo.agent != null) inimigo.agent.isStopped = true;
        }
    }

    public bool IsActive() => isActive;
}