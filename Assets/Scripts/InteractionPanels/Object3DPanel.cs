using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanel : MonoBehaviour
{
	public Text title;
	public GameObject object3d;

	private GameObject objectRenderer;

	public void Init(string newTitle, List<string> newPaths)
	{
		title.text = newTitle;
		objectRenderer = GameObject.Find("ObjectRenderer");

		if (newPaths.Count == 1)
		{
			//TODO LOAD OBJ
		} else
		{
			//TODO LOAD OBJ WITH MTL
		}
		object3d.transform.parent = objectRenderer.transform;

		var renderer = object3d.GetComponentInChildren<Renderer>();
		var bound = renderer.bounds;
		var center = bound.center;
		var radius = bound.extents.magnitude;
		Debug.Log(bound.ToString() + "\t" + center.ToString() + "\t" + radius.ToString());

		object3d.transform.localScale = new Vector3(50, 50, 50);
		object3d.transform.localRotation = new Quaternion(0, 0, 0, 0);
		object3d.transform.localPosition = new Vector3(0, 0, -50);
		object3d.SetLayer(12);
		object3d.SetActive(false);
	}

	private void OnEnable()
	{
		object3d.SetActive(true);
	}

	private void OnDisable()
	{
		object3d.SetActive(false);
	}
}
