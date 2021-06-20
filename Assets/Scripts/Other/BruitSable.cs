using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class BruitSable : MonoBehaviour{
    public AudioSource audiosource;
	public float volume = 1;
    public GameObject terrainChunkManager;
    TerrainChunkManager terrainChunkManagerScript;

	
	void Start(){
        try {
            terrainChunkManagerScript = terrainChunkManager.GetComponent<TerrainChunkManager>();
        } catch {
            terrainChunkManagerScript = null;
        };
    }

	void Update () {
        float y_sol = terrainChunkManagerScript.GetHeightAtPosition(transform.position.x, transform.position.z);
        float distance_sol = transform.position.y - y_sol;
        if (distance_sol <= 8 && !audiosource.isPlaying){
            audiosource.PlayOneShot(audiosource.clip, volume);
        }
        else if(distance_sol > 8){
            audiosource.Stop();
        }
	}

}