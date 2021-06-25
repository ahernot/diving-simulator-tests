/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved
 FishBiomeGenerator v1.1 (forked from BiomeGenerator v1.2.2)
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using RealSpace3D;

// TODO: one file per biome, and then lay biomes out on grid?
// TODO: make biomes permanent, instead of during runtime only
// Careful: do not move the biome generator after generation, it messes up the fish movement boundaries

static class FishConstants
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
    public GameObject player;

    [Header("Assets")]
    public FishAsset[] fishAssets;
    Dictionary<string, Asset> assetDictionary;

    // Array of biome settings
    FishBiome[,] fishBiomes;
    List<GameObject> biomes;
    public int loadRadius = 2;

    public bool generateOnLoad;


    void Start ()
    {
        if (this.generateOnLoad)
        {
            this.Generate();
        }
    }

    void Update ()
    {
        this.HideShowChunks();
    }

    public void Generate ()
    {
        // Generate biomes array
        this.GenerateFishBiomeArray();

        // Generate assets dictionary
        this.GenerateAssetDictionary();
        
        // Read CSV
        this.ReadCSV();

        // Build biomes
        this.BuildFishBiomes();
    }


    void HideShowChunks ()
    {

        float chunkDiagonal =  Mathf.Sqrt (
            Mathf.Pow (this.xBiomeSize, 2) + Mathf.Pow (this.zBiomeSize, 2)
        );

        for (int i = 0; i < this.biomes.Length; i ++)
        {

            float distance = Vector2.Distance (
                new Vector2 (this.biomes[i].midCoordinates.x, this.biomes[i].midCoordinates.z),
                new Vector2 (this.player.transform.coordinates.x, this.player.transform.coordinates.z)
            );

            if (distance > this.loadRadius * chunkDiagonal) {
                this.biomes[i] .SetActive (false);
            } else {
                this.biomes[i] .SetActive (true);
            }

        }
    }


    void GenerateAssetDictionary ()
    {
        // Initialise dictionary
        this.assetDictionary = new Dictionary<string, FishAsset>();

        // Fill dictionary
        for (int i = 0; i < this.fishAssets.Length; i ++)
        {
            this.assetDictionary [this.fishAssets[i].name] = this.fishAssets[i];
        }
    }

    void GenerateBiomeArray ()
    {
        // Initialise biomes array
        this.fishBiomes = new FishBiome[xNbBiomes, zNbBiomes];

        // Fill biomes array
        for (int xBiomeId = this.xBiomeMin; xBiomeId <= this.xBiomeMax; xBiomeId ++)
        {
            for (int zBiomeId = this.zBiomeMin; zBiomeId <= this.zBiomeMax; zBiomeId ++)
            {
                // Debug.Log("generating" + xBiomeId.ToString() + "_" + zBiomeId.ToString());
                this.fishBiomes [xBiomeId, zBiomeId] = new FishBiome();
            }
        }
    }

    void ReadCSV ()
    {
        using (var reader = new StreamReader (FishConstants.settingsPathRelative))
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
                var values = line.Split (FishConstants.CSVSeparator);

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
                FishBiomeElement fishBiomeElement = new FishBiomeElement (assetName, positionRelative, rotation, scale);

                // Add to corresponding biome
                this.fishBiomes [xBiomeId, zBiomeId].biomeElements .Add(fishBiomeElement);
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
                biome.name = "FishBiome" + "_" + xBiomeId.ToString() + "_" + zBiomeId.ToString();
                biome .transform.parent = biomeWrapper.transform;

                // Get biome elements list
                List<FishBiomeElement> fishBiomeElements = this.fishBiomes[xBiomeId, zBiomeId] .fishBiomeElements;

                // Generate biome elements
                for (int elementId = 0; elementId < fishBiomeElements.Count; elementId ++)
                {

                    // Get elementSettings struct instance
                    FishBiomeElement fishBiomeElement = fishBiomeElements [elementId];

                    // Get asset
                    if (!this.assetDictionary.ContainsKey (fishBiomeElement.name)) { continue; }
                    FishAsset fishAsset = this.assetDictionary [fishBiomeElement.name];

                    // Generate biomeAsset GameObject
                    GameObject biomeElementObject = GameObject.Instantiate (fishAsset.gameObject);// .Instantiate();
                    biomeElementObject .name = elementId.ToString() + "_" + fishBiomeElement.name;
                    biomeElementObject .transform.parent = biome.transform;
                    biomeElementObject .layer = fishAsset.layerId;

                    // Set biomeAsset's parameters
                    biomeElementObject .transform.position = fishBiomeElement.positionRelative;
                    biomeElementObject .transform.rotation = Quaternion.Euler (fishBiomeElement.rotation);
                    biomeElementObject .transform.localScale = fishBiomeElement.scale;

                    // Apply material
                    if (biomeElementObject.GetComponent<MeshRenderer>() == null) { biomeElementObject .AddComponent<MeshRenderer>(); }
                    MeshRenderer meshRenderer = biomeElementObject .GetComponent<MeshRenderer>();
                    meshRenderer .material = (Material)Instantiate (fishAsset.material);

                    //MeshCollider meshCollider = 
                    if (fishAsset.addMeshCollider) { biomeElementObject .AddComponent<MeshCollider>(); }

                    // Add FishMovement script
                    FishMovement fishMovement = biomeElementObject .AddComponent<FishMovement>();
                    fishMovement .maxHeadingDeflectionAngle = 30f;
                    fishMovement .movementForceMultiplier = 10f;
                    fishMovement dragForceMultiplier = 5f;
                    fishMovement .headingChangeProbability = 0.002f;

                    fishMovement .minCoordinates = new Vector2 (
                        transform.position.x + xBiomeId * this.xBiomeSize,
                        transform.position.z + zBiomeId * this.zBiomeSize
                    );
                    fishMovement .maxCoordinates = new Vector2 (
                        transform.position.x + (xBiomeId + 1) * this.xBiomeSize,
                        transform.position.z + (zBiomeId + 1) * this.zBiomeSize
                    );

                    fishMovement .boundaryRepulsionMultiplier = 0.5f;
                    fishMovement .boundaryRepulsionDistance = 1f;
                    fishMovement .waterHeight = 0f;
                    fishMovement .waterSurfaceRepulsionMultiplier = 10f;
                    fishMovement .waterSurfaceRepulsionDistance = 1f;
                    fishMovement .terrainChunkManager = this.terrainChunkManager;
                    fishMovement .defaultGroundHeight = -150f;

                    fishMovement .repulsionLayersStatic = new FishRepulsionLayer[] {
                        new FishRepulsionLayer (
                            "rocks",
                            20,
                            new Vector3 (),
                            25
                        ),
                    };

                    fishMovement .repulsionLayersDynamic = new FishRepulsionLayer[] {
                        new FishRepulsionLayer (
                            "fish",
                            24,
                            new Vector3 (),
                            5
                        ),
                    };

                    fishMovement .globalRepulsionMultiplier = 1f;
                    fishMovement .headingChangeMultiplier = 1f;
                    fishMovement .minDistance = 0.00001f;
                    fishMovement .detectectionRangeMultiplier = 2f;
                    fishMovement .timeMultiplier = 1f;

                    // Add other script
                    RealSpace3D_AudioSource audioSource = biomeElementObject .AddComponent<RealSpace3D_AudioSource>();
                    audioSource .rs3d_LoadAudioClip (fishAsset.audioPath);
                    
                    

                    // Hidden status
                    if (fishAsset.hidden) { 
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

                // Add to biome list
                this.biomes .Add(biome);

            }
        }
    }

}



[System.Serializable]
public class FishAsset
{
    public string name;
    public int layerId;
    public GameObject gameObject;
    public Material material;
    public bool addMeshCollider;
    public bool hidden;
    public string audioPath;

    public Asset (string name, int layerId, GameObject gameObject, Material material, bool addMeshCollider, bool hidden, string audioPath)
    {
        this.name = name;
        this.layerId = layerId;
        this.gameObject = gameObject;
        this.material = material;
        this.addMeshCollider = addMeshCollider;
        this.hidden = hidden;
        this.audioPath = audioPath;
    }
}

// Biome element (system)
public struct FishBiomeElement
{
    public string name;
    public Vector3 positionRelative;
    public Vector3 rotation;
    public Vector3 scale;

    public FishBiomeElement (string name, Vector3 positionRelative, Vector3 rotation, Vector3 scale)
    {
        this.name = name;
        this.positionRelative = positionRelative;
        this.rotation = rotation;
        this.scale = scale;
    }
}

// Biome
public class FishBiome
{
    public List<BiomeElement> fishBiomeElements;

    public FishBiome ()
    {
        this.fishBiomeElements = new List<FishBiomeElement>();
    }
}
