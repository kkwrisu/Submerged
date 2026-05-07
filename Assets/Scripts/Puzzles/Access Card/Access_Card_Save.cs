using UnityEngine;

public class AccessCardSaveBridge : MonoBehaviour, ISaveable
{
    private const string SAVE_ID = "__access_card_level__";

    public string GetSaveID() => SAVE_ID;

    public void SaveToData(SaveData data)
    {
        if (AccessCardManager.Instance == null) return;

        int level = AccessCardManager.Instance.CardLevel;

        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id == SAVE_ID)
            {
                data.puzzles[i] = new PuzzleSaveRecord { id = SAVE_ID + level, completed = true };
                return;
            }

            if (data.puzzles[i].id.StartsWith(SAVE_ID))
            {
                data.puzzles[i] = new PuzzleSaveRecord { id = SAVE_ID + level, completed = true };
                return;
            }
        }

        data.puzzles.Add(new PuzzleSaveRecord { id = SAVE_ID + level, completed = true });
    }

    public void LoadFromSave(SaveData data)
    {
        if (AccessCardManager.Instance == null) return;

        for (int i = 0; i < data.puzzles.Count; i++)
        {
            if (data.puzzles[i].id.StartsWith(SAVE_ID) && data.puzzles[i].completed)
            {
                string suffix = data.puzzles[i].id.Substring(SAVE_ID.Length);

                if (int.TryParse(suffix, out int savedLevel))
                {
                    AccessCardManager.Instance.SetLevel(savedLevel);
                    Debug.Log($"[AccessCardSaveBridge] Nível carregado: {savedLevel}");
                }
                return;
            }
        }

        Debug.Log("[AccessCardSaveBridge] Nenhum save de cartão encontrado, mantendo nível 1.");
    }
}