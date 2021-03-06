/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 TerrainChunkMesh v1.4
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class TerrainChunkMesh : MonoBehaviour
{

    // Chunk coordinates
    [HideInInspector]
    public int xChunk = 0;
    [HideInInspector]
    public int zChunk = 0;

    [Header("Generation Settings")]
    // Chunk dimensions (determined by ChunkManager)
    public int xChunkSize = 16;
    public int zChunkSize = 16;

    // Chunk mesh dimensions
    public int xNbPolygons = 32;
    public int zNbPolygons = 32;
    float[] xVerticesRel;
    float[] zVerticesRel;
    
    [Header("Mesh Resolution")]
    [Range(1, 128)]
    public int xReductionRatio = 4;
    [Range(1, 128)]
    public int zReductionRatio = 4;

    public enum MeshResolution {Low, Medium, High};
    public MeshResolution meshResolution;
    MeshResolution previousResolution;
    
    // Initialise mesh resolutions
    Mesh meshLow;
    Mesh meshMed;
    Mesh meshHigh;

    // Low = default (flat)
    Vector3[] verticesLow;
    int[] trianglesLow;
    Vector2[] uvsLow;

    // Med = low poly relief
    Vector3[] verticesMed;
    int[] trianglesMed;
    Vector2[] uvsMed;

    // High = high poly
    Vector3[] verticesHigh;
    int[] trianglesHigh;
    Vector2[] uvsHigh;

    // Noise
    TerrainNoiseManager noiseManager;
    public NoiseLayer[] noiseLayers; // Noise settings
    public float[,] noiseMap;

    void ClampParameters ()
    {
        if (this.xChunkSize <= 0)
        {
            this.xChunkSize = 1;
        }
        if (this.zChunkSize <= 0)
        {
            this.zChunkSize = 1;
        }

        if (this.xNbPolygons <= 0)
        {
            this.xNbPolygons = 1;
        }
        if (this.zNbPolygons <= 0)
        {
            this.zNbPolygons = 1;
        }
    }

    void Start ()
    {
        this.GenerateChunk();
    }

    public void GenerateChunk ()
    {
        gameObject.isStatic = true;
        
        // Check parameters
        this.ClampParameters();

        // Initialise vertices' relative positions
        this.xVerticesRel = Functions.LinearRange (0, this.xChunkSize, this.xNbPolygons + 1);
        this.zVerticesRel = Functions.LinearRange (0, this.zChunkSize, this.zNbPolygons + 1);

        // Generate noise map
        this.noiseManager = new TerrainNoiseManager (this.noiseLayers);
        this.GenerateNoiseMap();

        // Generate all meshes
        this.GenerateMeshLow();
        this.GenerateMeshMed();
        this.GenerateMeshHigh();
    }

    void Update ()
    {
        // Set chosen mesh
        if (this.previousResolution != this.meshResolution)
        {
            this.SetMesh();
        }
    }

    ~TerrainChunkMesh ()
    {
        this.meshLow.Clear();
        this.meshMed.Clear();
        this.meshHigh.Clear();

        Array.Clear(this.verticesLow, 0, this.verticesLow.Length);
        Array.Clear(this.trianglesLow, 0, this.trianglesLow.Length);
        Array.Clear(this.uvsLow, 0, this.uvsLow.Length);

        Array.Clear(this.verticesMed, 0, this.verticesMed.Length);
        Array.Clear(this.trianglesMed, 0, this.trianglesMed.Length);
        Array.Clear(this.uvsMed, 0, this.uvsMed.Length);

        Array.Clear(this.verticesHigh, 0, this.verticesHigh.Length);
        Array.Clear(this.trianglesHigh, 0, this.trianglesHigh.Length);
        Array.Clear(this.uvsHigh, 0, this.uvsHigh.Length);

        Array.Clear(this.noiseMap, 0, this.noiseMap.Length);
    }

    void GenerateNoiseMap ()
    {
        int xNbVertices = this.xNbPolygons + 1;
        int zNbVertices = this.zNbPolygons + 1;
        int seed = 0;

        NoiseLayer noiseLayer = this.noiseLayers [0];

        Vector2 vertexStart = new Vector2 (this.xChunkSize * this.xChunk, this.zChunkSize * this.zChunk);
        Vector2 vertexStop = new Vector2 (vertexStart.x + this.xChunkSize, vertexStart.y + this.zChunkSize);

        Vector2 size = new Vector2 (this.xChunkSize, this.zChunkSize); // will be this.size
        this.noiseMap = this.noiseManager .GenerateNoiseChunk (vertexStart, size, xNbVertices); // xNbVertices or zNbVertices
    }
    
    public void SetMesh ()
    {
        if (this.meshResolution == MeshResolution.Low) {
            GetComponent<MeshFilter>().mesh = this.meshLow;
            GetComponent<MeshCollider>().sharedMesh = this.meshLow;
        } else if (this.meshResolution == MeshResolution.Medium) {
            GetComponent<MeshFilter>().mesh = this.meshMed;
            GetComponent<MeshCollider>().sharedMesh = this.meshMed;
        } else if (this.meshResolution == MeshResolution.High) {
            GetComponent<MeshFilter>().mesh = this.meshHigh;
            GetComponent<MeshCollider>().sharedMesh = this.meshHigh;
        }

        // Update previous mesh resolution
        this.previousResolution = this.meshResolution;
    }

    // only called once
    private void GenerateMeshLow ()
    {
        // Initialise mesh
        this.meshLow = new Mesh();

        // Generate vertices and uvs
        this.verticesLow = new Vector3 [4];
        this.uvsLow = new Vector2 [4];
        int i = 0;
        for (int zVertexRel = 0; zVertexRel <= this.zChunkSize; zVertexRel += this.zChunkSize) // increment to only select bounds
        {
            for (int xVertexRel = 0; xVertexRel <= this.xChunkSize; xVertexRel += this.xChunkSize) // increment to only select bounds
            {
                // Add vertex
                this.verticesLow [i] = new Vector3 (xVertexRel, 0, zVertexRel);

                // Add uv
                this.uvsLow [i] = new Vector2 (
                    (float) xVertexRel / this.xChunkSize,
                    (float) zVertexRel / this.zChunkSize
                );

                i ++;
            }
        }

        // Generate triangles
        this.trianglesLow = new int [6] {
            0, 3, 1,
            0, 2, 3
        };

        // Fill mesh
        this.meshLow .Clear();
        this.meshLow.vertices = this.verticesLow;
        this.meshLow.triangles = this.trianglesLow;
        this.meshLow.uv = this.uvsLow;
        this.meshLow .RecalculateNormals();
    }

    // only called once
    private void GenerateMeshMed ()
    {
        // Initialise mesh
        this.meshMed = new Mesh();

        // Calculate number of polygons per side
        int xNbPolygonsMed = (int) Mathf.Ceil (this.xNbPolygons / this.xReductionRatio); // max between this and 1
        if (xNbPolygonsMed <= 0) { xNbPolygonsMed = 1; }
        int zNbPolygonsMed = (int) Mathf.Ceil (this.zNbPolygons / this.zReductionRatio); // max between this and 1
        if (zNbPolygonsMed <= 0) { zNbPolygonsMed = 1; }

        // Calculate index step
        int xIdStep = (int) Mathf.Floor (this.xNbPolygons / xNbPolygonsMed); // floor to avoid overrun
        int zIdStep = (int) Mathf.Floor (this.zNbPolygons / zNbPolygonsMed); // floor to avoid overrun

        // Initialise vertices Vector3 array
        this.verticesMed = new Vector3 [(xNbPolygonsMed + 1) * (zNbPolygonsMed + 1)];
        // Initialise uvs Vector2 array
        this.uvsMed = new Vector2 [this.verticesMed .Length];

        // Initialise relative vertex positions
        float xVertexRel;
        float yVertexRel;
        float zVertexRel;

        int i = 0;
        for (int zVertexId = 0; zVertexId < zNbPolygonsMed; zVertexId ++)
        {
            // Get z vertex coordinate
            zVertexRel = this.zVerticesRel [zVertexId * zIdStep];

            for (int xVertexId = 0; xVertexId < xNbPolygonsMed; xVertexId ++)
            {
                // Get x vertex coordinate
                xVertexRel = this.xVerticesRel [xVertexId * xIdStep];

                // Get y vertex coordinate using the noise map
                yVertexRel = this.noiseMap [xVertexId * xIdStep, zVertexId * zIdStep];

                // Add vertex
                this.verticesMed [i] = new Vector3 (xVertexRel, yVertexRel, zVertexRel);

                // Add uv
                this.uvsMed [i] = new Vector2 (
                    (float) xVertexRel / this.xChunkSize,
                    (float) zVertexRel / this.zChunkSize
                );

                i ++;
            }

            // Add end vertices and uvs for x=X_MAX
            xVertexRel = this.xVerticesRel [this.xNbPolygons];
            yVertexRel = this.noiseMap [this.xNbPolygons, zVertexId * zIdStep];

            this.verticesMed [i] = new Vector3 (xVertexRel, yVertexRel, zVertexRel);

            this.uvsMed [i] = new Vector2 (
                (float) xVertexRel / this.xChunkSize,
                (float) zVertexRel / this.zChunkSize
            );

            i ++;

        }

        // Add end vertices and uvs for z=Z_MAX
        zVertexRel = this.zVerticesRel [this.zNbPolygons];
        for (int xVertexId = 0; xVertexId < xNbPolygonsMed; xVertexId ++)
        {
            xVertexRel = this.xVerticesRel [xVertexId * xIdStep];
            yVertexRel = this.noiseMap [xVertexId * xIdStep, this.zNbPolygons];

            this.verticesMed [i] = new Vector3 (xVertexRel, yVertexRel, zVertexRel);

            this.uvsMed [i] = new Vector2 (
                (float) xVertexRel / this.xChunkSize,
                (float) zVertexRel / this.zChunkSize
            );

            i ++;
        }

        // Add final vertex
        xVertexRel = this.xVerticesRel [this.xNbPolygons];
        zVertexRel = this.zVerticesRel [this.zNbPolygons];
        yVertexRel = this.noiseMap[this.xNbPolygons, this.zNbPolygons];

        this.verticesMed [i] = new Vector3 (xVertexRel, yVertexRel, zVertexRel);

        this.uvsMed [i] = new Vector2 (
            (float) xVertexRel / this.xChunkSize,
            (float) zVertexRel / this.zChunkSize
        );

        // Generate triangles
        this.trianglesMed = new int [xNbPolygonsMed * zNbPolygonsMed * 6];
        int vert = 0;
        int tris = 0;
        for (int zVertexId = 0; zVertexId < zNbPolygonsMed; zVertexId ++)
        {
            for (int xVertexId = 0; xVertexId < xNbPolygonsMed; xVertexId ++)
            {
                this.trianglesMed [tris + 0] = vert + 0;
                this.trianglesMed [tris + 1] = vert + xNbPolygonsMed + 1;
                this.trianglesMed [tris + 2] = vert + 1;
                this.trianglesMed [tris + 3] = vert + 1;
                this.trianglesMed [tris + 4] = vert + xNbPolygonsMed + 1;
                this.trianglesMed [tris + 5] = vert + xNbPolygonsMed + 2;

                vert ++;
                tris += 6;
            }
            vert ++;
        }

        // Fill mesh
        this.meshMed .Clear();
        this.meshMed.vertices = this.verticesMed;
        this.meshMed.triangles = this.trianglesMed;
        this.meshMed.uv = this.uvsMed;
        this.meshMed .RecalculateNormals();
    }

    // only called once
    private void GenerateMeshHigh ()
    {

        // Initialise mesh
        this.meshHigh = new Mesh();

        // Initialise vertices Vector3 array
        this.verticesHigh = new Vector3 [(this.xNbPolygons + 1) * (this.zNbPolygons + 1)];
        // Initialise uvs Vector2 array
        this.uvsHigh = new Vector2 [this.verticesHigh .Length];

        // Initialise relative vertex positions
        float xVertexRel;
        float yVertexRel;
        float zVertexRel;

        int i = 0;
        for (int zVertexId = 0; zVertexId <= this.zNbPolygons; zVertexId ++)
        {
            // Get z vertex coordinate
            zVertexRel = this.zVerticesRel [zVertexId];

            for (int xVertexId = 0; xVertexId <= this.xNbPolygons; xVertexId ++)
            {
                // Get x vertex coordinate
                xVertexRel = this.xVerticesRel [xVertexId];

                // Get y vertex coordinate using the noise map
                yVertexRel = this.noiseMap [xVertexId, zVertexId];

                // Add vertex
                this.verticesHigh [i] = new Vector3 (xVertexRel, yVertexRel, zVertexRel);

                // Add uv
                this.uvsHigh [i] = new Vector2 (
                    (float) xVertexRel / this.xChunkSize,
                    (float) zVertexRel / this.zChunkSize
                );

                i ++;
            }
        }

        // Initialise triangles int array
        this.trianglesHigh = new int [this.xNbPolygons * this.zNbPolygons * 6];

        int vert = 0;
        int tris = 0;
        for (int zVertexId = 0; zVertexId < this.zNbPolygons; zVertexId ++)
        {
            for (int xVertexId = 0; xVertexId < this.xNbPolygons; xVertexId ++)
            {
                this.trianglesHigh [tris + 0] = vert + 0;
                this.trianglesHigh [tris + 1] = vert + this.xNbPolygons + 1;
                this.trianglesHigh [tris + 2] = vert + 1;
                this.trianglesHigh [tris + 3] = vert + 1;
                this.trianglesHigh [tris + 4] = vert + this.xNbPolygons + 1;
                this.trianglesHigh [tris + 5] = vert + this.xNbPolygons + 2;

                vert ++;
                tris += 6;
            }
            vert ++;
        }

        // Fill mesh
        this.meshHigh .Clear();
        this.meshHigh.vertices = this.verticesHigh;
        this.meshHigh.triangles = this.trianglesHigh;
        this.meshHigh.uv = this.uvsHigh;
        this.meshHigh .RecalculateNormals();
    }

}
