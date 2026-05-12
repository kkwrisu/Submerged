using System;
using System.Collections.Generic;
using UnityEngine;

public class RepairPuzzleTutorialDialogue : MonoBehaviour
{
    [System.Serializable]
    public class SlideHighlightEntry
    {
        [Tooltip("Índice do slide (começa em 0).")]
        public int slideIndex;

        [Tooltip("Objetos da cena que devem brilhar neste slide.")]
        public GameObject[] targets;
    }

    [Header("Highlights por Slide")]
    [Tooltip("Configure aqui quais objetos da cena destacar em cada slide.")]
    public SlideHighlightEntry[] slideHighlights;

    // ── Estado interno ───────────────────────────────────────────────────────

    private RepairPuzzleTutorialData data;
    private int currentSlide;
    private Action onFinished;
    private List<RepairPuzzleTutorialHighlight> activeHighlights = new List<RepairPuzzleTutorialHighlight>();

    // ── API Pública ──────────────────────────────────────────────────────────

    public void Show(RepairPuzzleTutorialData tutorialData, Action onFinishedCallback)
    {
        if (tutorialData == null || tutorialData.slides == null || tutorialData.slides.Length == 0)
        {
            onFinishedCallback?.Invoke();
            return;
        }

        data = tutorialData;
        onFinished = onFinishedCallback;
        currentSlide = 0;

        ShowSlide(currentSlide);
    }

    public void Hide()
    {
        ClearHighlights();

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.ForceEndDialogue();
    }

    // ── Navegação ────────────────────────────────────────────────────────────

    private void GoNext()
    {
        if (data == null) return;

        if (currentSlide >= data.slides.Length - 1)
        {
            Finish();
            return;
        }

        currentSlide++;
        ShowSlide(currentSlide);
    }

    private void GoPrev()
    {
        if (currentSlide <= 0) return;

        currentSlide--;
        ShowSlide(currentSlide);
    }

    private void Finish()
    {
        ClearHighlights();
        onFinished?.Invoke();
    }

    // ── Slide ────────────────────────────────────────────────────────────────

    private void ShowSlide(int index)
    {
        if (data == null || index < 0 || index >= data.slides.Length)
            return;

        ClearHighlights();

        RepairPuzzleTutorialData.TutorialSlide slide = data.slides[index];

        string fullText = $"<b>{slide.title}</b>\n{slide.description}";

        bool isFirst = index == 0;
        bool isLast = index == data.slides.Length - 1;

        List<Interactable.DialogueChoice> choices = new List<Interactable.DialogueChoice>();

        // Pular
        Interactable.DialogueChoice skipChoice = new Interactable.DialogueChoice();
        skipChoice.choiceText = "Pular";
        skipChoice.closeDialogue = false;
        skipChoice.triggerAction = false;
        skipChoice.onChoiceSelected = new UnityEngine.Events.UnityEvent();
        skipChoice.onChoiceSelected.AddListener(() => Finish());
        choices.Add(skipChoice);

        // Anterior
        if (!isFirst)
        {
            Interactable.DialogueChoice prevChoice = new Interactable.DialogueChoice();
            prevChoice.choiceText = "← Anterior";
            prevChoice.closeDialogue = false;
            prevChoice.triggerAction = false;
            prevChoice.onChoiceSelected = new UnityEngine.Events.UnityEvent();
            prevChoice.onChoiceSelected.AddListener(() => GoPrev());
            choices.Add(prevChoice);
        }

        // Próximo / Entendido
        Interactable.DialogueChoice nextChoice = new Interactable.DialogueChoice();
        nextChoice.choiceText = isLast ? "Entendido!" : "Próximo →";
        nextChoice.closeDialogue = false;
        nextChoice.triggerAction = false;
        nextChoice.onChoiceSelected = new UnityEngine.Events.UnityEvent();
        nextChoice.onChoiceSelected.AddListener(() => GoNext());
        choices.Add(nextChoice);

        // Node de diálogo
        Interactable.DialogueNode node = new Interactable.DialogueNode();
        node.text = fullText;
        node.choices = choices.ToArray();
        node.endDialogueHere = false;
        node.nextNodeIndex = -1;

        Interactable tempInteractable = new GameObject("__TutorialDialogueTemp__").AddComponent<Interactable>();
        tempInteractable.dialogueNodes = new Interactable.DialogueNode[] { node };
        DontDestroyOnLoad(tempInteractable.gameObject);

        DialogueManager.Instance.StartDialogue(tempInteractable);

        Destroy(tempInteractable.gameObject, 0.1f);

        ActivateHighlights(index, slide.highlightColor);
    }

    // ── Highlights ───────────────────────────────────────────────────────────

    private void ActivateHighlights(int slideIndex, Color color)
    {
        if (slideHighlights == null) return;

        for (int i = 0; i < slideHighlights.Length; i++)
        {
            if (slideHighlights[i].slideIndex != slideIndex) continue;
            if (slideHighlights[i].targets == null) continue;

            for (int j = 0; j < slideHighlights[i].targets.Length; j++)
            {
                GameObject target = slideHighlights[i].targets[j];
                if (target == null) continue;

                RepairPuzzleTutorialHighlight highlight = target.GetComponent<RepairPuzzleTutorialHighlight>();

                if (highlight == null)
                    highlight = target.AddComponent<RepairPuzzleTutorialHighlight>();

                highlight.glowColor = color;
                highlight.EnableGlow();
                activeHighlights.Add(highlight);
            }

            break;
        }
    }

    private void ClearHighlights()
    {
        for (int i = 0; i < activeHighlights.Count; i++)
        {
            if (activeHighlights[i] != null)
                activeHighlights[i].DisableGlow();
        }

        activeHighlights.Clear();
    }
}