using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class RayTracing_MultiFrame : MonoBehaviour
{
    public GameObject baseStationPrefab;
    public int raysPerBaseStation = 10;
    public int maxReflections = 5;

    private float kappaAz;
    private float kappaEl;
    private float muAz;
    private float muEl;
    private class APParams
    {
        public string AP;
        public float Azimuth_Gauss_Mean_rad;
        public float Azimuth_Gauss_Std_rad;
        public float Azimuth_VM_Mean_rad;
        public float Azimuth_VM_Kappa;
        public float Elevation_Gauss_Mean_rad;
        public float Elevation_Gauss_Std_rad;
        public float Elevation_VM_Mean_rad;
        public float Elevation_VM_Kappa;
    }
    public string csvFileName = "angle_error_distribution_params_rad.csv"; 
    private static List<APParams> apList = new List<APParams>();

    private float kappa = 10f;

    // public List<float> proposalStepSize = new List<float>();
    // public List<float> sampleBoxSize = new List<float>();
    public int mcmcIterations = 200;
    public float proposalStepAz = 0.1f;
    public float proposalStepEl = 0.1f;

    private GameObject[] baseStations;
    private List<Vector3> baseStationPositions = new List<Vector3>();
    private List<float> APpitch = new List<float>();
    private List<float> APyaw = new List<float>();
    private List<List<float?>> azimuths = new List<List<float?>>();
    private List<List<float?>> elevations = new List<List<float?>>();
    private List<Vector3> ground_truth = new List<Vector3>();

    private GameObject rayContainer;
    private List<RayData> rays = new List<RayData>();
    private IntersectionLogger intersectionLogger;
    private IntersectionDetector intersectionDetector;
    private VoxelIntersectionCounter voxelCounter;

    private int currentMeasureIndex = 0;
    private int measureCnt = 0;
    private int APCnt = 0;

    private string outputFile = "Assets/Results/EstimateByIntersection.csv";
    private string basePath = "Assets/Results/";
    private int topK = 10;
    private string outputFile1;


    private float? aziMin = null, aziMax = null;
    private float? eleMin = null, eleMax = null;

    private List<(float az, float el)> samples = new List<(float az, float el)>();

    // 从建筑物模型roof获取mcmc采样点的坐标范围
    private float minX;
    private float maxX;
    private float minY;
    private float maxY;
    private float minZ;
    private float maxZ;

    private Vector3 center = Vector3.zero;

    void Start()
    {
        // outputFile1 = Path.Combine(basePath, "EstimateByVoxel_"+raysPerBaseStation+"_"+maxReflections+"_"+topK+".csv");
        outputFile1 = Path.Combine(basePath, "EstimateByVoxel_VM_MH_" + proposalStepAz + "_" + raysPerBaseStation + "_" + maxReflections + ".csv");

        GetLocationData();
        WriteHeaders();
        LoadAPParamsFromCSV();
        InitializeBaseStations();

        rayContainer = new GameObject("RayContainer");
        intersectionLogger = new IntersectionLogger();
        intersectionDetector = new IntersectionDetector(intersectionLogger);
        voxelCounter = new VoxelIntersectionCounter();

        GameObject roof = GameObject.Find("roof");
        if (roof != null)
        {
            MeshCollider mc = roof.GetComponent<MeshCollider>();
            if (mc == null) mc = roof.AddComponent<MeshCollider>();
            mc.convex = false;

            MeshFilter mf = roof.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) mc.sharedMesh = mf.sharedMesh;

            // 获得建筑物边界用作采样限制
            // 获取所有子 Renderer
            Renderer[] renderers = roof.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }

                minX = combinedBounds.min.x;
                maxX = combinedBounds.max.x;
                minY = combinedBounds.min.y;
                maxY = combinedBounds.max.y;
                minZ = combinedBounds.min.z;
                maxZ = combinedBounds.max.z;

                Debug.Log($"Combined bounds of roof:");
                Debug.Log($"X: {minX} ~ {maxX}");
                Debug.Log($"Y: {minY} ~ {maxY}");
                Debug.Log($"Z: {minZ} ~ {maxZ}");
            }
            else
            {
                Debug.LogWarning("No Renderers found in roof or its children.");
            }
        }

        StartCoroutine(RunSimulation());
    }

    IEnumerator RunSimulation()
    {
        while (currentMeasureIndex < measureCnt)
        {
            ClearRays();
            InitializeRays();

            yield return new WaitForSeconds(1.0f);

            voxelCounter.CountVoxels(rays);

            // intersectionLogger.SaveRaysToFile(rays);
            // intersectionDetector.DetectIntersections(rays);
            // intersectionLogger.SaveIntersectionsToFile();

            // Vector3 est = VoxelUtils.ComputeAveragePositionFromFile("Assets/FourRayIntersections.txt");
            // Debug.Log("Estimated position (four-line intersection): " + est);

            Vector3 est2 = VoxelUtils.ComputeWeightedPositionFromFile("Assets/voxel_hits.txt");

            // Vector3 est2 = VoxelUtils.ComputeStableEstimateFromFile("Assets/voxel_hits.txt",topK);

            // Vector3 est2 = voxelCounter.ComputeLocalWeightedCentroid();

            // Vector3 est2 = KDEEstimator.EstimateByKDEFromVoxelFile("Assets/voxel_hits.txt", 0.5f);

            // Vector3 est2 = VoxelEstimator.ComputeFilteredWeightedCentroid("Assets/voxel_hits.txt",0.8f);


            // Compute the error
            // float errorIntersection = Vector3.Distance(ground_truth[currentMeasureIndex], est);
            float errorVoxel = Vector3.Distance(ground_truth[currentMeasureIndex], est2);
            Debug.Log("error of voxel:" + errorVoxel);
            // Save the results to CSV files
            // SaveResult(outputFile, ground_truth[currentMeasureIndex], est, errorIntersection);
            SaveResult(outputFile1, ground_truth[currentMeasureIndex], est2, errorVoxel);

            currentMeasureIndex++;
        }

        Debug.Log("All measurements completed!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
    }

    void LoadAPParamsFromCSV()
    {
        string filePath = Path.Combine(Application.dataPath, csvFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string[] tokens = lines[i].Split(',');

            if (tokens.Length < 5) continue;

            APParams ap = new APParams
            {
                AP = tokens[0],
                Azimuth_Gauss_Mean_rad = float.Parse(tokens[1]),
                Azimuth_Gauss_Std_rad = float.Parse(tokens[2]),
                Azimuth_VM_Mean_rad = float.Parse(tokens[3]),
                Azimuth_VM_Kappa = float.Parse(tokens[4]),
                Elevation_Gauss_Mean_rad = float.Parse(tokens[5]),
                Elevation_Gauss_Std_rad = float.Parse(tokens[6]),
                Elevation_VM_Mean_rad = float.Parse(tokens[7]),
                Elevation_VM_Kappa = float.Parse(tokens[8]),
            };
            apList.Add(ap);
        }

        Debug.Log($"✅ Loaded {apList.Count} AP entries from CSV.");
    }
    void ClearRays()
    {
        rays.Clear();
        foreach (Transform child in rayContainer.transform) Destroy(child.gameObject);
    }

    void InitializeBaseStations()
    {
        baseStations = new GameObject[baseStationPositions.Count];
        for (int i = 0; i < baseStationPositions.Count; i++)
        {
            baseStations[i] = Instantiate(baseStationPrefab, baseStationPositions[i], Quaternion.identity);
            baseStations[i].name = "BaseStation" + (i + 1);

            // 添加碰撞器（如果没有的话）
            if (baseStations[i].GetComponent<Collider>() == null)
            {
                baseStations[i].AddComponent<BoxCollider>();
            }
        }

        // 计算四个AP的中心点
        foreach (var pos in baseStationPositions)
        {
            center += pos;
        }
        center /= baseStationPositions.Count;
    }

    void InitializeRays()
    {
        Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };

        for (int i = 0; i < baseStations.Length; i++)
        {
            samples = SamplePositions(i);
            // ShowSamplePoints(i, samples, isAngleSample: false, length: 3f);  
            Vector3 pos = baseStations[i].transform.position;

            for (int j = 0; j < samples.Count; j++)
            {   
                (float az, float el) = samples[j];  // az/el 单位是弧度
                float azWithYaw = az + Mathf.Deg2Rad * APyaw[i]; // 弧度 + 弧度
                Vector3 dir = SphericalToCartesian(azWithYaw, el);

                GameObject lineObject = new GameObject("Ray_" + i + "_" + j);
                lineObject.transform.parent = rayContainer.transform;

                LineRenderer lr = lineObject.AddComponent<LineRenderer>();
                lr.startWidth = lr.endWidth = 0.02f;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = lr.endColor = colors[i % colors.Length];

                // ✅ 正确设置 LineRenderer 的世界坐标点
                // lr.positionCount = 2;
                // lr.SetPosition(0, pos); // 起点：基站位置
                // lr.SetPosition(1, pos + dir * 500f); // 终点：延着方向延伸

                Ray ray = new Ray(pos, dir);
                rays.Add(new RayData(ray, lr, i, colors[i % colors.Length], maxReflections));
            }
        }
    }

    List<(float az, float el)> SamplePositions(int baseIndex)
    {
        List<(float az, float el)> samples = new List<(float az, float el)>();
        float az_curr = (azimuths[currentMeasureIndex][baseIndex] ?? 0f) + apList[baseIndex].Azimuth_VM_Mean_rad;
        float el_curr = (elevations[currentMeasureIndex][baseIndex] ?? 0f) + apList[baseIndex].Elevation_VM_Mean_rad;
        float currentProb = PosteriorAzEl(az_curr, el_curr, baseIndex);
        int accepted = 0;
        for (int i = 0; i < mcmcIterations; i++)
        {
            float az_prop = az_curr + GaussianRandom(0,proposalStepAz);
            float el_prop = el_curr + GaussianRandom(0,proposalStepEl);

            float proposalProb = PosteriorAzEl(az_prop, el_prop, baseIndex);

            float acceptance = Mathf.Min(1f, proposalProb / (currentProb + 1e-9f));
            if (UnityEngine.Random.value < acceptance)
            {
                az_curr = az_prop;
                el_curr = el_prop;
                currentProb = proposalProb;
                accepted++;
            }

            if (samples.Count < raysPerBaseStation)
                samples.Add((az_curr, el_curr));

        }
        Debug.Log("Acceptance Rate = " + (float)accepted / mcmcIterations);

        return samples;
    }

    // VM
    float PosteriorAzEl(float az, float el, int baseIndex)
    {
        float muAz = apList[baseIndex].Azimuth_VM_Mean_rad +(azimuths[currentMeasureIndex][baseIndex] ?? 0f);
        float muEl = apList[baseIndex].Elevation_VM_Mean_rad + (elevations[currentMeasureIndex][baseIndex] ?? 0f);
        // float kappaAz = apList[baseIndex].azimuthKappa;
        // float kappaEl = apList[baseIndex].elevationKappa;

        float logAz = VonMisesLogPDF(az, muAz, apList[baseIndex].Azimuth_VM_Kappa);
        float logEl = VonMisesLogPDF(el, muEl, apList[baseIndex].Elevation_VM_Kappa);

        return Mathf.Exp(logAz + logEl); // 返回联合概率（非对数）
    }


    public static float GaussianRandom(float mean, float stdDev)
    {
        float u1 = 1.0f - UnityEngine.Random.value; // 防止 log(0)
        float u2 = 1.0f - UnityEngine.Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        return mean + stdDev * randStdNormal;
    }

    Vector3 SphericalToCartesianRad(float az, float el)
    {
        float x = Mathf.Cos(el) * Mathf.Cos(az);
        float y = Mathf.Sin(el);
        float z = Mathf.Cos(el) * Mathf.Sin(az);
        return new Vector3(x, y, z);
    }

    void Update()
    {
        foreach (var rayData in rays) UpdateRay(rayData);
    }

    void UpdateRay(RayData rayData)
    {
        if (rayData.reflectionCount >= maxReflections) return;

        Vector3 startPoint = rayData.ray.origin; // 原始起点
        if (Physics.Raycast(rayData.ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            Vector3 hitPoint = hitInfo.point;

            // 先设置当前线段
            int index = rayData.lineRenderer.positionCount;
            rayData.lineRenderer.positionCount += 2;
            rayData.lineRenderer.SetPosition(index, startPoint);
            rayData.lineRenderer.SetPosition(index + 1, hitPoint);
            rayData.lineRenderer.useWorldSpace = true;

            // 更新射线方向和起点
            rayData.ray.origin = hitPoint;
            rayData.ray.direction = Vector3.Reflect(rayData.ray.direction, hitInfo.normal).normalized;

            rayData.reflectionCount++;
        }
    }

    // VMF
    // float Posterior(Vector3 x, int baseIndex)
    // {
    //     Vector3 bs = baseStations[baseIndex].transform.position;
    //     Vector3 dir = (x - bs).normalized;

    //     Vector3 mu = GetObservedDirection(baseIndex); // 观测方向（单位向量）
    //     if (mu == Vector3.zero) return 0f;

    //     float dot = Vector3.Dot(mu, dir); // cos(夹角)
    //     float logProb = kappa * dot; // 忽略常数项
    //     return Mathf.Exp(logProb);
    // }

    // Vector3 GetObservedDirection(int baseIndex)
    // {
    //     float? az = azimuths[currentMeasureIndex][baseIndex];
    //     float? el = elevations[currentMeasureIndex][baseIndex];
    //     if (!az.HasValue || !el.HasValue) return Vector3.zero;

    //     float azR = az.Value; // 已是弧度
    //     float elR = el.Value;

    //     float x = Mathf.Cos(elR) * Mathf.Cos(azR);
    //     float y = Mathf.Cos(elR) * Mathf.Sin(azR);
    //     float z = Mathf.Sin(elR);
    //     return new Vector3(x, y, z).normalized;
    // }
    // VMF end

    float WrapToPi(float angle)
    {
        while (angle > Mathf.PI) angle -= 2 * Mathf.PI;
        while (angle < -Mathf.PI) angle += 2 * Mathf.PI;
        return angle;
    }

    float VonMisesLogPDF(float theta, float mu, float kappa)
    {
        return kappa * Mathf.Cos(theta - mu) - Mathf.Log(2 * Mathf.PI * BesselI0(kappa));
    }
  
    float BesselI0(float x)
    {
        float sum = 1f;
        float term = 1f;
        for (int k = 1; k < 10; k++)
        {
            term *= (x * x) / (4f * k * k);
            sum += term;
        }
        return sum;
    }

    (float, float) CartesianToSpherical(Vector3 dir)
    {
        float az = Mathf.Atan2(dir.z, dir.x);
        float el = Mathf.Asin(dir.y);
        return (az, el);
    }

    Vector3 SphericalToCartesian(float az, float el)
    {
        float x = Mathf.Cos(el) * Mathf.Cos(az);
        float y = Mathf.Sin(el);
        float z = Mathf.Cos(el) * Mathf.Sin(az);
        return new Vector3(x, y, z);
    }

    float GaussianLogPDF(float x, float mu, float sigma)
    {
        float diff = x - mu;
        return -0.5f * Mathf.Log(2f * Mathf.PI * sigma * sigma) - (diff * diff) / (2f * sigma * sigma);
    }

    void OnDrawGizmos()
    {
        // if (samples != null)
        // {
        //     Gizmos.color = Color.red;
        //     foreach (var s in samples)
        //     {
        //         Gizmos.DrawSphere(s, 0.1f);
        //     }
        // }

        // if (ground_truth != null)
        // {
        //     Gizmos.color = Color.green;
        //     foreach (var gt in ground_truth)
        //     {
        //         Gizmos.DrawSphere(gt, 0.1f); // 绿色点，表示真实位置
        //     }
        // }

    }

    // Write headers to CSV files if they don't exist
    void WriteHeaders()
    {
        // if (File.Exists(outputFile))
        // {
        //     File.Delete(outputFile);
        // }

        // using (StreamWriter sw = new StreamWriter(outputFile, false))
        // {
        //     sw.WriteLine("GT_X,GT_Y,GT_Z,EST_X,EST_Y,EST_Z,Error");
        // }

        if (File.Exists(outputFile1))
        {
            File.Delete(outputFile1);
        }

        using (StreamWriter sw = new StreamWriter(outputFile1, false))
        {
            sw.WriteLine("GT_X,GT_Y,GT_Z,EST_X,EST_Y,EST_Z,Error");
        }
    }

    void SaveResult(string path, Vector3 gt, Vector3 est, float error)
    {
        using (StreamWriter sw = new StreamWriter(path, true))
        {
            sw.WriteLine($"{gt.x:F3},{gt.y:F3},{gt.z:F3},{est.x:F3},{est.y:F3},{est.z:F3},{error:F3}");
        }
    }

    void GetLocationData()
    {
        var data = GlobalDataManager.Instance.Data;
        APCnt = data.AP[0].Count;
        measureCnt = data.AoA[0].Count;

        for (int i = 0; i < APCnt; i++)
        {
            baseStationPositions.Add(new Vector3(data.AP[0][i], data.AP[2][i], data.AP[1][i]));
            APyaw.Add(data.APyaw[i]);
            APpitch.Add(data.APpitch[i]);
        }

        for (int j = 0; j < measureCnt; j++)
        {
            List<float?> aziRow = new List<float?>();
            List<float?> eleRow = new List<float?>();

            for (int m = 0; m < APCnt; m++)
            {
                float? val = data.AoA[m][j] ?? 0f;
                aziRow.Add(val);
                if (val.HasValue)
                {
                    if (aziMin == null || val < aziMin) aziMin = val;
                    if (aziMax == null || val > aziMax) aziMax = val;
                }
            }

            for (int m = APCnt; m < 2 * APCnt; m++)
            {
                float? val = data.AoA[m][j] ?? 0f;
                eleRow.Add(val);
                if (val.HasValue)
                {
                    if (eleMin == null || val < eleMin) eleMin = val;
                    if (eleMax == null || val > eleMax) eleMax = val;
                }
            }

            azimuths.Add(aziRow);
            elevations.Add(eleRow);
        }

        // Debug.Log($"Azimuth Range: [{aziMin}, {aziMax}]");
        // Debug.Log($"Elevation Range: [{eleMin}, {eleMax}]");

        for (int k = 0; k < measureCnt; k++)
        {
            ground_truth.Add(new Vector3(data.ground_truth[0][k], data.ground_truth[2][k], data.ground_truth[1][k]));
        }
    }

    public void ShowSamplePoints<T>(int baseIndex, List<T> samples, bool isAngleSample = false, float length = 3f)
    {
        Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };
        Color c = colors[baseIndex % colors.Length];
        Transform parent = rayContainer != null ? rayContainer.transform : null;

        Vector3 bsPos = baseStations[baseIndex].transform.position;

        for (int i = 0; i < samples.Count; i++)
        {
            Vector3 pos;

            if (!isAngleSample && samples[i] is Vector3 vec)
            {
                pos = vec;
            }
            else if (isAngleSample && samples[i] is ValueTuple<float, float> angles)
            {
                Vector3 dir = SphericalToCartesian(angles.Item1, angles.Item2);
                pos = bsPos + dir * length;
            }
            else
            {
                Debug.LogWarning("Unsupported sample type or mismatched flag.");
                continue;
            }

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = pos;
            sphere.transform.localScale = Vector3.one * 0.1f;
            sphere.GetComponent<Renderer>().material.color = c;
            sphere.name = $"Sample_BS{baseIndex}_{i}";
            if (parent != null) sphere.transform.parent = parent;
        }
    }

}
