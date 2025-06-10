using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GMMComponent
{
    public float weight;
    public Vector3 mean;
    public Vector3 covarianceDiag;
}

[System.Serializable]
public class GMMEntry
{
    public int azimuthDeg;
    public int elevationDeg;
    public List<GMMComponent> components;
}

[System.Serializable]
public class GMMTablePerBS
{
    public int baseStationIndex;
    public List<GMMEntry> entries = new List<GMMEntry>();
}

[System.Serializable]
public class GMMTableListWrapper
{
    public List<GMMTablePerBS> tables;
}
