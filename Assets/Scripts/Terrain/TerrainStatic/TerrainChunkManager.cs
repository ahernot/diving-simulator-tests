/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 TerrainChunkManager v1.5
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour
{

    [Tooltip("Player (chunk loader)")]
    public GameObject player;
    [Tooltip("Player camera")]
    public Camera viewCamera;

    [Tooltip("Chunk material")]
    public Material material;

    // List of chunks
    [HideInInspector]
    public GameObject[] chunks;

    public int layerId;

    [Header("Generation Settings")]
    // Number of chunks
    [Tooltip("Half number of chunks on the x-axis")]
    public int xHalfNbChunks = 64;
    [Tooltip("Half number of chunks on the z-axis")]
    public int zHalfNbChunks = 64;

    // Chunks to load (square of side 2x+1)
    [Tooltip("Number of chunks to load in each direction around the player")]
    public int loadHighRadius = 8;
    public int loadRadius = 12;


    [Header("Chunk Settings")]
    // Size of a chunk (affects texture size)
    [Tooltip("x-axis size of the chunk (in world units)")]
    public int xChunkSize = 16;
    [Tooltip("z-axis size of the chunk (in world units)")]
    public int zChunkSize = 16;

    // Chunk mesh poly count (fineness)
    [Tooltip("Number of polygons per chunk along the x-axis")]
    public int xNbPolygons = 32;
    [Tooltip("Number of polygons per chunk along the z-axis")]
    public int zNbPolygons = 32;

    [Header("Mesh Resolution")]
    [Tooltip("Mesh reduction ratio along the x-axis")]
    [Range(1, 128)]
    public int xReductionRatio = 4;
    [Tooltip("Mesh reduction ratio along the z-axis")]
    [Range(1, 128)]
    public int zReductionRatio = 4;

    // Player chunk position (updated at runtime)
    int xChunkPlayer;
    int zChunkPlayer;

    // Noise settings
    [Header("Noise Settings")]
    public NoiseLayer[] noiseLayers;

    [Space(30)]
    [Tooltip("Optimize chunk loading by hiding chunks behind player")]
    public bool optimizeLoading = false;

    public bool generateOnLoad = false;

    // Frame counter
    private int frames;
    public int resolutionSkippedFrames = 30;

    void Start ()
    {
        if (generateOnLoad)
        { 
            if (gameObject.transform.childCount != 0)
            {
                this.DestroyChunks();
            }
            
            this.GenerateChunks();
        }

        this.UpdateResolutions();
    }

    /**
    * Regenerate the chunks (with their noise maps)
    **/
    public void GenerateChunks ()
    {
        // Initialise chunks array
        this.chunks = new GameObject[this.xHalfNbChunks * this.zHalfNbChunks * 4];

        int i = 0;
        for (int xChunkId = -1 * this.xHalfNbChunks; xChunkId < this.xHalfNbChunks; xChunkId++)
        {
            for (int zChunkId = -1 * this.zHalfNbChunks; zChunkId < this.zHalfNbChunks; zChunkId++)
            {
                // Initialise empty GameObject
                this.chunks[i] = new GameObject();
                this.chunks[i] .name = "TerrainChunk_" + xChunkId.ToString() + "_" + zChunkId.ToString();
                this.chunks[i] .transform.parent = gameObject.transform; // set parent
                this.chunks[i] .layer = this.layerId;

                // Update position and rotation
                this.chunks[i] .transform.position = new Vector3(xChunkId * this.xChunkSize, gameObject.transform.position.y, zChunkId * this.zChunkSize);
                this.chunks[i] .transform.rotation = Quaternion.identity;

                // Add necessary components
                this.chunks[i] .AddComponent<MeshFilter>();
                this.chunks[i] .AddComponent<MeshCollider>();
                this.chunks[i] .AddComponent<MeshRenderer>();

                MeshRenderer meshRenderer = this.chunks[i] .GetComponent<MeshRenderer>();
                meshRenderer.material = (Material)Instantiate(this.material);

                // Create TerrainChunkMesh component
                TerrainChunkMesh terrainChunkMesh = this.chunks[i] .AddComponent<TerrainChunkMesh>();

                // Set ChunkMesh parameters
                terrainChunkMesh.xChunk = xChunkId;
                terrainChunkMesh.zChunk = zChunkId;
                terrainChunkMesh.xChunkSize = this.xChunkSize;
                terrainChunkMesh.zChunkSize = this.zChunkSize;
                terrainChunkMesh.xNbPolygons = this.xNbPolygons;
                terrainChunkMesh.zNbPolygons = this.zNbPolygons;
                terrainChunkMesh.xReductionRatio = this.xReductionRatio;
                terrainChunkMesh.zReductionRatio = this.zReductionRatio;

                // Set noise parameters
                terrainChunkMesh.noiseLayers = this.noiseLayers;

                i++;
            }
        }
    }

    public void DestroyChunks ()
    {
        foreach (Transform child in transform) {
            GameObject.Destroy(child.gameObject);
        }
    }

    void Update ()
    {
        this.GetPlayerChunk();

        if (this.frames % this.resolutionSkippedFrames == 0)
        {
            this.UpdateResolutions();
            this.frames = 0;
        }

        this.frames ++;
        
    }

    void GetPlayerChunk ()
    {
        float x = this.player .transform.position.x;
        float z = this.player .transform.position.z;

        this.xChunkPlayer = (int) Mathf.Floor((float)x / this.xChunkSize);
        this.zChunkPlayer = (int) Mathf.Floor((float)z / this.zChunkSize);
    }

    public GameObject GetChunkAtPosition (float x, float z)
    {
        // Generate chunk ID
        int xChunkId = (int) Mathf.Floor (x / this.xChunkSize);
        int zChunkId = (int) Mathf.Floor (z / this.zChunkSize);

        // Chunk out of generation bounds
        if ((Mathf.Abs(xChunkId) > this.xHalfNbChunks) || (Mathf.Abs(zChunkId) > this.zHalfNbChunks))
        {
            return null;
        }

        // Generate chunk name
        string chunkName = "TerrainChunk_" + xChunkId.ToString() + "_" + zChunkId.ToString(); // same as in this.GenerateChunks()

        // Get chunk object
        GameObject chunk = GameObject.Find (chunkName);

        return chunk;
    }

    public float GetHeightAtPosition (float x, float z)
    {
        float heightOffset = transform.position.y;

        // Get chunk
        GameObject chunk = this.GetChunkAtPosition (x, z);
        if (chunk == null) { return heightOffset; }

        // Get chunk mesh and vertices
        Mesh chunkMesh = chunk.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = chunkMesh.vertices;

        // Calculate position in chunk
        float xRel = x - Mathf.Floor (x / this.xChunkSize) * this.xChunkSize;
        float zRel = z - Mathf.Floor (z / this.zChunkSize) * this.zChunkSize;

        // Calculate nearest vertex id in chunk
        int xVertexId = (int) Mathf.Floor ((xRel / this.xChunkSize) * (this.xNbPolygons)); // xNbPolygons+1 or not?
        int zVertexId = (int) Mathf.Floor ((zRel / this.zChunkSize) * (this.zNbPolygons)); // zNbPolygons+1 or not?

        // Get chunk noise map
        float[,] chunkNoiseMap = chunk.GetComponent<TerrainChunkMesh>().noiseMap;
        float noiseHeight = chunkNoiseMap [xVertexId, zVertexId];

        return heightOffset + noiseHeight;
    }
    

    void UpdateResolutions ()
    {

        // Get camera direction
        Vector2 cameraForward2D = new Vector2 ();  // blank vector to prevent errors
        if (this.optimizeLoading == true) {
            Vector3 cameraForward = this.viewCamera .transform.forward;
            cameraForward2D = new Vector2 (cameraForward.x, cameraForward.z);    
        }
        
        int i = 0;
        for (int xChunkId = -1 * this.xHalfNbChunks; xChunkId < this.xHalfNbChunks; xChunkId++)
        {
            for (int zChunkId = -1 * this.zHalfNbChunks; zChunkId < this.zHalfNbChunks; zChunkId++)
            {

                // Continue if chunk out of bounds (nb of chunks was increased but not yet generated)
                if (i >= this.chunks.Length) { continue; }

                // Get chunk's mesh script
                TerrainChunkMesh terrainChunkMesh = this.chunks[i] .GetComponent<TerrainChunkMesh>();

                // Calculate horizontal distance to chunk
                float playerToChunkDist = Mathf.Sqrt ( Mathf.Pow (xChunkId - this.xChunkPlayer, 2) + Mathf.Pow(zChunkId - this.zChunkPlayer, 2) );

                // Update resolutions based on distance
                if (playerToChunkDist > this.loadRadius) {
                    terrainChunkMesh.meshResolution = TerrainChunkMesh.MeshResolution.Low;
                    this.chunks[i] .SetActive (false);
                } else if (playerToChunkDist > this.loadHighRadius) {
                    this.chunks[i] .SetActive (true);
                    terrainChunkMesh.meshResolution = TerrainChunkMesh.MeshResolution.Medium;
                } else {
                    this.chunks[i] .SetActive (true);
                    terrainChunkMesh.meshResolution = TerrainChunkMesh.MeshResolution.High;
                }

                // Loading optimisation (overwrites previously activated chunks)
                if (this.optimizeLoading == true) {
                    Vector2 playerToChunk = new Vector2 (xChunkId - this.xChunkPlayer, zChunkId - this.zChunkPlayer);
                    float distanceToChunk = playerToChunk.magnitude;
                    if ((Vector2.Dot (cameraForward2D, playerToChunk) / distanceToChunk < -0.2f) && (distanceToChunk > 2f))  // would work better with 3D dot product?
                    {
                        this.chunks[i] .SetActive (false);
                    }
                }

                i ++;
            }
        }

    }

}
