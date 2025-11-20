using System.IO;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    public static MainManager Instance;

    // Singleton Instance that is initialized on Awake and persists through all Scenes
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadGame();
    }

    /// <summary>
    /// Data to be stored in save file locally.
    /// </summary>
    [System.Serializable]
    class SaveData
    {

    }

    /// <summary>
    /// Saves current game to a save file in JSON format. 
    /// </summary>
    /// TODO: Save to existing file if current game already has been saved before
    public void SaveGame()
    {
        SaveData data = new SaveData();

        string json = JsonUtility.ToJson(data);

        File.WriteAllText(Application.persistentDataPath + "/savefile.json", json);
    }

    /// <summary>
    /// Load game from save file if it exists.
    /// </summary>
    public void LoadGame()
    {
        string path = Application.persistentDataPath + "/savefile.json";
        if(File.Exists(path))
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
        }
    }
}
