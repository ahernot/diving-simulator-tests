using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;


public class BancsPoissons: MonoBehaviour{
    public GameObject Poisson1;
    public GameObject Poisson2;
    public GameObject Poisson3;
    public int distance;
    System.Random rand;
    int theta1;
    int theta2;
    int theta3;
    float y1;
    float y2;
    float y3;
    
	
	void Start(){
        rand = new System.Random();
        theta1 = rand.Next(360);
        theta2 = theta1 + 120 + rand.Next(-20,20);
        theta3 = theta1 + 240 + rand.Next(-20,20);
        y1 = rand.Next(-60, -20);
        y2 = rand.Next(-60, -20);
        y3 = rand.Next(-60, -20);
    }

	void Update () {
        float x = transform.position.x;
        float z = transform.position.z;
        Poisson1.transform.position = new Vector3 (x+distance*Mathf.Cos(theta1),y1,z+distance*Mathf.Sin(theta1));
        Poisson2.transform.position = new Vector3 (x+distance*Mathf.Cos(theta2),y2,z+distance*Mathf.Sin(theta2));
        Poisson3.transform.position = new Vector3 (x+distance*Mathf.Cos(theta3),y3,z+distance*Mathf.Sin(theta3)) ;
	}

}