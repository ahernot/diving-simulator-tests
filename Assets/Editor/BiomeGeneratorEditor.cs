/*
 Copyright Anatole Hernot, 2021
 Licensed to CRC Mines ParisTech
 All rights reserved

 BiomeGeneratorEditor v1.0
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (BiomeGenerator))]
public class BiomeGeneratorEditor : Editor
{
    // Override default inspector
    public override void OnInspectorGUI()
    {
        // Fetch target
        BiomeGenerator biomeGenerator = (BiomeGenerator)target;

        // Draw default inspector
        if (DrawDefaultInspector())
        {
            DrawDefaultInspector();
        }

        EditorGUILayout.Space();

        // Draw Generate button
        if (GUILayout.Button ("Generate Biomes"))
        {
            biomeGenerator.Generate();
        }

    }
}
