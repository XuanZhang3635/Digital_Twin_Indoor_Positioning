using UnityEngine;

public class WallManager : MonoBehaviour
{
    public GameObject wallPrefab; // The wall prefab
    public MonteCarloRayTracing rayTracingScript; // Reference to the MonteCarloRayTracing script
    public float wallHeight = 3f; // Wall height
    public float wallThickness = 0.1f; // Wall thickness
    public float padding = 1f; // Padding between the wall and the base station boundaries

    void Start()
    {
        // Check if the RayTracing script is assigned
        if (rayTracingScript == null)
        {
            Debug.LogError("RayTracing script not assigned!");
            return;
        }

        // Get base station positions
        Vector3[] baseStationPositions = rayTracingScript.GetBaseStationPositions();

        if (baseStationPositions == null || baseStationPositions.Length == 0)
        {
            Debug.LogError("No base station positions found!");
            return;
        }

        // Calculate the wall boundaries around the base stations
        CreateWallsAroundBaseStations(baseStationPositions);
    }

    // Create walls around the base stations
    private void CreateWallsAroundBaseStations(Vector3[] positions)
    {
        // Initialize boundary values
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        // Loop through base station positions to determine the boundaries
        foreach (var pos in positions)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minZ = Mathf.Min(minZ, pos.z);
            maxZ = Mathf.Max(maxZ, pos.z);
        }

        // Add padding to the boundaries
        minX -= padding;
        maxX += padding;
        minZ -= padding;
        maxZ += padding;

        // Calculate the dimensions of the walls
        float wallLengthX = maxX - minX; // Horizontal length
        float wallLengthZ = maxZ - minZ; // Vertical length

        // Create the walls
        CreateWall(new Vector3((minX + maxX) / 2, wallHeight / 2, minZ), new Vector3(wallLengthX, wallHeight, wallThickness), "BackWall");
        CreateWall(new Vector3((minX + maxX) / 2, wallHeight / 2, maxZ), new Vector3(wallLengthX, wallHeight, wallThickness), "FrontWall");
        CreateWall(new Vector3(minX, wallHeight / 2, (minZ + maxZ) / 2), new Vector3(wallThickness, wallHeight, wallLengthZ), "LeftWall");
        CreateWall(new Vector3(maxX, wallHeight / 2, (minZ + maxZ) / 2), new Vector3(wallThickness, wallHeight, wallLengthZ), "RightWall");
    }

    // Create a single wall
    private void CreateWall(Vector3 position, Vector3 scale, string name)
    {
        if (wallPrefab == null)
        {
            Debug.LogError("Wall Prefab not assigned!");
            return;
        }

        GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
        wall.transform.localScale = scale;
        wall.name = name;

        // Check and add BoxCollider if missing
        BoxCollider collider = wall.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = wall.AddComponent<BoxCollider>();
        }

        // Set the physics material for the collider
        PhysicsMaterial physicsMaterial = new PhysicsMaterial("WallMaterial")
        {
            bounciness = 1.0f,  // Bounciness
            dynamicFriction = 0.0f,  // Dynamic friction
            staticFriction = 0.0f  // Static friction
        };
        collider.material = physicsMaterial;

        // Set the wall's layer to the default layer
        wall.layer = LayerMask.NameToLayer("Default");
    }
}
