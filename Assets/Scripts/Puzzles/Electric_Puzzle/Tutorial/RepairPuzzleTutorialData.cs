using UnityEngine;

[CreateAssetMenu(menuName = "RepairPuzzle/TutorialData", fileName = "NewRepairPuzzleTutorial")]
public class RepairPuzzleTutorialData : ScriptableObject
{
    [System.Serializable]
    public class TutorialSlide
    {
        [Tooltip("Título curto exibido no topo do slide.")]
        public string title;

        [TextArea(2, 5)]
        [Tooltip("Descrição explicativa do elemento ou mecânica.")]
        public string description;

        [Tooltip("Se false, nenhum objeto será destacado neste slide.")]
        public bool hasHighlight = false;

        [Tooltip("Tipos de nó que serão destacados neste slide. Cada tipo pode ter sua própria cor.")]
        public HighlightEntry[] highlightEntries;
    }

    [System.Serializable]
    public class HighlightEntry
    {
        [Tooltip("Tipo de nó a destacar.")]
        public RepairPuzzleNodeType nodeType = RepairPuzzleNodeType.Empty;

        [Tooltip("Cor do glow para este tipo de nó.")]
        public Color highlightColor = Color.yellow;
    }

    [Header("Identificação")]
    public string tutorialID;

    [Header("Slides")]
    public TutorialSlide[] slides;
}