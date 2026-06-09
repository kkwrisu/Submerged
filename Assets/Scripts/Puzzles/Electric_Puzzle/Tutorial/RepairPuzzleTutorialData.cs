using UnityEngine;

[CreateAssetMenu(menuName = "RepairPuzzle/TutorialData", fileName = "NewRepairPuzzleTutorial")]
public class RepairPuzzleTutorialData : ScriptableObject
{
    [System.Serializable]
    public class TutorialSlide
    {
        [HideInInspector]
        public string title;

        [TextArea(2, 5)]
        [Tooltip("Texto exibido no painel cyberpunk.")]
        public string description;

        [Tooltip("Se false, nenhum objeto é destacado neste slide.")]
        public bool hasHighlight = false;

        [Tooltip("Nós a destacar neste slide, cada um com cor própria.")]
        public HighlightEntry[] highlightEntries;

        [Header("Posição do painel")]
        [Tooltip("Se true, move o painel (botão + texto) para panelPosition neste slide.")]
        public bool overridePosition = false;

        [Tooltip("anchoredPosition do RectTransform do panelRoot para este slide.")]
        public Vector2 panelPosition;

        [Header("Posição do overlay (balão)")]
        [Tooltip("Se true, move o overlay para overlayPosition neste slide.")]
        public bool overrideOverlayPosition = false;

        [Tooltip("anchoredPosition do RectTransform do overlayImage para este slide.")]
        public Vector2 overlayPosition;
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