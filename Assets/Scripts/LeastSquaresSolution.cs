using UnityEngine;
using System.IO;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Single;

public class LeastSquaresSolution : MonoBehaviour
{
    private List<UnityEngine.Vector3> baseStationPositions = new List<UnityEngine.Vector3>(); // Base station positions
    private List<float> APpitch = new List<float>();
    private List<float> APyaw = new List<float>();
    private List<List<float?>> azimuths = new List<List<float?>>();
    private List<List<float?>> elevations = new List<List<float?>>();
    private List<UnityEngine.Vector3> ground_truth = new List<UnityEngine.Vector3>();
    private int APCnt = 0;
    private int measureCnt = 0;

    private LocalizationData localizationData;

    public GameObject baseStationPrefab;
    public string outputFile = "Assets/Results/LeastSquaresSolution.csv";
    private bool headerWritten = false;

    void Start()
    {
        GetLocationData();
        for (int i = 0; i < baseStationPositions.Count; i++)
        {
            GameObject newAP = Instantiate(baseStationPrefab, baseStationPositions[i], Quaternion.identity);
            newAP.name = "AP_" + i;
        }
        for(int m=0;m<measureCnt;m++)
        {
            Matrix<float> H = DenseMatrix.Create(2 * baseStationPositions.Count, 3, 0f);
            Vector<float> Y = DenseVector.Create(2 * baseStationPositions.Count, 0f);

            for (int i = 0; i < baseStationPositions.Count; i++)
            {
                float azi = Mathf.Deg2Rad * APyaw[i] + azimuths[m][i]?? 0f;
                float ele = elevations[m][i] ?? 0f;
                if (float.IsNaN(azi) || float.IsNaN(ele))
                    continue;
                // azimuth_angles += randn * 1e-2;
                // elevation_angles += randn * 1e-2;

                float px = baseStationPositions[i].x;
                float py = baseStationPositions[i].z;
                float pz = baseStationPositions[i].y;

                // H matrix rows
                H[2 * i, 0] = Mathf.Sin(azi);
                H[2 * i, 1] = -Mathf.Cos(azi);
                H[2 * i, 2] = 0;

                H[2 * i + 1, 0] = Mathf.Cos(azi) * Mathf.Sin(ele);
                H[2 * i + 1, 1] = Mathf.Sin(azi) * Mathf.Sin(ele);
                H[2 * i + 1, 2] = -Mathf.Cos(ele);

                // Y vector
                Y[2 * i] = px * Mathf.Sin(azi) - py * Mathf.Cos(azi);
                Y[2 * i + 1] = px * Mathf.Cos(azi) * Mathf.Sin(ele)
                                + py * Mathf.Sin(azi) * Mathf.Sin(ele)
                                - pz * Mathf.Cos(ele);

            }

            // Least squares solution：X = pinv(H) * Y
            Vector3 estimatedPosition = SolveLeastSquares(H, Y);
            Debug.Log("Estimated position: " + estimatedPosition);
            ShowEstimatedPoint(estimatedPosition,m);

            if (!headerWritten && !File.Exists(outputFile))
            {
                using (StreamWriter sw = new StreamWriter(outputFile, false))
                {
                    sw.WriteLine("GT_X,GT_Y,GT_Z,EST_X,EST_Y,EST_Z,Error");
                }
                headerWritten = true;
            }

            using (StreamWriter sw = new StreamWriter(outputFile, true))
            {
                float error = Vector3.Distance(ground_truth[m], estimatedPosition);
                sw.WriteLine($"{ground_truth[m].x:F3},{ground_truth[m].y:F3},{ground_truth[m].z:F3}," +
                            $"{estimatedPosition.x:F3},{estimatedPosition.y:F3},{estimatedPosition.z:F3},{error:F3}");
            }
        }

    }

    Vector3 SolveLeastSquares(Matrix<float> H, Vector<float> Y)
    {
        var Hpinv = H.PseudoInverse();
        var result = Hpinv * Y;
        return new Vector3(result[0], result[2], result[1]);
    }

    void ShowEstimatedPoint(Vector3 estimatedPosition,int i)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.position = estimatedPosition;
        // Scale
        cube.transform.localScale = Vector3.one * 0.2f;
        // Color
        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material.color = Color.yellow;
        // Name
        cube.name = "EstimatedPositionCube_" + i;
    }

    void GetLocationData()
    {
        localizationData = GlobalDataManager.Instance.Data;
        APCnt = localizationData.AP[0].Count;
        measureCnt = localizationData.AoA[0].Count;
        // baseStationPositions
        for(int i=0;i<APCnt;i++)
        {
            UnityEngine.Vector3 vector = new UnityEngine.Vector3();
            vector.x = localizationData.AP[0][i];
            vector.y = localizationData.AP[2][i];
            vector.z = localizationData.AP[1][i];
            baseStationPositions.Add(vector);

            APyaw.Add(localizationData.APyaw[i]);
            APpitch.Add(localizationData.APpitch[i]);
        }

        for(int j=0;j<measureCnt;j++)
        {
            // azimuths
            List<float?> item1 = new List<float?>();
            for(int m=0;m<APCnt;m++)
            {
                item1.Add(localizationData.AoA[m][j]);
            }
            azimuths.Add(item1);
            // elevations
            List<float?> item2 = new List<float?>();
            for(int n=APCnt;n<APCnt*2;n++)
            {
                item2.Add(localizationData.AoA[n][j]);
            }
            elevations.Add(item2);
        }

        // GT
        for(int k=0;k<measureCnt;k++)
        {
            UnityEngine.Vector3 vc = new UnityEngine.Vector3();
            vc.x = localizationData.ground_truth[0][k];
            vc.y = localizationData.ground_truth[2][k];
            vc.z = localizationData.ground_truth[1][k];
            ground_truth.Add(vc);
        }
        
    }
    
}
