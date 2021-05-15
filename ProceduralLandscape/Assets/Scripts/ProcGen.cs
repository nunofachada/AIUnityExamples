﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * Author: Nuno Fachada
 * */

using UnityEngine;
using LibGameAI.ProcGen;

public class ProcGen : MonoBehaviour
{
    [Header("General parameters")]

    [SerializeField]
    private float tileSize = 10f;

    [SerializeField]
    [Range(0, 1)]
    private float maxAltitude = 0.1f;

    [SerializeField]
    private int seed = 1234;

    [SerializeField]
    private bool perlinNoise = true;

    [Header("Fault parameters")]

    [SerializeField]
    private int numFaults = 0;

    [SerializeField]
    [Range(0, 1)]
    private float meanDepth = 0.01f;

    [SerializeField]
    private float decreaseDistance = 0f;

    [Header("Sandpile")]

    [SerializeField]
    private bool sandpile = false;

    [SerializeField]
    private float threshold = 4;

    [SerializeField]
    private float increment = 1;

    [SerializeField]
    private float decrement = 4;

    [SerializeField]
    private float grainDropDensity = 10;

    [SerializeField]
    private bool staticDrop = true;
    [SerializeField]
    private bool stochastic = true;

    // [Header("Thermal erosion parameters")]

    // [SerializeField]
    // [Range(0, 1)]
    // private float maxHeight = 0;

    private void Awake()
    {
        System.Random random = new System.Random(seed);

        Terrain terrain = GetComponent<Terrain>();
        int width = terrain.terrainData.heightmapResolution;
        int height = terrain.terrainData.heightmapResolution;

        float[,] heights = new float[width, height];

        float min = 0, max = float.NegativeInfinity;

        // Apply faults
        for (int i = 0; i < numFaults; i++)
        {
            Landscape.FaultModifier(
                heights, meanDepth, () => (float)random.NextDouble(),
                decreaseDistance);
        }

        // Apply Perlin noise
        if (perlinNoise)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heights[i, j] += Mathf.PerlinNoise(
                        tileSize * i / (float)width,
                        tileSize * (float)j / height);
                }
            }
        }

        // Apply sandpile
        if (sandpile)
        {
            Landscape.Sandpile(heights, threshold, increment, decrement, grainDropDensity, staticDrop, stochastic, random.Next, random.NextDouble);
        }


        // // Apply thermal erosion (not working atm)
        // if (maxHeight > 0)
        // {
        //     Landscape.ThermalErosion(heights, maxHeight);
        // }

        // Post-processing / normalizing
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (heights[i, j] < min) min = heights[i, j];
                else if (heights[i, j] > max) max = heights[i, j];
            }
        }

        if (min < 0 || max > maxAltitude)
        {
            //float modif = (max - min) / maxAltitude;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    heights[i, j] =
                        maxAltitude * (heights[i, j] - min) / (max - min);
                }
            }
        }

        // Apply terrain heights
        terrain.terrainData.SetHeights(0, 0, heights);
    }

}
