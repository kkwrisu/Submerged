using System;
using System.Collections.Generic;

/// <summary>
/// Registro de um tutorial já visto pelo jogador.
/// Adicione um campo `public List<TutorialSeenRecord> seenTutorials = new List<TutorialSeenRecord>();`
/// na sua classe SaveData existente.
/// </summary>
[Serializable]
public class TutorialSeenRecord
{
    public string tutorialID;
    public bool seen;
}

/// <summary>
/// Extensão de SaveData — copie o campo abaixo para dentro da sua classe SaveData:
///
///     public List<TutorialSeenRecord> seenTutorials = new List<TutorialSeenRecord>();
///
/// Este arquivo existe apenas para documentar o modelo; não é necessário compilar
/// separadamente se você preferir editar SaveData diretamente.
/// </summary>
public static class SaveDataTutorialExtensions
{
    /// <summary>
    /// Retorna true se o tutorial com o ID fornecido já foi visto.
    /// Passe a lista `saveData.seenTutorials`.
    /// </summary>
    public static bool HasSeenTutorial(List<TutorialSeenRecord> records, string tutorialID)
    {
        if (records == null) return false;

        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].tutorialID == tutorialID)
                return records[i].seen;
        }

        return false;
    }

    /// <summary>
    /// Marca o tutorial como visto na lista fornecida.
    /// </summary>
    public static void MarkTutorialSeen(List<TutorialSeenRecord> records, string tutorialID)
    {
        if (records == null) return;

        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].tutorialID == tutorialID)
            {
                records[i].seen = true;
                return;
            }
        }

        records.Add(new TutorialSeenRecord { tutorialID = tutorialID, seen = true });
    }
}