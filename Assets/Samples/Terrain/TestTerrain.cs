using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTerrain : MonoBehaviour
{
    public Terrain terrain;

    public void DebugOutput()
    {
        TerrainData terrainData = terrain.terrainData;

        Debug.Log($"detailHeight: {terrainData.detailHeight}");
        Debug.Log($"detailResolution: {terrainData.detailResolution}");
        Debug.Log($"detailPatchCount: {terrainData.detailPatchCount}");
        Debug.Log($"terrainData.size: {terrainData.size}");
        Debug.Log($"terrain.heightmapMaximumLOD: {terrain.heightmapMaximumLOD}");
    }
}