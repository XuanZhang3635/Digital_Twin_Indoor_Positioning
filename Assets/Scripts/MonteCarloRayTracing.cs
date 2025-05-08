using System;
using System.Collections.Generic;
using UnityEngine;

// 对于每个AP，采样 l 个角度
// 
public class MonteCarloRayTracing : MonoBehaviour
{
    public GameObject baseStationPrefab; // Base station prefab
    public int raysPerBaseStation = 10; // Number of rays emitted per base station
    // public Vector2[] aoaRanges; // AoA range for each base station
    private GameObject[] baseStations; // Array of base station objects
    private Vector3[] baseStationPositions = new Vector3[] // Base station positions
    {
        new Vector3(7f, 2f, -28f),  // Base station 1
        new Vector3(2f, 2f, -28f),  // Base station 2
        new Vector3(3f, 2f, -20f),  // Base station 3
        new Vector3(7f, 2f, -20f)   // Base station 4
    };
    private GameObject rayContainer; // Container for storing rays
    public int maxReflections = 5; // Maximum number of reflections
    private List<RayData> rays = new List<RayData>(); // List to store ray data
    private IntersectionLogger intersectionLogger; // 交点记录器
    private IntersectionDetector intersectionDetector; 
    private VoxelIntersectionCounter voxelCounter;

    void Start()
    {
        // Initialize base stations
        InitializeBaseStations();

        // Set AoA ranges (manually input)
        // InitializeAoARanges();

        // Initialize ray container
        rayContainer = new GameObject("RayContainer");

        // Initialize ray data
        InitializeRays();

        intersectionLogger = new IntersectionLogger();
        intersectionDetector = new IntersectionDetector(intersectionLogger);
        voxelCounter = new VoxelIntersectionCounter();

        GameObject roof = GameObject.Find("roof"); // 注意名字大小写要完全匹配！
        if (roof != null)
        {
            Debug.Log("Roof found: " + roof.name);
            MeshCollider mc = roof.GetComponent<MeshCollider>();
            if (mc == null)
            {
                mc = roof.AddComponent<MeshCollider>();
                mc.convex = false;
            }
            
            // ✅ 关键：绑定 mesh 到 collider
            MeshFilter mf = roof.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                mc.sharedMesh = mf.sharedMesh;
                Debug.Log("MeshCollider 绑定成功！");
            }
            else
            {
                Debug.LogWarning("MeshFilter 或 sharedMesh 为空！");
            }

        }
        else
        {
            Debug.LogWarning("Couldn't find GameObject named 'roof'");
        }
    }

    void Update()
    {
        // Update each ray
        for (int i = 0; i < rays.Count; i++)
        {
            UpdateRay(rays[i]);
        }
    }

	void OnApplicationQuit()
	{
        voxelCounter.CountVoxels(rays);

        intersectionLogger.SaveRaysToFile(rays);
        intersectionDetector.DetectIntersections(rays);
        intersectionLogger.SaveIntersectionsToFile();

        Vector3 estimate = VoxelUtils.ComputeAveragePositionFromFile("Assets/FourRayIntersections.txt");
        Debug.Log("碰撞点统计最终位置：" + estimate);
        Vector3 estimate1 = VoxelUtils.ComputeAveragePositionFromFile("Assets/voxel_hits.txt");
        Debug.Log("voxel统计最终位置：" + estimate1);

	}

	// Initialize base station positions
	void InitializeBaseStations()
    {
        baseStations = new GameObject[baseStationPositions.Length];
        for (int i = 0; i < baseStationPositions.Length; i++)
        {
            // Instantiate the base station
            baseStations[i] = Instantiate(baseStationPrefab, baseStationPositions[i], Quaternion.identity);
            baseStations[i].name = "BaseStation" + (i + 1); // Set base station name
        }
    }
    // Initialize ray data
    void InitializeRays()
    {
        Color[] baseStationColors = new Color[]
        {
            Color.red,      // Ray color for base station 1
            Color.green,    // Ray color for base station 2
            Color.blue,     // Ray color for base station 3
            Color.yellow    // Ray color for base station 4
        };

        // 每个基站的 azimuth 和 elevation (方位角和俯仰角)
        float[] azimuths = new float[]
        {
            -0.104682660304528f,
            0.158604703949607f,
            -0.255902581057323f,
            -0.325480510442599f
        };

        float[] elevations = new float[]
        {
            0.00712837955108657f,
            -0.0737631890991050f,
            -0.235052160624987f,
            -0.0957842433025755f
        };

        // 每个基站的安装姿态角度
        float[] APpitch = new float[] { 0f, 0f, 0f, 0f };
        float[] APyaw = new float[] { 135f, 45f, -60f, -110f };
        
        float azimuthOffsetDeg = 20f; // 正负20度
        float elevationOffsetDeg = 10f; 

        for (int i = 0; i < baseStations.Length; i++)
        {
            Vector3 baseStationPosition = baseStations[i].transform.position;

            for (int j = 0; j < raysPerBaseStation; j++)
            {
                // 计算该基站的 azimuth 角范围（单位：弧度）
                float azimuthDeg = Mathf.Rad2Deg * azimuths[i] + APyaw[i];
                // 添加一个随机数
                azimuthDeg += UnityEngine.Random.Range(-azimuthOffsetDeg, azimuthOffsetDeg);
                float azimuthRad =  Mathf.Deg2Rad * azimuthDeg;
                // 计算 elevation
                float elevationDeg = Mathf.Rad2Deg * elevations[i];
                // 添加一个随机数
                elevationDeg += UnityEngine.Random.Range(-elevationOffsetDeg, elevationOffsetDeg);
                float elevationRad =  Mathf.Deg2Rad * elevationDeg; 

                // 在局部坐标系下计算方向向量
                Vector3 worldDirection = SphericalToCartesianRad(azimuthRad, elevationRad);
                GameObject lineObject = new GameObject("Ray_" + i + "_" + j);
                lineObject.transform.parent = rayContainer.transform;

                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.startWidth = 0.05f;
                lineRenderer.endWidth = 0.05f;
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = baseStationColors[i];
                lineRenderer.endColor = baseStationColors[i];

                Ray ray = new Ray(baseStationPosition, worldDirection);
                RayData rayData = new RayData(ray, lineRenderer, i, baseStationColors[i], maxReflections);
                rays.Add(rayData);
            }

        }

        Debug.Log($"Total Rays Generated: {rays.Count}");
    }

    // Update a single ray
    void UpdateRay(RayData rayData)
    {
        if (rayData.reflectionCount >= maxReflections)
        {
            return; // Stop if the maximum number of reflections is reached
        }

        RaycastHit hitInfo;
        if (Physics.Raycast(rayData.ray, out hitInfo, Mathf.Infinity))
        {
            Debug.Log("Hit: " + hitInfo.collider.gameObject.name);
            rayData.AddHitPoint(hitInfo.point);

            // Draw the ray in the scene (for debugging)
            Debug.DrawRay(rayData.ray.origin, rayData.ray.direction * hitInfo.distance, rayData.rayColor, 10f);

            // Update the ray's origin to the hit point
            rayData.ray.origin = hitInfo.point;

            // Calculate reflection direction
            rayData.ray.direction = Vector3.Reflect(rayData.ray.direction, hitInfo.normal).normalized;

            // Set the positions for the LineRenderer
            rayData.lineRenderer.positionCount += 2;
            rayData.lineRenderer.SetPosition(rayData.reflectionCount * 2, hitInfo.point);
            rayData.lineRenderer.SetPosition(rayData.reflectionCount * 2 + 1, rayData.ray.origin);

            // Increment reflection count
            rayData.reflectionCount++;
        }
        else
        {
            // If no collision, just draw the ray
            Debug.DrawRay(rayData.ray.origin, rayData.ray.direction * 10f, rayData.rayColor, 10f);
        }
    }

    // Convert spherical coordinates to Cartesian coordinates
    public Vector3 SphericalToCartesian(float azimuth, float elevation)
    {
        float azimuthRad = azimuth * Mathf.Deg2Rad;  // Convert to radians
        float elevationRad = elevation * Mathf.Deg2Rad;  // Convert to radians

        // Calculate Cartesian coordinates
        float x = Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad);
        float y = Mathf.Sin(elevationRad);
        float z = Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad);

        return new Vector3(x, y, z);
    }

    public Vector3 SphericalToCartesianRad(float azimuthRad, float elevationRad)
    {
        float x = Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad);
        float y = Mathf.Sin(elevationRad);
        float z = Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad);
        return new Vector3(x, y, z);
    }

    public Vector3[] GetBaseStationPositions()
    {
        return baseStationPositions;
    }

    public List<RayData> GetRayData()
    {
        return rays;
    }

}
