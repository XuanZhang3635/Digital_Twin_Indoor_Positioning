using System.Collections.Generic;

[System.Serializable]
public class LocalizationData
{
    public List<List<float?>> AoA;
    public List<List<float>> AP;
    public List<float> APpitch;
    public List<float> APyaw;
    public List<List<float>> ground_truth;
}
