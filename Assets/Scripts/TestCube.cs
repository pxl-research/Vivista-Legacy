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
		hittable.onHover.AddListener(OnHover);
	}

	public void OnHit()
	{
		if (hittable.hitting)
		{
			transform.localScale *= 1.5f;
		}
	}

	public void OnHover()
	{
		if (hittable.hovering)
		{
			gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
		}
		else
		{
			gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
		}
	}
}
