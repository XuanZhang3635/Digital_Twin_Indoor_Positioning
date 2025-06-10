using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GMMTableReader : MonoBehaviour
{
    public string jsonFileName = "GMMTable.json";  // JSON file in Assets folder
    private Dictionary<int, Dictionary<(int, int), List<GMMComponent>>> gmmLookup;

    void Awake()
    {
        LoadGMMTable(Path.Combine(Application.dataPath, jsonFileName));
    }

    void LoadGMMTable(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Debug.LogError("GMM table file not found at: " + fullPath);
            return;
        }

        string json = File.ReadAllText(fullPath);
        GMMTableListWrapper wrapper = JsonUtility.FromJson<GMMTableListWrapper>(json);
        gmmLookup = new Dictionary<int, Dictionary<(int, int), List<GMMComponent>>>();

        foreach (var table in wrapper.tables)
        {
            var dict = new Dictionary<(int, int), List<GMMComponent>>();
            foreach (var entry in table.entries)
            {
                (int, int) key = (entry.azimuthDeg, entry.elevationDeg);
                dict[key] = entry.components;
            }
            gmmLookup[table.baseStationIndex] = dict;
        }

        Debug.Log("GMM table loaded. Base stations: " + gmmLookup.Count);
    }

    /// <summary>
    /// 查找指定基站下 azimuth, elevation 对应的 GMM。
    /// </summary>
    public List<GMMComponent> FindGMMForBaseStation(int bsIndex, int azimuthDeg, int elevationDeg)
    {
        if (gmmLookup == null)
        {
            Debug.LogError("GMM lookup table not initialized.");
            return null;
        }

        if (!gmmLookup.ContainsKey(bsIndex)) return null;

        var dict = gmmLookup[bsIndex];
        (int, int) key = (azimuthDeg, elevationDeg);

        if (dict.ContainsKey(key))
        {
            return dict[key];
        }
        else
        {
            Debug.LogWarning($"No GMM found for BS {bsIndex} @ az={azimuthDeg}, el={elevationDeg}");
            return null;
        }
    }

    /// <summary>
    /// 获取当前支持的方向键列表（用于调试或插值）
    /// </summary>
    public List<(int azimuth, int elevation)> GetAvailableDirections(int bsIndex)
    {
        if (gmmLookup.ContainsKey(bsIndex))
        {
            return new List<(int, int)>(gmmLookup[bsIndex].Keys);
        }
        return new List<(int, int)>();
    }
}
