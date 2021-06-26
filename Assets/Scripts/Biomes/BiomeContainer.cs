// /*
//  Copyright Anatole Hernot, 2021
//  Licensed to CRC Mines ParisTech
//  All rights reserved

//  BiomeContainer v1.0.1
// */

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;


// public class BiomeContainer : MonoBehaviour
// {
//     [Header("Biome dimensions")]
//     [Tooltip("x-axis dimension of the biome")]
//     public float xSize = 250f;
//     [Tooltip("z-axis dimension of the biome")]
//     public float zSize = 250f;

//     [Header("Vertical axis dimensions")]
//     public float yMin = -100f;
//     public float yMax = 10f;

//     [Header("Mesh color")]
//     public Color boundariesMeshColor = new Color (1f, 0f, 0f, 0.15f);

//     // Boundary coordinates (horizontal)
//     Vector2 minCoordinates;
//     Vector2 maxCoordinates;
//     public Vector2 midCoordinates;

//     // Boundaries mesh
//     Mesh boundariesMesh;


//     void Start ()
//     {
//         // Compute boundary coordinates
//         this.minCoordinates = new Vector2 (transform.position.x, transform.position.z);
//         this.maxCoordinates = this.minCoordinates + new Vector2 (this.xSize, this.zSize);

//         // Generate boundaries mesh
//         this.GenerateBoundayMesh();

//         this.midCoordinates = new Vector2 (
//             transform.position.x + this.xSize / 2f,
//             transform.position.y,
//             transform.position.z + this.zSize / 2f
//         );
//     }



//     void OnDrawGizmosSelected ()
//     {
//         Gizmos.color = this.boundariesMeshColor;
//         Gizmos.DrawMesh (this.boundariesMesh, Vector3.zero);

//         Gizmos.color = Color.black;
//         Gizmos.DrawLine (this.boundariesMesh.vertices[0], this.boundariesMesh.vertices[1]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[1], this.boundariesMesh.vertices[2]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[2], this.boundariesMesh.vertices[3]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[3], this.boundariesMesh.vertices[0]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[0], this.boundariesMesh.vertices[4]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[1], this.boundariesMesh.vertices[5]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[2], this.boundariesMesh.vertices[6]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[3], this.boundariesMesh.vertices[7]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[4], this.boundariesMesh.vertices[5]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[5], this.boundariesMesh.vertices[6]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[6], this.boundariesMesh.vertices[7]);
//         Gizmos.DrawLine (this.boundariesMesh.vertices[7], this.boundariesMesh.vertices[4]);
//     }


//     void GenerateBoundayMesh ()
//     {
//         this.boundariesMesh = new Mesh();

//         this.boundariesMesh.vertices = new Vector3[] {
//             new Vector3 (this.minCoordinates.x, this.yMin, this.minCoordinates.y),
//             new Vector3 (this.minCoordinates.x, this.yMin, this.maxCoordinates.y),
//             new Vector3 (this.maxCoordinates.x, this.yMin, this.maxCoordinates.y),
//             new Vector3 (this.maxCoordinates.x, this.yMin, this.minCoordinates.y),

//             new Vector3 (this.minCoordinates.x, this.yMax, this.minCoordinates.y),
//             new Vector3 (this.minCoordinates.x, this.yMax, this.maxCoordinates.y),
//             new Vector3 (this.maxCoordinates.x, this.yMax, this.maxCoordinates.y),
//             new Vector3 (this.maxCoordinates.x, this.yMax, this.minCoordinates.y)
//         };
        
//         this.boundariesMesh.triangles = new int[] {
//             // xMin face
//             0, 4, 5,
//             0, 5, 1,
//             0, 5, 4,
//             0, 1, 5,

//             // xMax face
//             2, 6, 7,
//             2, 7, 3,
//             2, 7, 6,
//             2, 3, 7,

//             // yMin face
//             0, 3, 7,
//             0, 7, 4,
//             0, 7, 3,
//             0, 4, 7,

//             // yMax face
//             2, 1, 5,
//             2, 5, 6,
//             2, 5, 1,
//             2, 6, 5,
//         };
        
//         this.boundariesMesh.RecalculateNormals();
//     }



// }
