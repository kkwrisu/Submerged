using UnityEngine;

[CreateAssetMenu(menuName = "Generator/TutorialData", fileName = "NewGeneratorTutorial")]
public class GeneratorTutorialData : ScriptableObject
{
    [System.Serializable]
    public class TutorialSlide
    {
        [Tooltip("Título exibido no topo do slide.")]
        public string title;

        [TextArea(2, 5)]
        [Tooltip("Descrição explicativa da mecânica.")]
        public string description;
    }

    [Header("Identificação")]
    public string tutorialID;

    [Header("Slides — Tutorial de Aproximação")]
    public TutorialSlide[] approachSlides;

    [Header("Slides — Tutorial de QTE")]
    public TutorialSlide[] qteSlides;
}