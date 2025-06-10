using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public class GlobalDataManager
{
    private static GlobalDataManager _instance;
    public static GlobalDataManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new GlobalDataManager();
            return _instance;
        }
    }

    public LocalizationData Data { get; private set; }

    private GlobalDataManager()
    {
        // JSON data
        LoadData();  
    }

    private void LoadData()
    {
        string path = Path.Combine(Application.dataPath, "made-data-test-2.json");
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            Data = JsonConvert.DeserializeObject<LocalizationData>(json);
        }
        else
        {
            Debug.LogError("JSON file not found at: " + path);
        }
    }
}
