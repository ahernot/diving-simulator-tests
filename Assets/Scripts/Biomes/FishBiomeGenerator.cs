/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved
 FishBiomeGenerator v1.0 (forked from BiomeGenerator v1.2.2)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// TODO: one file per biome, and then lay biomes out on grid?
// TODO: make biomes permanent, instead of during runtime only

static class Constants
{
    public const string settingsPathRelative = @"./Assets/Scripts/Biomes/FishBiomeSettings.csv";
    public const char CSVSeparator = ',';
}


public class FishBiomeGenerator : MonoBehaviour
{
    [Tooltip("Generation altitude")]
    public float generationAltitude = 0f;

    public int xNbBiomes = 4;
    public int zNbBiomes = 4;

    [Space(30)]

    // Lazy coordinates range for calling all biomes
    public int xBiomeMin = 0;
    public int xBiomeMax = 3;
    public int zBiomeMin = 0;
    public int zBiomeMax = 3;

    [Space(30)]

    public float xBiomeSize = 250f;
    public float zBiomeSize = 250f;

    public GameObject terrainChunkManager;

    [Header("Assets")]
    public Asset[] assets;
    Dictionary<string, Asset> assetDictionary;

    // Array of biomes
    Biome[,] biomes;

    public bool generateOnLoad;


    void Start ()
    {
        if (this.generateOnLoad)
        {
            this.Generate();
        }
    }

    public void Generate ()
    {
        // Generate biomes array
        this.GenerateBiomeArray();

        // Generate assets dictionary
        this.GenerateAssetDictionary();
        
        // Read CSV
        this.ReadCSV();

        // Build biomes
        this.BuildBiomes();
    }

    void GenerateAssetDictionary ()
    {
        // Initialise dictionary
        this.assetDictionary = new Dictionary<string, Asset>();

        // Fill dictionary
        for (int i = 0; i < this.assets.Length; i ++)
        {
            this.assetDictionary [this.assets[i].name] = this.assets[i];
        }
    }

    void GenerateBiomeArray ()
    {
        // Initialise biomes array
        this.biomes = new Biome[xNbBiomes, zNbBiomes];

        // Fill biomes array
        for (int xBiomeId = this.xBiomeMin; xBiomeId <= this.xBiomeMax; xBiomeId ++)
        {
            for (int zBiomeId = this.zBiomeMin; zBiomeId <= this.zBiomeMax; zBiomeId ++)
            {
                // Debug.Log("generating" + xBiomeId.ToString() + "_" + zBiomeId.ToString());
                this.biomes [xBiomeId, zBiomeId] = new Biome();
            }
        }
    }

    void ReadCSV ()
    {
        using (var reader = new StreamReader (Constants.settingsPathRelative))
        {
            
            int lineId = -1;
            while (!reader.EndOfStream)
            {
                // Increment lineId
                lineId ++;

                // Read line
                var line = reader.ReadLine();
                if (lineId <= 0) { continue; }

                // Split line
                var values = line.Split (Constants.CSVSeparator);

                // Unpack line
                int   xBiomeId          = int.Parse   (values[0]);
                int   zBiomeId          = int.Parse   (values[1]);
                string assetName        =              values[2];
                float xPositionRelative = float.Parse (values[3].Replace(".", ","));
                float yPositionRelative = float.Parse (values[4].Replace(".", ","));
                float zPositionRelative = float.Parse (values[5].Replace(".", ","));
                float xRotation         = float.Parse (values[6].Replace(".", ","));
                float yRotation         = float.Parse (values[7].Replace(".", ","));
                float zRotation         = float.Parse (values[8].Replace(".", ","));
                float xScale            = float.Parse (values[9].Replace(".", ","));
                float yScale            = float.Parse (values[10].Replace(".", ","));
                float zScale            = float.Parse (values[11].Replace(".", ","));

                // Format data
                Vector3 positionRelative = new Vector3 (xPositionRelative, yPositionRelative, zPositionRelative);
                Vector3 rotation = new Vector3 (xRotation, yRotation, zRotation);
                Vector3 scale = new Vector3 (xScale, yScale, zScale);

                // Create BiomeElement
                BiomeElement biomeElement = new BiomeElement (assetName, positionRelative, rotation, scale);

                // Add to corresponding biome
                this.biomes [xBiomeId, zBiomeId].biomeElements .Add(biomeElement);
            }
        }
    }

    void BuildBiomes ()
    {
        // Will all be placed in world (NOT as children of BiomeGenerator), with an offset of this.transform.position
        // Will create objects named assetName + id

        // Generate biome visualisation panes in the Biome-0-0 parent object etc.

        // Generate biomeWrapper GameObject
        GameObject biomeWrapper = this.gameObject; //new GameObject();
        //biomeWrapper.name = "Biome Wrapper";

        for (int xBiomeId = this.xBiomeMin; xBiomeId < this.xBiomeMax; xBiomeId ++)
        {
            for (int zBiomeId = this.zBiomeMin; zBiomeId < this.zBiomeMax; zBiomeId ++)
            {

                // Generate biome GameObject
                GameObject biome = new GameObject();
                biome.name = "Biome" + "_" + xBiomeId.ToString() + "_" + zBiomeId.ToString();
                biome .transform.parent = biomeWrapper.transform;

                // Get biome elements list
                List<BiomeElement> biomeElements = this.biomes[xBiomeId, zBiomeId] .biomeElements;

                // Generate biome elements
                for (int elementId = 0; elementId < biomeElements.Count; elementId ++)
                {

                    // Get elementSettings struct instance
                    BiomeElement biomeElement = biomeElements [elementId];

                    // Get asset
                    if (!this.assetDictionary.ContainsKey (biomeElement.name)) { continue; }
                    Asset asset = this.assetDictionary [biomeElement.name];

                    // Generate biomeAsset GameObject
                    GameObject biomeElementObject = GameObject.Instantiate (asset.gameObject);// .Instantiate();
                    biomeElementObject.name = elementId.ToString() + "_" + biomeElement.name;
                    biomeElementObject .transform.parent = biome.transform;
                    biomeElementObject .layer = asset.layerId;

                    // Set biomeAsset's parameters
                    biomeElementObject.transform.position = biomeElement.positionRelative;
                    biomeElementObject.transform.rotation = Quaternion.Euler (biomeElement.rotation);
                    biomeElementObject.transform.localScale = biomeElement.scale;

                    // Apply material
                    if (biomeElementObject.GetComponent<MeshRenderer>() == null) { biomeElementObject .AddComponent<MeshRenderer>(); }
                    MeshRenderer meshRenderer = biomeElementObject .GetComponent<MeshRenderer>();
                    meshRenderer.material = (Material)Instantiate (asset.material);

                    //MeshCollider meshCollider = 
                    if (asset.addMeshCollider) { biomeElementObject .AddComponent<MeshCollider>(); }

                    // Add FishMovement script
                    FishMovement fishMovement = biomeElementObject .AddComponent<FishMovement>();
                    fishMovement .maxHeadingDeflectionAngle = 30f;
                    fishMovement .movementForceMultiplier = 10f;
                    fishMovement dragForceMultiplier = 5f;
                    fishMovement .headingChangeProbability = 0.002f;

                    fishMovement .minCoordinates = new Vector2();
                    fishMovement .maxCoordinates = new Vector2();

                    fishMovement .boundaryRepulsionMultiplier = 0.5f;
                    fishMovement .boundaryRepulsionDistance = 1f;
                    fishMovement .waterHeight = 0f;
                    fishMovement .waterSurfaceRepulsionMultiplier = 10f;
                    fishMovement .waterSurfaceRepulsionDistance = 1f;
                    fishMovement .terrainChunkManager = this.terrainChunkManager;
                    fishMovement .defaultGroundHeight = -150f;

                    fishMovement .repulsionLayersStatic = new RepulsionLayer[] {
                        new RepulsionLayer (
                            "rocks",
                            20,
                            new Vector3(),
                            25
                        ),
                    };

                    fishMovement .repulsionLayersDynamic = new RepulsionLayer[] {
                        new RepulsionLayer (
                            "fish",
                            24,
                            new Vector3(),
                            5
                        ),
                    };

                    fishMovement .globalRepulsionMultiplier = 1f;
                    fishMovement .headingChangeMultiplier = 1f;
                    fishMovement .minDistance = 0.00001f;
                    fishMovement .detectectionRangeMultiplier = 2f;
                    fishMovement .timeMultiplier = 1f;

                    // Add other script
                    // FishMovement fishMovement = biomeElementObject .AddComponent<FishMovement>();
                    

                    // Hidden status
                    if (asset.hidden) { 
                        biomeElementObject.SetActive (false);
                    } else {
                        biomeElementObject.SetActive (true);
                    }
                    
                }

                // Move biome
                biome .transform.position = new Vector3 (this.xBiomeSize * xBiomeId, this.generationAltitude, this.zBiomeSize * zBiomeId);

                // Add BiomeGizmos
                BiomeContainer biomeContainer = biome .AddComponent<BiomeContainer>();
                biomeContainer .xSize = this.xBiomeSize;
                biomeContainer .zSize = this.zBiomeSize;

            }
        }
    }

}



[System.Serializable]
public class Asset
{
    public string name;
    public int layerId;
    public GameObject gameObject;
    public Material material;
    public bool addMeshCollider;
    public bool hidden;

    public Asset (string name, int layerId, GameObject gameObject, Material material, bool addMeshCollider, bool hidden)
    {
        this.name = name;
        this.layerId = layerId;
        this.gameObject = gameObject;
        this.material = material;
        this.addMeshCollider = addMeshCollider;
        this.hidden = hidden;
    }
}

// Biome element (system)
public struct BiomeElement
{
    public string name;
    public Vector3 positionRelative;
    public Vector3 rotation;
    public Vector3 scale;

    public BiomeElement (string name, Vector3 positionRelative, Vector3 rotation, Vector3 scale)
    {
        this.name = name;
        this.positionRelative = positionRelative;
        this.rotation = rotation;
        this.scale = scale;
    }
}

// Biome
public class Biome
{
    public List<BiomeElement> biomeElements;

    public Biome ()
    {
        this.biomeElements = new List<BiomeElement>();
    }
}
