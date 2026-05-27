using UnityEngine;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscene/CutsceneData")]
public class CutsceneData : ScriptableObject
{
    [System.Serializable]
    public class CutsceneStep
    {
        [Header("Texto")]
        [TextArea(2, 5)]
        public string dialogueText;
        public bool showDialogue = true;

        [Header("Câmera")]
        [Tooltip("-1 = não mover a câmera neste step")]
        public int cameraTargetIndex = -1;
        public float cameraRotateSpeed = 3f;

        [Header("Tempo")]
        [Tooltip("Tempo extra de espera DEPOIS que o typewriter terminar antes de ir pro próximo step")]
        public float holdTimeAfterTyping = 1f;

        [Header("Áudio")]
        public AudioClip voiceClip;
        public AudioClip typewriterTickClip;
        [Range(0f, 1f)] public float tickVolume = 0.3f;
        public int tickEveryNChars = 2;
    }

    public CutsceneStep[] steps;

    [Header("Configurações Gerais")]
    public float typewriterSpeed = 40f;

    [Tooltip("Tempo de espera antes de começar o primeiro step")]
    public float introDelay = 0.5f;

    [Header("Fim da Cutscene")]
    [Tooltip("Tempo de espera após o último step antes de reativar o gameplay.")]
    public float endDelay = 0.5f;

    [Tooltip("Duração do fade de entrada da UI ao fim da cutscene. 0 = sem fade.")]
    public float uiFadeDuration = 0.5f;
}