/*
 Copyright Anatole Hernot, 2021
 All rights reserved

 FishMovement v1.0
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


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
        // Initialise biomes array
        this.biomes = new Biome[xNbBiomes, zNbBiomes];

        // Generate assets dictionary
        this.GenerateAssetDictionary();
        
        // Read CSV
        this.ReadCSV();
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

    void ReadCSV ()
    {
        using (var reader = new StreamReader (Constants.settingsPathRelative))
        {
            
            int lineId = 0;
            while (!reader.EndOfStream)
            {
                // Read line
                var line = reader.ReadLine();
                if (lineId == 0) { continue; }

                // Split line
                var values = line.Split (Constants.CSVSeparator);

                // Unpack line
                int   xBiomeId          = int.Parse   (values[0]);
                int   zBiomeId          = int.Parse   (values[1]);
                string assetName        =              values[2];
                int   layer             = int.Parse   (values[3]);
                float xPositionRelative = float.Parse (values[4]);
                float yPositionRelative = float.Parse (values[5]);
                float zPositionRelative = float.Parse (values[6]);
                float xRotation         = float.Parse (values[7]);
                float yRotation         = float.Parse (values[8]);
                float zRotation         = float.Parse (values[9]);
                float xScale            = float.Parse (values[10]);
                float yScale            = float.Parse (values[11]);
                float zScale            = float.Parse (values[12]);

                Vector3 positionRelative = new Vector3 (xPositionRelative, yPositionRelative, zPositionRelative);
                Vector3 rotation = new Vector3 (xRotation, yRotation, zRotation);
                Vector3 scale = new Vector3 (xScale, yScale, zScale);

                // Create BiomeElement
                BiomeElement biomeElement = new BiomeElement(assetName, positionRelative, rotation, scale);

                // Add to corresponding biome
                this.biomes [xBiomeId, zBiomeId] .biomeElements .Add(biomeElement);

                // Increment lineId
                lineId ++;
            }
        }
    }


    void BuildBiomes ()
    {
        // Will all be placed in world (NOT as children of BiomeGenerator), with an offset of this.transform.position
        // Will create objects named assetName + id

        // Generate biome visualisation panes in the Biome-0-0 parent object etc.
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
[System.Serializable]
public struct Biome
{
    public List<BiomeElement> biomeElements;
}
