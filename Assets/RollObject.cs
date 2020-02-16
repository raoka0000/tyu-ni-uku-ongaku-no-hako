using UnityEngine;
using System.Collections;

public class RollObject : MonoBehaviour {
	public float speed = 5.0f;
	public float r = 4;
	public GameObject[] allChildObject;
	// Use this for initialization
	void Start () {
		for (int i = 0; i < allChildObject.Length; i++) {
			float rate = i / (float)allChildObject.Length;
			float theta = 2 * Mathf.PI * rate;
			allChildObject [i].transform.position = new Vector3 (Mathf.Sin(theta)*r,0,Mathf.Cos(theta)*r);
			allChildObject [i].transform.LookAt (this.transform);
			allChildObject[i].GetComponent<Renderer> ().material.SetColor("_EmissionColor",Color.HSVToRGB (rate, 0.3f, 0.3f));
		}
	}
	
	// Update is called once per frame
	void Update () {
		gameObject.transform.Rotate(0, speed, 0f);
	}
}
