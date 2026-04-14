public interface ISaveable
{
    string GetSaveID();
    void LoadFromSave(SaveData data);
    void SaveToData(SaveData data);
}