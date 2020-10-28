using AsImpL;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanel : MonoBehaviour
{
	public Text title;
	public GameObject object3d;

	private GameObject objectRenderer;

	[SerializeField]
	private string filePath = "";
	[SerializeField]
	private string objectName = "";
	[SerializeField]
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;

	public void Init(string newTitle, List<string> newPaths)
	{
		title.text = newTitle;
		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		if (objImporter == null)
		{
			objImporter = objectRenderer.AddComponent<ObjectImporter>();
		}

		objImporter.ImportingComplete += SetObjectProperties;

		if (newPaths.Count >= 1)
		{
			filePath = newPaths[0];
			objectName = filePath.Substring(filePath.LastIndexOf("\\") + 1);
			objectName = objectName.Substring(0, objectName.IndexOf("."));
			objImporter.ImportModelAsync(objectName, filePath, objectRenderer.transform, importOptions);
		} 
	}

	private void SetObjectProperties()
	{
		var objects3d = objectRenderer.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < objects3d.Length; i++)
		{
			var tempObject = objects3d[i];
			if (tempObject.name == objectName)
			{
				object3d = tempObject.gameObject;
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 0; j < children.Length; j++)
				{
					children[j].localPosition = Vector3.zero;
				}
				var rotation = object3d.transform.localRotation.eulerAngles;
				rotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(rotation);
				object3d.transform.localPosition = new Vector3(0, 0, -50);
				object3d.transform.localScale = new Vector3(10, 10, 10);
				object3d.SetLayer(12);
				object3d.SetActive(false);
				
				break;
			}
		}
	}

	private void Update()
	{
		if (object3d != null)
		{
			var oldRotation = object3d.transform.localRotation.eulerAngles;
			oldRotation.y += 0.1f;
			object3d.transform.localRotation = Quaternion.Euler(oldRotation);
		}
	}

	private void OnEnable()
	{
		if (object3d != null)
		{
			object3d.SetActive(true);
		}
	}

	private void OnDisable()
	{
		if (object3d != null)
		{
			object3d.SetActive(false);
		}
	}
}
