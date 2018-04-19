using System;
using UnityEngine;

public class TestCube : MonoBehaviour
{
	private Hittable hittable;

	// Use this for initialization
	void Start()
	{
		hittable = gameObject.GetComponent<Hittable>();
		hittable.onHit.AddListener(OnHit);
		hittable.onHoverStart.AddListener(OnHoverStart);
		hittable.onHoverEnd.AddListener(OnHoverEnd);
	}

	public void OnHit()
	{
		transform.localScale *= 1.5f;
	}

	public void OnHoverStart()
	{
		gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
	}

	public void OnHoverEnd()
	{
		gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
	}
}
