/*
 Copyright Anatole Hernot, 2021
 All rights reserved

 FishMovement v1.1
*/

// TODO: new random movement frequency selector
// TODO: cone of headings, with random direction chosen (to restrict angle)
// TODO: adjust speed setting to be the only setting available >> this.heading normalized, * this.speed
// TODO: increase répulsion aux parois, decrease répulsion en haut de l'eau
// TODO: add offset for rocks
// TODO: work with forces&accelerations instead of speeds

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishMovement : MonoBehaviour
{
    [Header("Movement boundaries")]
    [Tooltip("Min boundaries on the x and z axes")]
    public Vector2 minCoordinates;
    [Tooltip("Max boundaries on the x and z axes")]
    public Vector2 maxCoordinates;

    // Speed
    public float speed = 2f;
    public float repulsionSpeed = 10f;

    // Repulsion function parameters
    float repulsionAmplitude = 100f;
    float repulsionScale = 5f;
    float boundaryRepulsionDistance = 0.1f;
    float waterSurfaceRepulsionDistance = 1f;

    // Random heading settings
    Vector3 minHeading;
    Vector3 maxHeading;
    
    [Header("Repulsion Layers")]
    [Tooltip("Water level")]
    public float waterHeight;

    [Tooltip("Static repulsive objects")]
    public RepulsionLayer[] repulsionLayersStatic;
    [Tooltip("Dynamic repulsive objects (position recalculatedd each frame)")]
    public RepulsionLayer[] repulsionLayersDynamic;

    [Header("Current heading")]
    [SerializeField]
    Vector3 heading;
    [SerializeField]
    Vector3 objectsRepulsion;
    [SerializeField]
    Vector3 terrainRepulsion;
    [SerializeField]
    Vector3 seaLevelRepulsion;
    [SerializeField]
    Vector3 boundaryRepulsion;

    // Optimised lists
    List<Vector3[]> repulsionObjectsCoordinatesStatic = new List<Vector3[]>();
    List<float> repulsionRadiiStatic = new List<float>();
    List<Vector3[]> repulsionObjectsCoordinatesDynamic = new List<Vector3[]>();
    List<float> repulsionRadiiDynamic = new List<float>();

    // Gizmos
    Mesh boundariesMesh;
    Mesh topMesh;

    [Space(30)]

    // Debug mode
    [Tooltip("Enable Gizmos visualisations")]
    public bool debugMode;


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
        // Generate gizmo meshes if debug mode toggled on
        if ((this.debugMode) && (this.topMesh == null)) { this.GenerateTopMesh(); };
        if ((this.debugMode) && (this.boundariesMesh == null)) { this.GenerateBoundariesMesh(); };

        // Pick a new direction (0.2% chance per frame)
        float rd = UnityEngine.Random.Range(0, 999);
        if (rd <= 2) { this.ChangeHeading(); }

        // Refresh dynamic objects' location
        this.LocateObjects (false);

        // Calculate repulsion vectors
        this.CalculateObjectsRepulsion();
        this.CalculateSeaLevelRepulsion();
        this.CalculateBoundaryRepulsion();

        // Apply objects repulsion vector
        if (!float.IsNaN(this.objectsRepulsion.x) && !float.IsNaN(this.objectsRepulsion.y) && !float.IsNaN(this.objectsRepulsion.z)) {
            this.heading += this.objectsRepulsion;
        }
        // Apply sea level repulsion vector
        if (!float.IsNaN(this.seaLevelRepulsion.x) && !float.IsNaN(this.seaLevelRepulsion.y) && !float.IsNaN(this.seaLevelRepulsion.z)) {
            this.heading += this.seaLevelRepulsion * 0.001f;
        }
        // Apply boundary repulsion vector
        if (!float.IsNaN(this.boundaryRepulsion.x) && !float.IsNaN(this.boundaryRepulsion.y) && !float.IsNaN(this.boundaryRepulsion.z)) {
            this.heading += this.boundaryRepulsion * 0.001f;
        }
        // Apply terrain repulsion vector (NOT IMPLEMENTED)
        if (!float.IsNaN(this.terrainRepulsion.x) && !float.IsNaN(this.terrainRepulsion.y) && !float.IsNaN(this.terrainRepulsion.z)) {
            this.heading += this.terrainRepulsion;
        }

        // Move this.gameObject
        // TODO: Apply fish rotation and animation here too, using (Vector3)this.heading
        transform.position += this.heading * Time.deltaTime;
        //transform.rotation = Quaternion.Euler();
    }


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

    void LocateObjects (bool locateStaticObjects)
    {

        // Test of using one function for both static and dynamic (tbc)
        // void Locate (RepulsionLayer[] repulsionLayers, ref List<Vector3[]> repulsionObjectsCoordinates, ref List<float> repulsionRadii)
        // {
        //     for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
        //     {
        //         // Get repulsionLayer
        //         RepulsionLayer repulsionLayer = this.repulsionLayersStatic [i];

        //         // Write repulsion radius
        //         repulsionRadii.Add (repulsionLayer.repulsionRadius);

        //         // Get GameObjects
        //         GameObject[] repulsionObjects = Functions.FindGameObjectsWithLayer (repulsionLayer.layerId);
        //         Vector3[] objectCoordinates = new Vector3 [repulsionObjects.Length]; // Initialise array of coordinates
                
        //         // Get coordinates
        //         for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
        //         {
        //             objectCoordinates [objectId] = repulsionObjects[objectId] .transform.position;
        //         }

        //         // Write coordinates array to list
        //         repulsionObjectsCoordinates.Add (objectCoordinates);
                
        //     }
        // }

        // Locate static repulsive objects
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
                Vector3[] objectCoordinates = new Vector3 [repulsionObjects.Length]; // Initialise array of coordinates
                
                // Get coordinates
                for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
                {
                    objectCoordinates [objectId] = repulsionObjects[objectId] .transform.position;
                }

                // Write coordinates array to list
                this.repulsionObjectsCoordinatesStatic.Add (objectCoordinates);      
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
            Vector3[] objectCoordinates = new Vector3 [repulsionObjects.Length]; // Initialise array of coordinates
            
            // Get coordinates
            for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
            {
                objectCoordinates [objectId] = repulsionObjects[objectId] .transform.position;
            }

            // Write coordinates array to list
            this.repulsionObjectsCoordinatesDynamic.Add (objectCoordinates);
        }
    }


    // Calculate repulsion vectors
    void CalculateObjectsRepulsion ()
    {
        // Initialise objectsRepulsion vector
        this.objectsRepulsion = new Vector3();

        // Static objects' repulsion
        for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
        {
            // Extract coordinates array
            Vector3[] objectCoordinates = this.repulsionObjectsCoordinatesStatic[i];

            // Get replusion radius
            float repulsionRadius = this.repulsionRadiiStatic[i];

            // Run through objects' coordinates
            for (int objectId = 0; objectId < objectCoordinates.Length; objectId ++)
            {
                // Compute direction from repulsive object to current GameObject
                Vector3 impulsionDirectionRaw = transform.position - objectCoordinates[objectId];

                // Compute object distance to current GameObject
                float distance = impulsionDirectionRaw.magnitude;

                // Calculate repulsion force and vector (if not too far away)
                if (distance <= repulsionRadius * 10)
                {
                    float repulsionForce;
                    if (distance < repulsionRadius) {
                        repulsionForce = this.repulsionAmplitude;
                    } else {
                        repulsionForce = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * (distance - repulsionRadius) / repulsionRadius);
                    };
                    Vector3 repulsionVector = impulsionDirectionRaw / distance * repulsionForce;

                    // Add to global repulsion vector
                    this.objectsRepulsion += repulsionVector;
                }   
            } 
        }

        // Dynamic objects' repulsion
        for (int i = 0; i < this.repulsionLayersDynamic.Length; i ++)
        {
            // Extract coordinates array
            Vector3[] objectCoordinates = this.repulsionObjectsCoordinatesDynamic[i];

            // Get replusion radius
            float repulsionRadius = this.repulsionRadiiDynamic[i];

            // Run through objects' coordinates
            for (int objectId = 0; objectId < objectCoordinates.Length; objectId ++)
            {
                // Compute direction from repulsive object to current GameObject
                Vector3 impulsionDirectionRaw = transform.position - objectCoordinates[objectId];

                // Compute object distance to current GameObject
                float distance = impulsionDirectionRaw.magnitude;

                // Calculate repulsion force and vector (if not too far away)
                if (distance <= repulsionRadius * 10)
                {
                    float repulsionForce;
                    if (distance < repulsionRadius) {
                        repulsionForce = this.repulsionAmplitude;
                    } else {
                        repulsionForce = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * (distance - repulsionRadius) / repulsionRadius);
                    };
                    Vector3 repulsionVector = impulsionDirectionRaw / distance * repulsionForce;

                    // Add to global repulsion vector
                    this.objectsRepulsion += repulsionVector;
                }
            }
        }

    }

    void CalculateSeaLevelRepulsion ()
    {
        // Initialise seaLevelRepulsion vector
        this.seaLevelRepulsion = new Vector3();

        // Calculate water surface repulsion force
        float distance = -1 * transform.position.y;

        if (distance <= this.waterSurfaceRepulsionDistance * 10)
        {
            float repulsionForce = this.repulsionAmplitude * Mathf.Exp (-1 * this.repulsionScale * distance / this.waterSurfaceRepulsionDistance);
            this.seaLevelRepulsion = repulsionForce * Vector3.down;
        }        
    }

    void CalculateBoundaryRepulsion ()
    {
        // Initialise boundaryRepulsion vector
        this.boundaryRepulsion = new Vector3();

        // Calculate distances to boundaries
        float minXDistance = transform.position.x - this.minCoordinates.x;
        float maxXDistance = this.maxCoordinates.x - transform.position.x;
        float minZDistance = transform.position.z - this.minCoordinates.y;
        float maxZDistance = this.maxCoordinates.y - transform.position.z;

        // Calculate repulsion force and vector (if not too far away)
        if (minXDistance <= boundaryRepulsionDistance * 10) {
            float repulsionForce;

            if (minXDistance < boundaryRepulsionDistance) {
                repulsionForce = this.repulsionAmplitude;
            } else {
                repulsionForce = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * minXDistance / boundaryRepulsionDistance);
            };
            Vector3 repulsionVector = new Vector3 (1f, 0f, 0f) * repulsionForce;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionVector;
        } else if (maxXDistance <= boundaryRepulsionDistance * 10) {
            float repulsionForce;
            
            if (maxXDistance < boundaryRepulsionDistance) {
                repulsionForce = this.repulsionAmplitude;
            } else {
                repulsionForce = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * maxXDistance / boundaryRepulsionDistance);
            };
            Vector3 repulsionVector = new Vector3 (-1f, 0f, 0f) * repulsionForce;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionVector;
        }

        if (minZDistance <= boundaryRepulsionDistance * 10) {
            float repulsionForce;

            if (minZDistance < boundaryRepulsionDistance) {
                repulsionForce = this.repulsionAmplitude;
            } else {
                repulsionForce = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * minZDistance / boundaryRepulsionDistance);
            };
            Vector3 repulsionVector = new Vector3 (0f, 0f, 1f) * repulsionForce;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionVector;
        } else if (maxZDistance <= boundaryRepulsionDistance * 10) {
            float repulsionForce;
            
            if (maxZDistance < boundaryRepulsionDistance) {
                repulsionForce = this.repulsionAmplitude;
            } else {
                repulsionForce = this.repulsionAmplitude * Mathf.Exp(-1 * this.repulsionScale * maxZDistance / boundaryRepulsionDistance);
            };
            Vector3 repulsionVector = new Vector3 (1f, 0f, -1f) * repulsionForce;

            // Add to global repulsion vector
            this.boundaryRepulsion += repulsionVector;
        }
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

        if (Application.isPlaying)
        {
            Gizmos.color = new Color (0.09f, 0.94f, 0.6f, 0.2f);
            for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
            {
                // Extract coordinates array
                Vector3[] objectCoordinates = this.repulsionObjectsCoordinatesStatic[i];

                // Get replusion radius
                float repulsionRadius = this.repulsionRadiiStatic[i];

                // Run through objects' coordinates
                for (int objectId = 0; objectId < objectCoordinates.Length; objectId ++)
                {
                    Gizmos.DrawSphere (objectCoordinates[objectId], repulsionRadius);
                }
            }
        }
        
        if (Application.isPlaying)
        {
            Gizmos.color = new Color (0.1f, 0.96f, 0.09f, 0.2f);
            for (int i = 0; i < this.repulsionLayersDynamic.Length; i ++)
            {
                // Extract coordinates array
                Vector3[] objectCoordinates = this.repulsionObjectsCoordinatesDynamic[i];

                // Get replusion radius
                float repulsionRadius = this.repulsionRadiiDynamic[i];

                // Run through objects' coordinates
                for (int objectId = 0; objectId < objectCoordinates.Length; objectId ++)
                {
                    Gizmos.DrawSphere (objectCoordinates[objectId], repulsionRadius);
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
            // Bottom face
            // 0, 1, 2,
            // 0, 2, 3,
            // 0, 2, 1,
            // 0, 3, 2,

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

            // Top face
            // 4, 7, 6,
            // 4, 6, 5,
            // 4, 6, 7,
            // 4, 5, 6
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

[System.Serializable]
public struct RepulsionLayer
{
    public string name;

    public int layerId;
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
}
