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

        [Tooltip("Cor do glow aplicado nos objetos destacados.")]
        public Color highlightColor = Color.yellow;
    }

    [Header("Identificação")]
    public string tutorialID;

    [Header("Slides")]
    public TutorialSlide[] slides;
}