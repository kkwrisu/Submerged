using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string currentSceneName;
    public SerializableVector3 checkpointPosition;
    public float checkpointYRotation;
    public float alertLevel = 0f;
    public List<LeverSaveRecord> levers = new List<LeverSaveRecord>();
    public List<DoorSaveRecord> doors = new List<DoorSaveRecord>();
    public List<PuzzleSaveRecord> puzzles = new List<PuzzleSaveRecord>();
    public List<TutorialSeenRecord> seenTutorials = new List<TutorialSeenRecord>();
    public List<QuestProgressRecord> questProgress = new List<QuestProgressRecord>();

    /// <summary>
    /// JsonUtility.FromJson não inicializa listas na build compilada.
    /// Chame este método logo após desserializar para garantir que
    /// nenhuma lista seja null.
    /// </summary>
    public void EnsureListsInitialized()
    {
        if (levers == null) levers = new List<LeverSaveRecord>();
        if (doors == null) doors = new List<DoorSaveRecord>();
        if (puzzles == null) puzzles = new List<PuzzleSaveRecord>();
        if (seenTutorials == null) seenTutorials = new List<TutorialSeenRecord>();
        if (questProgress == null) questProgress = new List<QuestProgressRecord>();

        for (int i = 0; i < questProgress.Count; i++)
        {
            if (questProgress[i].talkedNpcIds == null)
            {
                QuestProgressRecord r = questProgress[i];
                r.talkedNpcIds = new List<string>();
                questProgress[i] = r;
            }
        }
    }
}

[Serializable]
public struct LeverSaveRecord
{
    public string id;
    public bool activated;
}

[Serializable]
public struct DoorSaveRecord
{
    public string id;
    public bool isOpen;
    public bool[] unlockedLocks;
}

[Serializable]
public struct PuzzleSaveRecord
{
    public string id;
    public bool completed;
}

[Serializable]
public struct QuestProgressRecord
{
    public string questId;
    public bool completed;
    public List<string> talkedNpcIds;
}

[Serializable]
public struct SerializableVector3
{
    public float x, y, z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x; this.y = y; this.z = z;
    }

    public SerializableVector3(Vector3 value)
    {
        x = value.x; y = value.y; z = value.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}