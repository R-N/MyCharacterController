using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDebug : MonoBehaviour {

	public void OnCollisionEnter(Collision col){
		Debug.Log ("Collision enter");
	}

	public void OnCollisionStay(Collision col){
		Debug.Log("Collision stay");
	}
}
