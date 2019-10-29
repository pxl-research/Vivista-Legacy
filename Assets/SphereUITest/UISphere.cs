using UnityEngine;
using UnityEngine.Assertions;

[ExecuteInEditMode]
public class UISphere : MonoBehaviour
{
	private Material material;
	private float offset;
	private SphereUIInputModule inputModule;

	void Start()
	{
		if (Application.isEditor && material == null)
		{
			Debug.Log("new material");
			material = new Material(GetComponent<Renderer>().sharedMaterial);
			GetComponent<Renderer>().material = material;
		}

		inputModule = FindObjectOfType<SphereUIInputModule>();
	}

	void Update()
	{
		offset += .1f;
		//NOTE(Simon): + 180 so "forward" aligns both world space and shader space
		var rotation = transform.localRotation.eulerAngles.y + 180 + offset;
		material.SetFloat("offsetDegrees", rotation);
		Assert.IsNotNull(inputModule);
		inputModule.offset = offset;
	}
}
