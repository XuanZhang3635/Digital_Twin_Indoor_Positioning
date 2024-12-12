using System.Collections.Generic;
using UnityEngine;

public class MonteCarloRayTracing : MonoBehaviour
{
    public GameObject baseStationPrefab; // Base station prefab
    public int raysPerBaseStation = 1; // Number of rays emitted per base station
    public Vector2[] aoaRanges; // AoA range for each base station
    private GameObject[] baseStations; // Array of base station objects
    private Vector3[] baseStationPositions = new Vector3[] // Base station positions
    {
        new Vector3(0f, 3f, 0f),       // Base station 1
        new Vector3(15.14f, 3f, 0f),   // Base station 2
        new Vector3(15.14f, 3f, 12.8f),// Base station 3
        new Vector3(0f, 3f, 12.8f)     // Base station 4
    };

    private GameObject rayContainer; // Container for storing rays
    public int maxReflections = 5; // Maximum number of reflections

    private List<RayData> rays = new List<RayData>(); // List to store ray data

    void Start()
    {
        // Initialize base stations
        InitializeBaseStations();

        // Set AoA ranges (manually input)
        aoaRanges = new Vector2[]
        {
            new Vector2(-21.65f, 44.02f), // AoA range for base station 1
            new Vector2(-21.65f, 44.02f), // AoA range for base station 2
            new Vector2(-21.65f, 44.02f), // AoA range for base station 3
            new Vector2(-21.65f, 44.02f)  // AoA range for base station 4
        };

        // Initialize ray container
        rayContainer = new GameObject("RayContainer");

        // Initialize ray data
        InitializeRays();
    }

    void Update()
    {
        // Update each ray
        for (int i = 0; i < rays.Count; i++)
        {
            UpdateRay(rays[i]);
        }
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

        for (int i = 0; i < baseStations.Length; i++)
        {
            Vector3 baseStationPosition = baseStations[i].transform.position;
            Vector2 aoaRange = aoaRanges[i];

            for (int j = 0; j < raysPerBaseStation; j++)
            {
                // Randomly sample angles within the AoA range
                float azimuthAngle = Random.Range(aoaRange.x, aoaRange.y); // Azimuth angle
                float elevationAngle = Random.Range(-10f, 10f); // Elevation angle range

                // Convert angles to direction vector
                Vector3 direction = SphericalToCartesian(azimuthAngle, elevationAngle);

                // Create a new GameObject to hold the LineRenderer component
                GameObject lineObject = new GameObject("Ray_" + i + "_" + j);
                lineObject.transform.parent = rayContainer.transform; // Set parent object (organize rays)

                // Add LineRenderer component to each ray
                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();

                // Configure LineRenderer
                lineRenderer.startWidth = 0.05f;  // Start width
                lineRenderer.endWidth = 0.05f;    // End width
                lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Set material
                lineRenderer.startColor = baseStationColors[i]; // Set start color for the ray
                lineRenderer.endColor = baseStationColors[i];   // Set end color for the ray

                // Store ray data in the list
                rays.Add(new RayData
                {
                    ray = new Ray(baseStationPosition, direction),
                    lineRenderer = lineRenderer,
                    reflectionCount = 0,
                    baseStationColor = baseStationColors[i]
                });
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

            // Draw the ray in the scene (for debugging)
            Debug.DrawRay(rayData.ray.origin, rayData.ray.direction * hitInfo.distance, rayData.baseStationColor, 10f);

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
            Debug.DrawRay(rayData.ray.origin, rayData.ray.direction * 10f, rayData.baseStationColor, 10f);
        }
    }

    // Convert spherical coordinates to Cartesian coordinates
    Vector3 SphericalToCartesian(float azimuth, float elevation)
    {
        float azimuthRad = azimuth * Mathf.Deg2Rad;  // Convert to radians
        float elevationRad = elevation * Mathf.Deg2Rad;  // Convert to radians

        // Calculate Cartesian coordinates
        float x = Mathf.Cos(elevationRad) * Mathf.Cos(azimuthRad);
        float y = Mathf.Sin(elevationRad);
        float z = Mathf.Cos(elevationRad) * Mathf.Sin(azimuthRad);

        return new Vector3(x, y, z);
    }

    public Vector3[] GetBaseStationPositions()
    {
        return baseStationPositions;
    }

    // Ray data structure
    private class RayData
    {
        public Ray ray;
        public LineRenderer lineRenderer;
        public int reflectionCount;
        public Color baseStationColor;
    }
}
