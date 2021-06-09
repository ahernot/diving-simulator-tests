﻿using System.Collections;
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


    void Start ()
    {
        this.ReadCSV();
    }

    void ReadCSV ()//string[] args)
    {
        using (var reader = new StreamReader (Constants.settingsPathRelative))
        {
            List<string> listA = new List<string>();
            List<string> listB = new List<string>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var values = line.Split (Constants.CSVSeparator);

                string lineString = "";
                for (int i = 0; i < values.Length; i ++)
                {
                    lineString += values[i];

                    if (i + 1 != values.Length)
                    {
                        lineString += " | ";
                    }   
                }

                Debug.Log(lineString);

                listA.Add(values[0]);
                listB.Add(values[1]);
            }
        }
    }
    
}



