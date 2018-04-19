using System;
using UnityEngine;

public class TestCube : MonoBehaviour
{
	public void OnHit()
	{
		transform.localScale *= 1.5f;
	}

	public void OnHoverStart()
	{
		gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
	}

	public void OnHoverStay()
	{
		Debug.Log("OnHoverStay");
	}

	public void OnHoverEnd()
	{
		gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
	}
}
