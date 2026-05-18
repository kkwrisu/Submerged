using System;
using System.Collections.Generic;

[Serializable]
public class TutorialSeenRecord
{
    public string tutorialID;
    public bool seen;
}

public static class SaveDataTutorialExtensions
{
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