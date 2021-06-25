/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 FishMovement v1.5.1
*/

// TODO: apply repulsion sphere offset
// TODO: draw less gizmos for performance
// TODO: add a cone gizmo to visualise the possible deflection paths
// Rotation to change; inertia => deltaTheta between old and new vector, and will rotate until it gets to new vector (careful bout oscillation)

/*
* Special instructions:
*   If new objects are added, run FishMovement.LocateObjects (false);
* 
* Information:
*   The water level, terrain, and object boundaries are hard boundaries
*   The bounding box boundaries are flexible
*   Object mass is 1kg
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FishMovement : MonoBehaviour
{

    // Random heading
    [Tooltip("Max heading deflection angle, in degrees")] // not really: should pair cylindrical geometry with (u,v,w) to get a true max angle
    public float maxHeadingDeflectionAngle = 30f;
    public float movementForceMultiplier = 10f;
    public float dragForceMultiplier = 5f;
    [Tooltip("Probability of heading change per frame")]
    [Range(0, 1)]
    public float headingChangeProbability = 0.002f;

    // Repulsion: bounding box
    [Header("Movement boundaries")]
    [Tooltip("Min boundaries on the x and z axes")]
    public Vector2 minCoordinates = new Vector2 (-50f, -50f);
    [Tooltip("Max boundaries on the x and z axes")]
    public Vector2 maxCoordinates = new Vector2 (50f, 50f);
    public float boundaryRepulsionMultiplier = 0.5f;
    public float boundaryRepulsionDistance = 1f;
    
    [Space(10)]
    // Repulsion: water level
    [Header("Water level")]
    [Tooltip("Water level")]
    public float waterHeight = 10f;
    public float waterSurfaceRepulsionMultiplier = 10f;
    public float waterSurfaceRepulsionDistance = 1f;
    

    // --- TODO --- (not used yet)
    [Space(10)]
    [Tooltip("Terrain")]
    public GameObject terrainChunkManager;
    TerrainChunkManager terrainChunkManagerScript;
    float defaultGroundHeight = -30f;

    [Space(10)]
    // Repulsion: layers
    [Header("Repulsion Layers")]
    [Tooltip("Static repulsive objects")]
    public RepulsionLayer[] repulsionLayersStatic;
    [Tooltip("Dynamic repulsive objects (position recalculatedd each frame)")]
    public RepulsionLayer[] repulsionLayersDynamic;
    [Space(10)]
    // Repulsion function parameters
    public float globalRepulsionMultiplier = 1f;


    [Header("Heading")]
    [Tooltip("Current heading")]
    [SerializeField]
    Vector3 u; // heading
    Vector3 v;
    Vector3 w;

    [Header("Motion variables")]
    [SerializeField]
    Vector3 acceleration;
    [SerializeField]
    Vector3 velocity;

    [Header("Force vectors")]
    [SerializeField]
    Vector3 movementForce;
    [SerializeField]
    Vector3 dragForce;
    [SerializeField]
    Vector3 objectsRepulsionForce;
    [SerializeField]
    Vector3 boundaryRepulsionForce;
    [SerializeField]
    Vector3 seaLevelRepulsionForce;
    [SerializeField]
    Vector3 terrainRepulsionForce;

    // Rotation
    public float headingChangeMultiplier = 1f;
    [SerializeField]
    Vector3 rotationAcceleration;
    [SerializeField]
    Vector3 rotationVelocity;
    // [SerializeField]
    // Vector3 rotation;

    // Optimised lists
    List<GameObject[]> repulsionObjectsStatic = new List<GameObject[]>();
    List<float> repulsionRadiiStatic = new List<float>();
    List<GameObject[]> repulsionObjectsDynamic = new List<GameObject[]>();
    List<float> repulsionRadiiDynamic = new List<float>();


    public float minDistance = 0.00001f;
    float detectectionRangeMultiplier = 2f;

    // Gizmos (debug mode)
    [Tooltip("Enable Gizmos visualisations")]
    public bool debugMode = false;
    Mesh boundariesMesh;
    Mesh topMesh;
    public float timeMultiplier = 1f;



    void Start ()
    {
        // Initialise acceleration vector and drag force vector
        this.acceleration = new Vector3();
        this.dragForce = new Vector3();

        // Generate gizmo meshes
        if (this.debugMode)
        {
            this.GenerateBoundariesMesh();
            this.GenerateTopMesh();
        }

        // Locate all objects
        this.LocateObjects (true);

        // Initialise heading
        this.InitialiseHeading();

        //
        try {
            this.terrainChunkManagerScript = this.terrainChunkManager.GetComponent<TerrainChunkManager>();
        } catch {
            this.terrainChunkManagerScript = null;
        };
        
    
    }

    void Update ()
    {
        // Pick a new direction
        float rd = UnityEngine.Random.Range(0f, 1f);
        if (rd <= this.headingChangeProbability) {
            this.ChangeHeading();
            if (this.debugMode) { Debug.Log (gameObject.name + ": changed heading"); }
        }

        // Reset acceleration
        this.acceleration = this.movementForce;

        // Calculate repulsion forces
        this.CalculateObjectsRepulsion();
        this.CalculateBoundaryRepulsion();
        this.CalculateSeaLevelRepulsion();
        this.CalculateTerrainRepulsion();
        this.CalculateDragForce();

        // Apply objects repulsion force
        if (!float.IsNaN(this.objectsRepulsionForce.x) && !float.IsNaN(this.objectsRepulsionForce.y) && !float.IsNaN(this.objectsRepulsionForce.z)) {
            this.acceleration += this.objectsRepulsionForce;
        }
        // Apply boundary repulsion force
        if (!float.IsNaN(this.boundaryRepulsionForce.x) && !float.IsNaN(this.boundaryRepulsionForce.y) && !float.IsNaN(this.boundaryRepulsionForce.z)) {
            this.acceleration += this.boundaryRepulsionForce;
        }
        // Apply sea level repulsion force
        if (!float.IsNaN(this.seaLevelRepulsionForce.x) && !float.IsNaN(this.seaLevelRepulsionForce.y) && !float.IsNaN(this.seaLevelRepulsionForce.z)) {
            this.acceleration += this.seaLevelRepulsionForce;
        }
        // Apply terrain repulsion force
        if (!float.IsNaN(this.terrainRepulsionForce.x) && !float.IsNaN(this.terrainRepulsionForce.y) && !float.IsNaN(this.terrainRepulsionForce.z)) {
            this.acceleration += this.terrainRepulsionForce;
        }
        // Apply drag force
        this.acceleration += this.dragForce;


        // Update movement force (heading) accordingly
        this.movementForce += 0.5f * this.velocity;
        this.u = this.movementForce / this.movementForce.magnitude;
        this.movementForce = this.u * this.movementForceMultiplier;
        

        // Integrate motion
        this.velocity += this.acceleration * Time.deltaTime * this.timeMultiplier;
        Vector3 positionDelta = this.velocity * Time.deltaTime * this.timeMultiplier;

        // Integrate rotation
        this.rotationVelocity += this.rotationAcceleration * Time.deltaTime * this.timeMultiplier;
        Vector3 rotationDelta = this.rotationVelocity * Time.deltaTime * this.timeMultiplier;

        this.UpdateRotationAcceleration();

        // Move and rotate
        transform.position += positionDelta;
        transform.eulerAngles += rotationDelta;
        // Set heading
        // transform.forward = this.velocity; // = this.u;


        // Generate gizmo meshes if debug mode toggled on during runtime
        if ((this.debugMode) && (this.topMesh == null)) { this.GenerateTopMesh(); };
        if ((this.debugMode) && (this.boundariesMesh == null)) { this.GenerateBoundariesMesh(); };
    }


    void UpdateRotationAcceleration ()
    {
        // Vector3 headingChangeVector = this.u - trasnform.forward;

        float angleVal = Vector3.Angle (transform.forward, this.u);

        Vector3 angleUVW = new Vector3 (
            0f,
            0f,
            angleVal
        );

        Vector3[] transformMatrix = new Vector3[3] {
            this.u,
            this.v,
            this.w
        };

        Vector3 angleXYZ = new Vector3 (
            transformMatrix[0].x * angleUVW.x + transformMatrix[1].x * angleUVW.y + transformMatrix[2].x * angleUVW.z,
            transformMatrix[0].y * angleUVW.x + transformMatrix[1].y * angleUVW.y + transformMatrix[2].y * angleUVW.z,
            transformMatrix[0].z * angleUVW.x + transformMatrix[1].z * angleUVW.y + transformMatrix[2].z * angleUVW.z
        );

        this.rotationAcceleration = this.headingChangeMultiplier * angleXYZ;
    }


    /**
    * Initialise the heading (run on start)
    */
    void InitialiseHeading ()
    {
        // Set random heading parameters
        Vector3 minHeading = new Vector3 (-10f, -10f, -10f);
        Vector3 maxHeading = new Vector3 (10f, 10f, 10f);

        // Generate random vector
        Vector3 randomHeading = new Vector3 (
            UnityEngine.Random.Range(minHeading.x, maxHeading.x),
            UnityEngine.Random.Range(minHeading.y, maxHeading.y),
            UnityEngine.Random.Range(minHeading.z, maxHeading.z)
        );

        // Set heading
        this.u = randomHeading / randomHeading.magnitude;

        // Set initial heading
        this.ChangeHeading();
    }

    /**
    * Change the heading using a flat probability function in a 'cone' around the current heading.
    * No rotation inertia.
    */
    void ChangeHeading ()
    {
        // Recalculate base vectors
        this.Calculatevw();

        // Generate random angles
        float thetav = Random.Range(-1f * this.maxHeadingDeflectionAngle, this.maxHeadingDeflectionAngle) / 180f * Mathf.PI;
        float thetaw = Random.Range(-1f * this.maxHeadingDeflectionAngle, this.maxHeadingDeflectionAngle) / 180f * Mathf.PI;

        // Calculate new heading (projection along (u,v,w))
        Vector3 newHeading = Mathf.Cos(thetav) * this.u + Mathf.Sin(thetav) * this.v + Mathf.Cos(thetaw) * this.u + Mathf.Sin(thetaw) * this.w;
        newHeading /= newHeading.magnitude;

        // Set new heading
        this.u = newHeading / newHeading.magnitude;
        this.movementForce = this.movementForceMultiplier * this.u;
    }


    /**
    * Calculate the heading's orthonormal base
    */
    void Calculatevw ()
    {
        this.v = new Vector3 (
            1f,
            1f,
            -1f * (this.u.x + this.u.y) / this.u.z
        );
        this.v /= this.v.magnitude;

        float wy = (this.u.x + this.u.y + (this.u.z * this.u.z) / this.u.x) / (this.u.z * (1 - this.u.y / this.u.x));
        float wx = -1f * (this.u.z + wy * this.u.y) / this.u.x;
        this.w = new Vector3 (
            wx,
            wy,
            1f
        );
        this.w /= this.w.magnitude;
    }


    /**
    * Locate all repulsive objects in the scene and store them in memory
    */
    void LocateObjects (bool locateStaticObjects)
    {
        // Initialise name blacklist
        string[] nameBlacklist = new string[] {gameObject.name};

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

                // Apply blacklist
                GameObject[] filteredGameObject = GameFunctions.FilterGameObjectsWithName (repulsionObjects, nameBlacklist);

                // Add GameObject[] to list
                this.repulsionObjectsStatic.Add (filteredGameObject);   
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

            // Apply blacklist
            GameObject[] filteredGameObject = GameFunctions.FilterGameObjectsWithName (repulsionObjects, nameBlacklist);

            // Add GameObject[] to list
            this.repulsionObjectsDynamic.Add (filteredGameObject); 
        }
    }


    void CalculateDragForce ()
    {
        this.dragForce = -1f * this.dragForceMultiplier * this.velocity;
    }

    /**
    * Calculate the object repulsion force vectors
    */
    void CalculateObjectsRepulsion ()
    {
        // Initialise objectsRepulsionForce vector
        this.objectsRepulsionForce = new Vector3();


        // Static objects' repulsion
        for (int i = 0; i < this.repulsionLayersStatic.Length; i ++)
        {
            // Get GameObject[] and repulsionRadius
            float repulsionRadius = this.repulsionRadiiStatic [i];
            GameObject[] repulsionObjects = this.repulsionObjectsStatic [i];

            // Run through objects
            for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
            {
                // Compute direction from repulsive object to current GameObject
                Vector3 repulsionForce = transform.position - repulsionObjects[objectId].transform.position;
                float distance = repulsionForce.magnitude;

                // Calculate repulsion force and vector (if not too far away)
                if (distance <= this.minDistance) { distance = this.minDistance; }
                if (distance <= repulsionRadius * this.detectectionRangeMultiplier)
                {
                    // Apply repulsion force multiplier (based on distance)
                    float repulsionMult = GameFunctions.HardRepulsionFunction (this.globalRepulsionMultiplier, repulsionRadius, distance);
                    repulsionForce = (repulsionForce / distance) * repulsionMult;

                    // Add to global repulsion vector
                    this.objectsRepulsionForce += repulsionForce;
                }   
            } 
        }

        // Dynamic objects' repulsion
        for (int i = 0; i < this.repulsionLayersDynamic.Length; i ++)
        {
            // Get GameObject[] and repulsionRadius
            float repulsionRadius = this.repulsionRadiiDynamic [i];
            GameObject[] repulsionObjects = this.repulsionObjectsDynamic [i];

            // Run through objects
            for (int objectId = 0; objectId < repulsionObjects.Length; objectId ++)
            {
                // Compute direction from repulsive object to current GameObject
                Vector3 repulsionForce = transform.position - repulsionObjects[objectId].transform.position;
                float distance = repulsionForce.magnitude;

                // Calculate repulsion force and vector (if not too far away)
                if (distance <= this.minDistance) { distance = this.minDistance; }
                if (distance <= repulsionRadius * this.detectectionRangeMultiplier)
                {
                    // Apply repulsion force multiplier (based on distance)
                    float repulsionMult = GameFunctions.HardRepulsionFunction (this.globalRepulsionMultiplier, repulsionRadius, distance);
                    repulsionForce = (repulsionForce / distance) * repulsionMult;

                    // Add to global repulsion vector
                    this.objectsRepulsionForce += repulsionForce;
                }
            }
        }

    }

    /**
    * Calculate the lateral boundary repulsion force vectors
    */
    void CalculateBoundaryRepulsion ()
    {
        // Initialise boundaryRepulsionForce vector
        this.boundaryRepulsionForce = new Vector3();

        // Calculate distances to boundaries (can be null without posing a problem)
        float minXDistanceSigned = transform.position.x - this.minCoordinates.x;
        float maxXDistanceSigned = this.maxCoordinates.x - transform.position.x;
        float minZDistanceSigned = transform.position.z - this.minCoordinates.y;
        float maxZDistanceSigned = this.maxCoordinates.y - transform.position.z;

        // Min X
        if (minXDistanceSigned <= boundaryRepulsionDistance * this.detectectionRangeMultiplier)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (1f, 0f, 0f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, minXDistanceSigned);
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsionForce += repulsionForce;
        }
        
        // Max X
        else if (maxXDistanceSigned <= boundaryRepulsionDistance * this.detectectionRangeMultiplier)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (-1f, 0f, 0f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, maxXDistanceSigned);
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsionForce += repulsionForce;
        }

        // Min Z
        if (minZDistanceSigned <= boundaryRepulsionDistance * this.detectectionRangeMultiplier)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (0f, 0f, 1f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, minZDistanceSigned);
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsionForce += repulsionForce;
        }
        
        // Max Z
        else if (maxZDistanceSigned <= boundaryRepulsionDistance * this.detectectionRangeMultiplier)
        {
            // Initialise repulsion force vector
            Vector3 repulsionForce = new Vector3 (0f, 0f, -1f);

            // Apply repulsion force multiplier (based on distance)
            float repulsionMult = GameFunctions.SoftRepulsionFunction (this.boundaryRepulsionMultiplier, this.boundaryRepulsionDistance, maxZDistanceSigned);
            repulsionForce *= repulsionMult;

            // Add to global repulsion vector
            this.boundaryRepulsionForce += repulsionForce;
        }
    }

    /**
    * Calculate the water surface repulsion force vector
    */
    void CalculateSeaLevelRepulsion ()
    {
        // Initialise seaLevelRepulsionForce vector
        this.seaLevelRepulsionForce = new Vector3();

        // Calculate distance to water surface (can be null without a problem)
        float distanceSigned = this.waterHeight - transform.position.y;

        // Initialise repulsion force vector
        Vector3 repulsionForce = Vector3.down;
        
        // Calculate repulsion force multiplier (based on distance)
        float repulsionMult = 0f;
        if (distanceSigned <= this.waterSurfaceRepulsionDistance * this.detectectionRangeMultiplier)
        {
            repulsionMult = GameFunctions.HardRepulsionFunction (this.waterSurfaceRepulsionMultiplier, this.waterSurfaceRepulsionDistance, distanceSigned);
        }
        repulsionForce *= repulsionMult;

        this.seaLevelRepulsionForce = repulsionForce; 
    }

    /**
    * Calculate the terrain repulsion force vector
    */
    void CalculateTerrainRepulsion ()
    {
        float groundHeight;
        if (this.terrainChunkManagerScript != null) {
            groundHeight = this.terrainChunkManagerScript.GetHeightAtPosition(transform.position.x, transform.position.z);
        } else {
            groundHeight = this.defaultGroundHeight;
        }

        // Initialise terrainRepulsionForce vector
        this.terrainRepulsionForce = new Vector3();

        // Calculate distance to ground surface (can be null without a problem)
        float distanceSigned = transform.position.y - groundHeight;

        // Initialise repulsion force vector
        Vector3 repulsionForce = Vector3.up;
        
        // Calculate repulsion force multiplier (based on distance)
        float repulsionMult = 0f;
        if (distanceSigned <= this.waterSurfaceRepulsionDistance * this.detectectionRangeMultiplier)
        {
            repulsionMult = GameFunctions.HardRepulsionFunction (this.waterSurfaceRepulsionMultiplier, this.waterSurfaceRepulsionDistance, distanceSigned);
        }
        repulsionForce *= repulsionMult;

        this.terrainRepulsionForce = repulsionForce; 
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
        // General heading vector
        Gizmos.color = Color.black;
        Gizmos.DrawLine (transform.position, transform.position + this.acceleration);

        // Objects repulsion vector
        if (!float.IsNaN(this.objectsRepulsionForce.x) && !float.IsNaN(this.objectsRepulsionForce.y) && !float.IsNaN(this.objectsRepulsionForce.z)) {
            Gizmos.color = Color.green;
            Gizmos.DrawLine (transform.position, transform.position + this.objectsRepulsionForce);
        }

        // Boundary repulsion vector
        if (!float.IsNaN(this.boundaryRepulsionForce.x) && !float.IsNaN(this.boundaryRepulsionForce.y) && !float.IsNaN(this.boundaryRepulsionForce.z)) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine (transform.position, transform.position + this.boundaryRepulsionForce);
        }
       
        // Sea level repulsion vector
        if (!float.IsNaN(this.seaLevelRepulsionForce.x) && !float.IsNaN(this.seaLevelRepulsionForce.y) && !float.IsNaN(this.seaLevelRepulsionForce.z)) {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine (transform.position, transform.position + this.seaLevelRepulsionForce);
        }
        
        // Terrain repulsion vector
        if (!float.IsNaN(this.terrainRepulsionForce.x) && !float.IsNaN(this.terrainRepulsionForce.y) && !float.IsNaN(this.terrainRepulsionForce.z)) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine (transform.position, transform.position + this.terrainRepulsionForce);
        }

        // Draw uvw gizmos
        // Gizmos.color = Color.red;
        // Gizmos.DrawLine (transform.position, transform.position + this.u);
        // Gizmos.color = Color.green;
        // Gizmos.DrawLine (transform.position, transform.position + this.v);
        // Gizmos.color = Color.blue;
        // Gizmos.DrawLine (transform.position, transform.position + this.w);

        // Exit method if debug mode off
        if (!this.debugMode) { return; };

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

    public RepulsionLayer (string name, int layerId, Vector3 repulsionSphereOffset, float repulsionRadius)
    {
        this.name = name;
        this.layerId = layerId;
        this.repulsionSphereOffset = repulsionSphereOffset;
        this.repulsionRadius = repulsionRadius;
    }
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


    public static GameObject[] FilterGameObjectsWithName (GameObject[] gameObjects, string[] nameBlacklist)
    {

        int nameNb = nameBlacklist.Length;

        // Initialised filtered list
        List<GameObject> filteredGameObjects = new List<GameObject>();

        // Loop through GameObject array items
        for (int objectId = 0; objectId < gameObjects.Length; objectId ++)
        {
            for (int nameId = 0; nameId < nameNb; nameId ++)
            {
                // Get gameObject
                GameObject gameObject = gameObjects [objectId];

                // Add only if name not in blacklist
                if (gameObject.name != nameBlacklist[nameId])
                {
                    filteredGameObjects.Add (gameObject);
                }
            }
        }

        return filteredGameObjects.ToArray();
    }
}
