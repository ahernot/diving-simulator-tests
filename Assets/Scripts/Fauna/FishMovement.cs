/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 FishMovement v1.3
*/

// TODO: new random movement frequency selector
// TODO: cone of headings, with random direction chosen (to restrict angle) –> Gaussian probability, with backwards still possible but way less likely
// TODO: adjust speed setting to be the only setting available >> this.heading normalized, * this.speed
// TODO: increase répulsion aux parois, decrease répulsion en haut de l'eau
// TODO: add offset for rocks
// TODO: work with forces&accelerations instead of speeds

// TODO: add a max speed parameter
// TODO: add a getter in TerrainChunkManager to know current player chunk, and nearest vertex position

/*
* Special instructions:
*   If new objects are added, run FishMovement.LocateObjects (false);
* 
* Information:
*   The water level, terrain, and object boundaries are hard boundaries
*   The bounding box boundaries are flexible
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FishMovement : MonoBehaviour
{

    // Random heading
    Vector3 minHeading;
    Vector3 maxHeading;

    // Repulsion: bounding box
    [Header("Movement boundaries")]
    [Tooltip("Min boundaries on the x and z axes")]
    public Vector2 minCoordinates;
    [Tooltip("Max boundaries on the x and z axes")]
    public Vector2 maxCoordinates;
    public float boundaryRepulsionMultiplier = 100f;
    public float boundaryRepulsionDistance = 0.1f;

    // Repulsion: water level
    [Tooltip("Water level")]
    public float waterHeight;
    public float waterSurfaceRepulsionMultiplier = 100f;
    public float waterSurfaceRepulsionDistance = 1f;

    [Tooltip("Terrain")]
    public GameObject terrainChunkManager;

    // Repulsion: layers
    [Header("Repulsion Layers")]
    [Tooltip("Static repulsive objects")]
    public RepulsionLayer[] repulsionLayersStatic;
    [Tooltip("Dynamic repulsive objects (position recalculatedd each frame)")]
    public RepulsionLayer[] repulsionLayersDynamic;
    // Repulsion function parameters
    public float globalRepulsionMultiplier = 100f;
    

    // Speed
    public float speed = 2f;
    public float repulsionSpeed = 10f;


    Vector3 acceleration;
    Vector3 velocity; // replaces heading
    // do not bother with rotation inertia for now



    [Header("Current heading")]
    [SerializeField]
    Vector3 heading;

    // Force vectors
    [SerializeField]
    Vector3 objectsRepulsion;
    [SerializeField]
    Vector3 boundaryRepulsion;
    [SerializeField]
    Vector3 terrainRepulsion;
    [SerializeField]
    Vector3 seaLevelRepulsion;
    

    // Optimised lists
    List<GameObject[]> repulsionObjectsStatic = new List<GameObject[]>();
    List<float> repulsionRadiiStatic = new List<float>();
    List<GameObject[]> repulsionObjectsDynamic = new List<GameObject[]>();
    List<float> repulsionRadiiDynamic = new List<float>();


    // Gizmos (debug mode)
    [Tooltip("Enable Gizmos visualisations")]
    public bool debugMode;
    Mesh boundariesMesh;
    Mesh topMesh;



    void Start ()
    {
        // Generate gizmo meshes
        if (this.debugMode)
        {
            this.GenerateBoundariesMesh();
            this.GenerateTopMesh();
        }

        // Set random heading parameters
        this.minHeading = new Vector3 (-10f, -10f, -10f);
        this.maxHeading = new Vector3 (10f, 10f, 10f);

        // Locate all objects
        this.LocateObjects (true);

        // Set initial heading
        this.ChangeHeading();
    }

    void Update ()
    {
        // Pick a new direction (0.2% chance per frame)
        float rd = UnityEngine.Random.Range(0, 999);
        if (rd <= 2) { this.ChangeHeading(); }
        // this.heading = new Vector3(); // Reset heading

        // Calculate repulsion vectors
        this.CalculateObjectsRepulsion();
        this.CalculateSeaLevelRepulsion();
        // this.CalculateBoundaryRepulsion();

        // Apply objects repulsion vector
        if (!float.IsNaN(this.objectsRepulsion.x) && !float.IsNaN(this.objectsRepulsion.y) && !float.IsNaN(this.objectsRepulsion.z)) {
            this.heading += this.objectsRepulsion;
        }
        // Apply sea level repulsion vector
        if (!float.IsNaN(this.seaLevelRepulsion.x) && !float.IsNaN(this.seaLevelRepulsion.y) && !float.IsNaN(this.seaLevelRepulsion.z)) {
            this.heading += this.seaLevelRepulsion;// * 0.001f;
        }
        // Apply boundary repulsion vector
        if (!float.IsNaN(this.boundaryRepulsion.x) && !float.IsNaN(this.boundaryRepulsion.y) && !float.IsNaN(this.boundaryRepulsion.z)) {
            this.heading += this.boundaryRepulsion;// * 0.001f;
        }
        // Apply terrain repulsion vector (NOT IMPLEMENTED)
        if (!float.IsNaN(this.terrainRepulsion.x) && !float.IsNaN(this.terrainRepulsion.y) && !float.IsNaN(this.terrainRepulsion.z)) {
            this.heading += this.terrainRepulsion;
        }

        // Move this.gameObject
        // TODO: Apply fish rotation and animation here too, using (Vector3)this.heading
        transform.position += this.heading * Time.deltaTime;
        //transform.rotation = Quaternion.Euler();



        // Generate gizmo meshes if debug mode toggled on during runtime
        if ((this.debugMode) && (this.topMesh == null)) { this.GenerateTopMesh(); };
        if ((this.debugMode) && (this.boundariesMesh == null)) { this.GenerateBoundariesMesh(); };
    }

    // TODO: change within a cone
    void ChangeHeading ()
    {
        // Generate random vector
        Vector3 randomHeading = new Vector3 (
            UnityEngine.Random.Range(minHeading.x, maxHeading.x),
            UnityEngine.Random.Range(minHeading.y, maxHeading.y),
            UnityEngine.Random.Range(minHeading.z, maxHeading.z)
        );

        // Set heading
        this.heading = this.speed * randomHeading / randomHeading.magnitude;
    }


    /**
    * Locate all repulsive objects in the scene and store them in memory
    */
    void LocateObjects (bool locateStaticObjects)
    {
        // Locate static repulsive objects (only called once, because static duh)
        if (locateStaticObjects)
        {
            for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
            {
                // Get repulsionLayer
                RepulsionLayer repulsionLayer = this.repulsionLayersStatic [i];

                // Write repulsion radius
                this.repulsionRadiiStatic.Add (repulsionLayer.repulsionRadius);

                // Get GameObjects
                GameObject[] repulsionObjects = GameFunctions.FindGameObjectsWithLayer (repulsionLayer.layerId);

                // Add GameObject[] to list
                this.repulsionObjectsStatic.Add (repulsionObjects);   
            }
        }

        // Locate dynamic repulsive objects
        for (int i = 0; i < this.repulsionLayersDynamic.Length; i ++)
        {
            // Get repulsionLayer
            RepulsionLayer repulsionLayer = this.repulsionLayersDynamic [i];

            // Write repulsion radius
            this.repulsionRadiiDynamic.Add (repulsionLayer.repulsionRadius);

            // Get GameObjects
            GameObject[] repulsionObjects = GameFunctions.FindGameObjectsWithLayer (repulsionLayer.layerId);

            // Add GameObject[] to list
                this.repulsionObjectsDynamic.Add (repulsionObjects); 
        }
    }


    /**
    * Calculate the object repulsion force vectors
    */
    void CalculateObjectsRepulsion ()
    {
        // Initialise objectsRepulsion vector
        this.objectsRepulsion = new Vector3();


        // Static objects' repulsion
        for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
        {
            // Get GameObject[] and repulsionRadius
            float repulsionRadius = this.repulsionRadiiStatic [i];
            GameObject[] repulsionObjects = this.repulsionObjectsStatic [i];

            // Run through objects' coordinates
            for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
            {
                // Compute direction from repulsive object to current GameObject
                Vector3 repulsionForce = transform.position - repulsionObjects[objectId].transform.position;
                float distance = repulsionForce.magnitude;

                // Calculate repulsion force and vector (if not too far away)
                if (distance <= repulsionRadius * 10)
                {
                    // Apply repulsion force multipiler (based on distance)
                    float repulsionMult = GameFunctions.HardRepulsionFunction (this.globalRepulsionMultiplier, repulsionRadius, distance);
                    // if (distance < repulsionRadius) {
                    //     repulsionMult = this.repulsionAmplitude;
                    // } else {
                    //     repulsionMult = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * (distance - repulsionRadius) / repulsionRadius);  
                    // };
                    repulsionForce = (repulsionForce / distance) * repulsionMult;

                    // Add to global repulsion vector
                    this.objectsRepulsion += repulsionForce;
                }   
            } 
        }

        // Dynamic objects' repulsion
        for (int i = 0; i < this.repulsionLayersDynamic.Length; i ++)
        {
            // Get replusion radius
            float repulsionRadius = this.repulsionRadiiDynamic [i];
            GameObject[] repulsionObjects = this.repulsionObjectsDynamic [i];

            // Run through objects' coordinates
            for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
            {
                // Compute direction from repulsive object to current GameObject
                Vector3 repulsionForce = transform.position - repulsionObjects[objectId].transform.position;
                float distance = repulsionForce.magnitude;

                // Calculate repulsion force and vector (if not too far away)
                if (distance <= repulsionRadius * 10)
                {
                    // Apply repulsion force multipiler (based on distance)
                    float repulsionMult = GameFunctions.HardRepulsionFunction (this.globalRepulsionMultiplier, repulsionRadius, distance);
                    // if (distance < repulsionRadius) {
                    //     repulsionMult = this.repulsionAmplitude;
                    // } else {
                    //     repulsionMult = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * (distance - repulsionRadius) / repulsionRadius);  
                    // };
                    repulsionForce = (repulsionForce / distance) * repulsionMult;

                    // Add to global repulsion vector
                    this.objectsRepulsion += repulsionForce;
                }
            }
        }

    }

    /**
    * Calculate the lateral boundary repulsion force vectors
    */
    void CalculateBoundaryRepulsion ()
    {
        // Initialise boundaryRepulsion vector
        this.boundaryRepulsion = new Vector3();

        // Calculate distances to boundaries
        float minXDistanceSigned = transform.position.x - this.minCoordinates.x;
        float maxXDistanceSigned = this.maxCoordinates.x - transform.position.x;
        float minZDistanceSigned = transform.position.z - this.minCoordinates.y;
        float maxZDistanceSigned = this.maxCoordinates.y - transform.position.z;

        // Min X
        if (minXDistanceSigned <= boundaryRepulsionDistance * 10)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (1f, 0f, 0f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, minXDistanceSigned);
            // if (minXDistanceSigned < boundaryRepulsionDistance) {
            //     repulsionMult = this.repulsionAmplitude;
            // } else {
            //     repulsionMult = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * minXDistanceSigned / boundaryRepulsionDistance);
            // };
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionForce;
        }
        
        // Max X
        else if (maxXDistanceSigned <= boundaryRepulsionDistance * 10)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (-1f, 0f, 0f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, maxXDistanceSigned);
            // if (maxXDistanceSigned < boundaryRepulsionDistance) {
            //     repulsionMult = this.repulsionAmplitude;
            // } else {
            //     repulsionMult = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * maxXDistanceSigned / boundaryRepulsionDistance);
            // };
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionForce;
        }

        // Min Z
        if (minZDistanceSigned <= boundaryRepulsionDistance * 10)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (0f, 0f, 1f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, minZDistanceSigned);
            // if (minZDistanceSigned < boundaryRepulsionDistance) {
            //     repulsionMult = this.repulsionAmplitude;
            // } else {
            //     repulsionMult = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * minZDistanceSigned / boundaryRepulsionDistance);
            // };
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionForce;
        }
        
        // Max Z
        else if (maxZDistanceSigned <= boundaryRepulsionDistance * 10)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (0f, 0f, -1f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, maxZDistanceSigned);
            // if (maxZDistanceSigned < boundaryRepulsionDistance) {
            //     repulsionMult = this.repulsionAmplitude;
            // } else {
            //     repulsionMult = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * maxZDistanceSigned / boundaryRepulsionDistance);
            // };
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionForce;
        }
    }

    /**
    * Calculate the water surface repulsion force vector
    */
    void CalculateSeaLevelRepulsion ()
    {
        // Initialise seaLevelRepulsion vector
        this.seaLevelRepulsion = new Vector3();

        // Calculate distance to water surface
        float distanceSigned = this.waterHeight - transform.position.y;

        // Initialise repulsion force vector
        Vector3 repulsionForce = Vector3.down;
        
        // Calculate repulsion force multiplier (based on distance)
        float repulsionMult = 0f;
        if (distanceSigned <= this.waterSurfaceRepulsionDistance * 10)
        {
            // float repulsionForce = this.repulsionAmplitude * Mathf.Exp (-1 * this.repulsionScale * distance / this.waterSurfaceRepulsionDistance);
            // this.seaLevelRepulsion = repulsionForce * Vector3.down;
            repulsionMult = GameFunctions.HardRepulsionFunction (this.waterSurfaceRepulsionMultiplier, this.waterSurfaceRepulsionDistance, distanceSigned);
        }
        repulsionForce *= repulsionMult;

        this.seaLevelRepulsion = repulsionForce; 
    }



    void CalculateTerrainRepulsion ()
    {
        // Initialise terrainRepulsion vector
        this.terrainRepulsion = new Vector3();

        // TODO: Get max terrain height on nearest vertices of current chunk
        //
    }


    // Gizmos functions
    void OnDrawGizmosSelected ()
    {
        // Exit method if debug mode off
        if (!this.debugMode) { return; };

        // Draw static objects' gizmos
        if (Application.isPlaying)
        {
            Gizmos.color = new Color (0.09f, 0.94f, 0.6f, 0.2f);
            for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
            {
                // Extract coordinates array
                GameObject[] repulsionObjects = this.repulsionObjectsStatic [i];

                // Get replusion radius
                float repulsionRadius = this.repulsionRadiiStatic[i];

                // Run through objects' coordinates
                for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
                {
                    Vector3 objectCoordinates = repulsionObjects[objectId].transform.position;
                    Gizmos.DrawSphere (objectCoordinates, repulsionRadius);
                }
            }
        }
        
        // Draw dynamic objects' gizmos
        if (Application.isPlaying)
        {
            Gizmos.color = new Color (0.1f, 0.96f, 0.09f, 0.2f);
            for (int i = 0; i < this.repulsionLayersDynamic.Length; i ++)
            {
                // Extract coordinates array
                GameObject[] repulsionObjects = this.repulsionObjectsDynamic [i];

                // Get replusion radius
                float repulsionRadius = this.repulsionRadiiDynamic[i];

                // Run through objects' coordinates
                for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
                {
                    Vector3 objectCoordinates = repulsionObjects[objectId].transform.position;
                    Gizmos.DrawSphere (objectCoordinates, repulsionRadius);
                }
            }
        }  
    }

    void OnDrawGizmos ()
    {
        // Exit method if debug mode off
        if (!this.debugMode) { return; };

        // General heading vector
        Gizmos.color = Color.black;
        Gizmos.DrawLine (transform.position, transform.position + this.heading);

        // Objects repulsion vector
        if (!float.IsNaN(this.objectsRepulsion.x) && !float.IsNaN(this.objectsRepulsion.y) && !float.IsNaN(this.objectsRepulsion.z)) {
            Gizmos.color = Color.green;
            Gizmos.DrawLine (transform.position, transform.position + this.objectsRepulsion);
        }
       
        // Sea level repulsion vector
        if (!float.IsNaN(this.seaLevelRepulsion.x) && !float.IsNaN(this.seaLevelRepulsion.y) && !float.IsNaN(this.seaLevelRepulsion.z)) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine (transform.position, transform.position + this.seaLevelRepulsion);
        }
        
        // Boundary repulsion vector
        if (!float.IsNaN(this.boundaryRepulsion.x) && !float.IsNaN(this.boundaryRepulsion.y) && !float.IsNaN(this.boundaryRepulsion.z)) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine (transform.position, transform.position + this.boundaryRepulsion);
        }
        
        // Terrain repulsion vector
        // if (!float.IsNaN(this.terrainRepulsion.x) && !float.IsNaN(this.terrainRepulsion.y) && !float.IsNaN(this.terrainRepulsion.z)) {
        //     Gizmos.color = Color.yellow;
        //     Gizmos.DrawLine (transform.position, transform.position + this.terrainRepulsion);
        // }

        // Draw boundaries mesh and its vertices
        if (Application.isPlaying)
        {
            Gizmos.color = new Color (0.95f, 0.57f, 0.18f, 0.35f);
            Gizmos.DrawMesh (this.boundariesMesh, Vector3.zero);
            Gizmos.color = Color.black;
            Gizmos.DrawLine (this.boundariesMesh.vertices[0], this.boundariesMesh.vertices[1]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[1], this.boundariesMesh.vertices[2]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[2], this.boundariesMesh.vertices[3]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[3], this.boundariesMesh.vertices[0]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[0], this.boundariesMesh.vertices[4]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[1], this.boundariesMesh.vertices[5]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[2], this.boundariesMesh.vertices[6]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[3], this.boundariesMesh.vertices[7]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[4], this.boundariesMesh.vertices[5]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[5], this.boundariesMesh.vertices[6]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[6], this.boundariesMesh.vertices[7]);
            Gizmos.DrawLine (this.boundariesMesh.vertices[7], this.boundariesMesh.vertices[4]);
        }

        // Draw top mesh
        if (Application.isPlaying)
        {
            Gizmos.color = new Color (0.09f, 0.64f, 0.98f, 0.35f);
            Gizmos.DrawMesh (this.topMesh, Vector3.zero);
        }
    }

    void GenerateBoundariesMesh ()
    {
        // Set y-axis boundaries
        float yMin = -100f;
        float yMax = this.waterHeight;

        this.boundariesMesh = new Mesh();

        this.boundariesMesh.vertices = new Vector3[] {
            new Vector3 (this.minCoordinates.x, yMin, this.minCoordinates.y),
            new Vector3 (this.minCoordinates.x, yMin, this.maxCoordinates.y),
            new Vector3 (this.maxCoordinates.x, yMin, this.maxCoordinates.y),
            new Vector3 (this.maxCoordinates.x, yMin, this.minCoordinates.y),

            new Vector3 (this.minCoordinates.x, yMax, this.minCoordinates.y),
            new Vector3 (this.minCoordinates.x, yMax, this.maxCoordinates.y),
            new Vector3 (this.maxCoordinates.x, yMax, this.maxCoordinates.y),
            new Vector3 (this.maxCoordinates.x, yMax, this.minCoordinates.y)
        };
        
        this.boundariesMesh.triangles = new int[] {

            // xMin face
            0, 4, 5,
            0, 5, 1,
            0, 5, 4,
            0, 1, 5,

            // xMax face
            2, 6, 7,
            2, 7, 3,
            2, 7, 6,
            2, 3, 7,

            // yMin face
            0, 3, 7,
            0, 7, 4,
            0, 7, 3,
            0, 4, 7,

            // yMax face
            2, 1, 5,
            2, 5, 6,
            2, 5, 1,
            2, 6, 5,
        };
        
        this.boundariesMesh.RecalculateNormals();
    }

    void GenerateTopMesh ()
    {
        this.topMesh = new Mesh();

        this.topMesh.vertices = new Vector3[] {
            new Vector3 (this.minCoordinates.x * 2, this.waterHeight, this.minCoordinates.y * 2),
            new Vector3 (this.minCoordinates.x * 2, this.waterHeight, this.maxCoordinates.y * 2),
            new Vector3 (this.maxCoordinates.x * 2, this.waterHeight, this.maxCoordinates.y * 2),
            new Vector3 (this.maxCoordinates.x * 2, this.waterHeight, this.minCoordinates.y * 2)
        };

        this.topMesh.triangles = new int[] {
            0, 1, 2,
            0, 2, 3,
            0, 2, 1,
            0, 3, 2
        };

        this.topMesh.RecalculateNormals();
    }

}


// User-referenced repulsion layer
[System.Serializable]
public struct RepulsionLayer
{
    public string name;
    public int layerId;
    public Vector3 repulsionSphereOffset;
    public float repulsionRadius;
}


public static class GameFunctions
{
    // TODO: IMPROVE to look only in current and adjacent chunks
    public static GameObject[] FindGameObjectsWithLayer (int layer)
    {
        GameObject[] goArray = GameObject.FindObjectsOfType<GameObject>();
        List<GameObject> goList = new System.Collections.Generic. List<GameObject>();

        for (var i = 0; i < goArray.Length; i++) {
            if (goArray[i].layer == layer) {
                goList.Add(goArray[i]);
            }
        }
        if (goList.Count == 0) {
            return null;
        }
        return goList.ToArray();
    }

    public static float HardRepulsionFunction (float multiplier, float radius, float x)
    {
        if (x > 0) {
            return (float) multiplier * radius / x;
        } else {
            return multiplier;
        }
        
    }

    public static float SoftRepulsionFunction (float multiplier, float radius, float x)
    {
        return (float) multiplier * Mathf.Exp ((radius - x) / radius);

        // Other option: cap at multiplier
        // if (x < radius) {
        //     return multiplier;
        // } else {
        //     return (float) multiplier * Mathf.Exp ((radius - x) / radius);
        // }
    }

    // public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    // {
    //     float u, v, S;
    
    //     do
    //     {
    //         u = 2.0f * UnityEngine.Random.value - 1.0f;
    //         v = 2.0f * UnityEngine.Random.value - 1.0f;
    //         S = u * u + v * v;
    //     }
    //     while (S >= 1.0f);
    
    //     // Standard Normal Distribution
    //     float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);
    
    //     // Normal Distribution centered between the min and max value
    //     // and clamped following the "three-sigma rule"
    //     float mean = (minValue + maxValue) / 2.0f;
    //     float sigma = (maxValue - mean) / 3.0f;
    //     return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    // }
}
