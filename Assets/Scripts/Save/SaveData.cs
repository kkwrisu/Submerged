using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string currentSceneName;
    public SerializableVector3 checkpointPosition;
    public float checkpointYRotation;

    public List<LeverSaveRecord> levers = new List<LeverSaveRecord>();
    public List<DoorSaveRecord> doors = new List<DoorSaveRecord>();
    public List<PuzzleSaveRecord> puzzles = new List<PuzzleSaveRecord>();
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
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public SerializableVector3(Vector3 value)
    {
        x = value.x;
        y = value.y;
        z = value.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}