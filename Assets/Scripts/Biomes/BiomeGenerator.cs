/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 BiomeGenerator v1.0
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// TODO: change biome asset naming (use incrementing numbers)
// TODO: one file per biome, and then lay biomes out on grid?
// TODO: add biome material, mesh
// TODO: make biomes permanent, instead of during runtime only

static class Constants
{
    public const string settingsPathRelative = @"./Assets/Scripts/Biomes/BiomeSettings.csv";
    public const char CSVSeparator = ',';
}


public class BiomeGenerator : MonoBehaviour
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

    [Header("Assets")]
    public Asset[] assets;
    Dictionary<string, Asset> assetDictionary;

    // Array of biomes
    Biome[,] biomes;


    void Start ()
    {
        this.Generate();
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
                Debug.Log("generating" + xBiomeId.ToString() + "_" + zBiomeId.ToString());
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
                float xPositionRelative = float.Parse (values[3]);
                float yPositionRelative = float.Parse (values[4]);
                float zPositionRelative = float.Parse (values[5]);
                float xRotation         = float.Parse (values[6]);
                float yRotation         = float.Parse (values[7]);
                float zRotation         = float.Parse (values[8]);
                float xScale            = float.Parse (values[9]);
                float yScale            = float.Parse (values[10]);
                float zScale            = float.Parse (values[11]);

                // Format data
                Vector3 positionRelative = new Vector3 (xPositionRelative, yPositionRelative, zPositionRelative);
                Vector3 rotation = new Vector3 (xRotation, yRotation, zRotation);
                Vector3 scale = new Vector3 (xScale, yScale, zScale);

                // Create BiomeElement
                BiomeElement biomeElement = new BiomeElement (assetName, positionRelative, rotation, scale);

                // Add to corresponding biome
                Debug.Log("adding_" + xBiomeId.ToString() + "_" + zBiomeId.ToString());
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
                    Debug.Log("Adding an element");

                    // Get elementSettings struct instance
                    BiomeElement biomeElement = biomeElements [elementId];

                    // Get asset
                    Asset asset = this.assetDictionary [biomeElement.name];

                    // Generate biomeAsset GameObject
                    GameObject biomeElementObject = GameObject.Instantiate (asset.gameObject);// .Instantiate();
                    biomeElementObject.name = biomeElement.name + "_" + elementId.ToString();
                    biomeElementObject .transform.parent = biome.transform;

                    // Set biomeAsset's parameters
                    biomeElementObject.transform.position = biomeElement.positionRelative;
                    biomeElementObject.transform.rotation = Quaternion.Euler (biomeElement.rotation);
                    biomeElementObject.transform.localScale = biomeElement.scale;

                    // Apply material
                    //asset.material
                }

                // Move biome
                biome .transform.position = new Vector3 (this.xBiomeSize * xBiomeId, this.generationAltitude, this.zBiomeSize * zBiomeId);
   
            }
        }
    }   
}


[System.Serializable]
public struct Asset
{
    public string name;
    public int layerId;
    public GameObject gameObject;
    public Material material;
    public bool addMeshCollider;
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
