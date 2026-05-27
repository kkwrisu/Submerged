using System.Collections;
using TMPro;
using UnityEngine;

public class CutsceneDialogueUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;

    private Coroutine typewriterCoroutine;

    public bool IsTyping { get; private set; }

    public void Show() => dialoguePanel?.SetActive(true);

    public void Hide()
    {
        dialoguePanel?.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
    }

    public void StartTypewriter(string text, float charsPerSecond, AudioClip tickClip,
        float tickVolume, int tickEveryN, AudioSource audioSource)
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        typewriterCoroutine = StartCoroutine(
            TypewriterRoutine(text, charsPerSecond, tickClip, tickVolume, tickEveryN, audioSource)
        );
    }

    private IEnumerator TypewriterRoutine(string text, float charsPerSecond, AudioClip tickClip,
        float tickVolume, int tickEveryN, AudioSource audioSource)
    {
        IsTyping = true;
        dialogueText.text = "";

        float delay = 1f / charsPerSecond;
        int charCount = 0;

        foreach (char c in text)
        {
            dialogueText.text += c;
            charCount++;

            if (tickClip != null && audioSource != null && charCount % tickEveryN == 0)
                audioSource.PlayOneShot(tickClip, tickVolume);

            yield return new WaitForSecondsRealtime(delay);
        }

        IsTyping = false;
    }
}