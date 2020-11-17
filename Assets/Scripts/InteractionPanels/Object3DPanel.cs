using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

//TODO(Jitse): Some objects have the wrong pivot/center point which messes with rotation.
//TODO(cont.): In some cases, the pivot point is so far off from the object that the object is too far away from the preview panel to see.
//TODO(cont.): Lay the responsibility of a correct pivot point on the user? Or is there a workaround?
public class Object3DPanel : MonoBehaviour
{
	public Text title;
	public Text errorText;
	public GameObject object3d;
	public Material transparent;
	public Image loadingCircle;
	public Image loadingCircleProgress;

	private GameObject objectRenderer;
	private GameObject objectHolder;

	private string filePath = "";
	private string objectName = "";
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private bool rotate;
	private Renderer rend;

	private int layer;

	public void Init(string newTitle, List<string> newPaths)
	{
		title.text = newTitle;

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();

		objImporter.ImportingComplete += SetObjectProperties;

		layer = LayerMask.NameToLayer("3DObjects");

		if (newPaths.Count > 0)
		{
			filePath = newPaths[0];
			objectName = Path.GetFileName(Path.GetDirectoryName(filePath));

			if (File.Exists(filePath))
			{
				errorText.gameObject.SetActive(false);
				//NOTE(Jitse): Create a parent object for the 3D object, to ensure it has the correct position for rotation
				if (GameObject.Find("/ObjectRenderer/holder_" + objectName) == null)
				{
					objectHolder = new GameObject("holder_" + objectName);
					objectHolder.transform.parent = objectRenderer.transform;
					objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);
				}
			}
			else
			{
				errorText.gameObject.SetActive(true);
				errorText.text = $"Error: File Not Found\n{filePath}";
			}
		}
	}

	private void SetObjectProperties()
	{
		var objects3d = objectRenderer.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < objects3d.Length; i++)
		{
			var currentObject = objects3d[i];
			if (currentObject.name == objectName)
			{
				object3d = currentObject.gameObject;

				var transforms = object3d.GetComponentsInChildren<Transform>();
				Vector3 objectCenter = Vector3.zero;

				float maxX = float.MinValue;
				float maxY = float.MinValue;
				float maxZ = float.MinValue;

				//NOTE(Jitse): Encapsulate the bounds to get the correct center of the 3D object.
				//NOTE(cont.): Also calculate the bounds size of the object.
				var meshes = object3d.GetComponentsInChildren<MeshRenderer>();
				var bounds = meshes[0].bounds;
				for (int j = 0; j < meshes.Length; j++)
				{
					var currentRend = meshes[j];
					var boundsSize = currentRend.bounds.size;
					if (boundsSize.x > maxX)
					{
						maxX = boundsSize.x;
					}
					if (boundsSize.y > maxY)
					{
						maxY = boundsSize.y;
					}
					if (boundsSize.z > maxZ)
					{
						maxZ = boundsSize.z;
					}

					if (j > 0)
					{
						bounds.Encapsulate(meshes[j].bounds);
					}
				}

				objectCenter = bounds.center;

				//NOTE(Jitse): Set the scaling value; 100f was chosen by testing which size would be most appropriate.
				//NOTE(cont.): Lowering or raising this value respectively decreases or increases the object size.
				const float desiredScale = 100f;
				var scale = desiredScale / Math.Max(Math.Max(maxX, maxY), maxZ);

				//NOTE(Jitse): Ensure every child object has the correct position within the object.
				//NOTE(cont.): Set object position to the bounding box center, this fixes when objects have an offset from their pivot point.
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 1; j < children.Length; j++)
				{
					children[j].localPosition = -objectCenter;
				}

				//NOTE(Jitse): Setting correct parameters of the object.
				var rotation = object3d.transform.localRotation.eulerAngles;
				rotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(rotation);
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(layer);

				//NOTE(Jitse): If the user has the preview panel active when the object has been loaded, do not hide the object
				if (!isActiveAndEnabled)
				{
					object3d.SetActive(false);
				}

				objectHolder = GameObject.Find("/ObjectRenderer/holder_" + objectName);
				objectHolder.transform.localPosition = new Vector3(0, 0, 0);

				break;
			}
		}

		loadingCircle.gameObject.SetActive(false);
		loadingCircleProgress.gameObject.SetActive(false);

		//NOTE(Jitse): After completion, remove current event handler, so that it won't be called again when another Init is called.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void Update()
	{
		if (object3d != null && rotate)
		{
			objectHolder.transform.Rotate(new Vector3(0, 0.1f, 0), Space.Self);
		} 
		else if (object3d == null && loadingCircle.IsActive())
		{
			var rotateSpeed = 200f;
			loadingCircleProgress.GetComponent<RectTransform>().Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
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

	private void OnDestroy()
	{
		Destroy(objectHolder);
	}

	public void ToggleRotate()
	{
		rotate = !rotate;
	}
}
