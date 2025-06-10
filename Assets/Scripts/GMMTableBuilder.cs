using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;

public class GMMTableBuilder : MonoBehaviour
{
    [Header("AoA Settings")]
    public int azimuthStart = 0;
	public int azimuthEnd = 360;
	public int azimuthStep = 2;

	public int elevationStart = -90;
	public int elevationEnd = 90;
	public int elevationStep = 2;

	private int[] azimuthDegrees;
	private int[] elevationDegrees;

    public int raysPerDir = 100;
    public int gmmComponents = 1;

    [Header("Reflection Settings")]
    public int maxReflections = 2;
    public float rayLength = 100f;
    public LayerMask raycastLayers = Physics.DefaultRaycastLayers;

    [Header("Output")]
    public string outputFileName = "GMMTable.json";

    // 固定的AP位置和朝向
    private Vector3[] baseStationPositions = new Vector3[]
    {
        new Vector3(7f, 2f, -28f),
        new Vector3(2f, 2f, -28f),
        new Vector3(3f, 2f, -20f),
        new Vector3(7f, 2f, -20f)
    };

    private float[] APyaw = new float[] { 135f, 45f, -60f, -110f }; // 单位：度
    private GameObject[] baseStations;

    void Start()
    {
		azimuthDegrees = GenerateRange(azimuthStart, azimuthEnd, azimuthStep);
    	elevationDegrees = GenerateRange(elevationStart, elevationEnd, elevationStep);

        SetupBaseStations();
        SetupSceneColliders();
        StartCoroutine(BuildAllTables());
    }

	int[] GenerateRange(int start, int end, int step)
	{
		List<int> list = new List<int>();
		for (int val = start; val < end; val += step)
		{
			list.Add(val);
		}
		return list.ToArray();
	}

    void SetupBaseStations()
    {
        baseStations = new GameObject[baseStationPositions.Length];
        for (int i = 0; i < baseStationPositions.Length; i++)
        {
            GameObject ap = new GameObject("BaseStation_" + i);
            ap.transform.position = baseStationPositions[i];
            ap.transform.rotation = Quaternion.Euler(0, APyaw[i], 0);
            baseStations[i] = ap;
        }
    }

    void SetupSceneColliders()
    {
        GameObject roof = GameObject.Find("roof");
        if (roof != null)
        {
            MeshCollider mc = roof.GetComponent<MeshCollider>();
            if (mc == null) mc = roof.AddComponent<MeshCollider>();
            mc.convex = false;

            MeshFilter mf = roof.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) mc.sharedMesh = mf.sharedMesh;
        }
        else
        {
            Debug.LogWarning("Roof model not found!");
        }
    }

    IEnumerator BuildAllTables()
    {
        List<GMMTablePerBS> allTables = new List<GMMTablePerBS>();

        for (int bsIdx = 0; bsIdx < baseStations.Length; bsIdx++)
        {
            GMMTablePerBS table = new GMMTablePerBS();
            table.baseStationIndex = bsIdx;

            foreach (int az in azimuthDegrees)
            {
                foreach (int el in elevationDegrees)
                {
                    Debug.Log("111111");
                    float adjustedAz = az + APyaw[bsIdx];
                    Vector3 bsPos = baseStations[bsIdx].transform.position;
                    Vector3 direction = SphericalToCartesian(Mathf.Deg2Rad * adjustedAz, Mathf.Deg2Rad * el);
                    List<Vector3> hits = new List<Vector3>();

                    for (int i = 0; i < raysPerDir; i++)
                    {
                        Vector3 jitteredDir = direction + Random.insideUnitSphere * 0.01f;
                        List<Vector3> reflectionHits = SimulateReflections(bsPos, jitteredDir.normalized, maxReflections);

                        if (reflectionHits.Count > 0)
                        {
                            hits.Add(reflectionHits.Last());
                        }
                    }

                    if (hits.Count > 0)
                    {
                        List<GMMComponent> comps = FitGMM(hits, gmmComponents);
                        GMMEntry entry = new GMMEntry
                        {
                            azimuthDeg = az,
                            elevationDeg = el,
                            components = comps
                        };
                        table.entries.Add(entry);
                    }

                    yield return null; // 防卡死
                }
            }

            allTables.Add(table);
        }

        string json = JsonUtility.ToJson(new GMMTableListWrapper { tables = allTables }, true);
        File.WriteAllText(Path.Combine(Application.dataPath, outputFileName), json);
        Debug.Log("GMM table saved to: " + outputFileName);
    }

    List<Vector3> SimulateReflections(Vector3 origin, Vector3 direction, int maxReflections)
    {
        List<Vector3> hitPoints = new List<Vector3>();
        Vector3 currentOrigin = origin;
        Vector3 currentDir = direction;

        for (int i = 0; i < maxReflections; i++)
        {
            if (Physics.Raycast(currentOrigin, currentDir, out RaycastHit hit, rayLength, raycastLayers))
            {
                hitPoints.Add(hit.point);
                currentOrigin = hit.point + hit.normal * 0.01f;
                currentDir = Vector3.Reflect(currentDir, hit.normal);
            }
            else
            {
                break;
            }
        }

        return hitPoints;
    }

    List<GMMComponent> FitGMM(List<Vector3> points, int components)
    {
        Vector3 mean = points.Aggregate(Vector3.zero, (a, b) => a + b) / points.Count;
        Vector3 cov = Vector3.zero;
        foreach (var p in points)
        {
            Vector3 d = p - mean;
            cov += new Vector3(d.x * d.x, d.y * d.y, d.z * d.z);
        }
        cov /= points.Count;

        return new List<GMMComponent> {
            new GMMComponent { weight = 1f, mean = mean, covarianceDiag = cov }
        };
    }

    Vector3 SphericalToCartesian(float az, float el)
    {
        float x = Mathf.Cos(el) * Mathf.Cos(az);
        float y = Mathf.Sin(el);
        float z = Mathf.Cos(el) * Mathf.Sin(az);
        return new Vector3(x, y, z);
    }

    [System.Serializable]
    public class GMMTableListWrapper
    {
        public List<GMMTablePerBS> tables;
    }
}
