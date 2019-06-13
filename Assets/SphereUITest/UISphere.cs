using UnityEngine;

[ExecuteInEditMode]
public class UISphere : MonoBehaviour
{
	private Material material;

	void Start()
	{
		if (Application.isEditor && material == null)
		{
			Debug.Log("new material");
			material = new Material(GetComponent<Renderer>().sharedMaterial);
			GetComponent<Renderer>().material = material;
		}
	}

	void Update()
	{
		//NOTE(Simon): + 180 so "forward" aligns both world space and shader space
		var rotation = transform.localRotation.eulerAngles.y + 180;
		material.SetFloat("offsetDegrees", rotation);
	}
}
