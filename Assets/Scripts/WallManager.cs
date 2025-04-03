using UnityEngine;

public class WallManager : MonoBehaviour
{
    public GameObject wallPrefab; // The wall prefab
    public MonteCarloRayTracing rayTracingScript; // Reference to the MonteCarloRayTracing script
    public float wallHeight = 6f; // Wall height
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
        foreach(var loc in baseStationPositions)
        {
            Debug.Log("locations of BS:" + loc);
        }

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
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        float groundY = 0f; // Set the Y coordinate of the ground

        foreach (var pos in positions)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minZ = Mathf.Min(minZ, pos.z);
            maxZ = Mathf.Max(maxZ, pos.z);
        }

        minX -= padding;
        maxX += padding;
        minZ -= padding;
        maxZ += padding;

        float wallLengthX = maxX - minX;
        float wallLengthZ = maxZ - minZ;

        // Calculate the Y position of the bottom of the wall (ground position + half the height of the wall)
        float wallBaseY = groundY + (wallHeight / 2);
        float roofY = groundY + wallHeight + (wallThickness / 2); // Calculating Roof Height

        // Create four walls
        CreateWall(new Vector3((minX + maxX) / 2, wallBaseY, minZ), new Vector3(wallLengthX, wallHeight, wallThickness), "BackWall");
        CreateWall(new Vector3((minX + maxX) / 2, wallBaseY, maxZ), new Vector3(wallLengthX, wallHeight, wallThickness), "FrontWall");
        CreateWall(new Vector3(minX, wallBaseY, (minZ + maxZ) / 2), new Vector3(wallThickness, wallHeight, wallLengthZ), "LeftWall");
        CreateWall(new Vector3(maxX, wallBaseY, (minZ + maxZ) / 2), new Vector3(wallThickness, wallHeight, wallLengthZ), "RightWall");

        // Create the bottom floor, with its Y position aligned with the bottom of the four walls
        float floorThickness = wallThickness * 2; // Make the floor a little thicker
        float floorY = groundY - (floorThickness / 2); // Align the floor with the ground

        CreateWall(new Vector3((minX + maxX) / 2, floorY, (minZ + maxZ) / 2), 
                   new Vector3(wallLengthX, floorThickness, wallLengthZ), "Floor");

        CreateWall(new Vector3((minX + maxX) / 2, roofY, (minZ + maxZ) / 2),
                   new Vector3(wallLengthX, wallThickness, wallLengthZ), "Ceiling");
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

        // Ceiling 
        if (name == "Ceiling")
        {
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material transparentMaterial = new Material(Shader.Find("Standard"));
                transparentMaterial.color = new Color(0f, 0f, 0f, 0f);
                transparentMaterial.SetFloat("_Mode", 3);
                transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentMaterial.SetInt("_ZWrite", 0);
                transparentMaterial.DisableKeyword("_ALPHATEST_ON");
                transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
                transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMaterial.renderQueue = 3000;

                renderer.material = transparentMaterial;
            }
        }
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
