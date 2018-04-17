using UnityEngine;

public class HittableCube : MonoBehaviour, IHittable
{
	// Use this for initialization
	void Start()
	{
		Player.hittables.Add(this);
	}

	void Update()
	{

	}

	public GameObject ReturnObject()
	{
		return gameObject;
	}

	public void OnHit()
	{
		transform.localScale *=  1.5f;
	}

	public void Hovering(bool hovering)
	{
		if (hovering)
		{
			Debug.Log("Oohhh...");
			gameObject.GetComponent<MeshRenderer>().material.color = Color.green;
		}
		else
		{
			Debug.Log("So lonely...");
			gameObject.GetComponent<MeshRenderer>().material.color = Color.red;
		}
	}
}
