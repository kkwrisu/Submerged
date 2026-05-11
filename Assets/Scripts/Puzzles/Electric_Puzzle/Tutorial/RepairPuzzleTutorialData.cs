using UnityEngine;

/// <summary>
/// ScriptableObject que define os slides do tutorial para um tipo de puzzle de reparo.
/// Crie um asset por tipo de puzzle (ex: "WirePuzzleTutorial") via
/// Create > RepairPuzzle > TutorialData no menu do Unity.
/// </summary>
[CreateAssetMenu(menuName = "RepairPuzzle/TutorialData", fileName = "NewRepairPuzzleTutorial")]
public class RepairPuzzleTutorialData : ScriptableObject
{
    [System.Serializable]
    public class TutorialSlide
    {
        [Tooltip("Título curto exibido no topo do slide (ex: 'Hazard').")]
        public string title;

        [TextArea(2, 5)]
        [Tooltip("Descrição explicativa do elemento ou mecânica.")]
        public string description;

        [Tooltip("Sprite do elemento sendo explicado. Deixe null para slides sem ícone (ex: slide de controles).")]
        public Sprite elementSprite;

        [Tooltip("Cor de tint do sprite highlight (use a cor real do elemento no jogo).")]
        public Color highlightColor = Color.white;

        [Tooltip("Se true, exibe o painel de controles em vez de um sprite de elemento.")]
        public bool isControlsSlide = false;
    }

    [Header("Identificação")]
    [Tooltip("ID único deste tutorial. Usado no save para marcar 'já viu'. Ex: 'wire_puzzle'.")]
    public string tutorialID;

    [Header("Slides")]
    public TutorialSlide[] slides;
}