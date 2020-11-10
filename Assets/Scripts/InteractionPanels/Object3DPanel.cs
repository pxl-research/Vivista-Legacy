using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanel : MonoBehaviour
{
	public Text title;
	public GameObject object3d;
	public Slider sliderScale;
	public Slider sliderX;
	public Slider sliderY;
	public Material transparent;

	private GameObject objectRenderer;

	[SerializeField]
	private string filePath = "";
	[SerializeField]
	private string objectName = "";
	[SerializeField]
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private float valueScaling;
	private int valueX;
	private int valueY;
	private bool rotate;
	private Renderer rend;

	public void Init(string newTitle, List<string> newPaths, float[] parameters)
	{
		title.text = newTitle;
		valueScaling = parameters[0];
		valueX = Convert.ToInt32(parameters[1]);
		valueY = Convert.ToInt32(parameters[2]);

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		if (objImporter == null)
		{
			objImporter = objectRenderer.AddComponent<ObjectImporter>();
		}

		objImporter.ImportingComplete += SetObjectProperties;

		if (newPaths.Count > 0)
		{
			filePath = newPaths[0];
			objectName = Path.GetFileName(Path.GetDirectoryName(filePath));
			objImporter.ImportModelAsync(objectName, filePath, objectRenderer.transform, importOptions);
		}

		sliderY.onValueChanged.AddListener(_ => YValueChanged());
		sliderX.onValueChanged.AddListener(_ => XValueChanged());
		sliderScale.onValueChanged.AddListener(_ => ScaleValueChanged());
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
				//NOTE(Jitse): If the object consists of more than 1 child objects
				if (transforms.Length > 2)
				{
					rend = object3d.AddComponent<MeshRenderer>();
					//NOTE(Jitse): We don't want to see the combined "parent" mesh, because we already see the separate children meshes with their respective materials, so we assign a transparent material
					rend.material = transparent;
					var mainMesh = object3d.AddComponent<MeshFilter>();

					//NOTE(Jitse): Combine the meshes of the object into one mesh, to correctly calculate the bounds
					MeshFilter[] meshFilters = object3d.GetComponentsInChildren<MeshFilter>();
					CombineInstance[] combine = new CombineInstance[meshFilters.Length];

					int k = 0;
					while (k < meshFilters.Length)
					{
						combine[k].mesh = meshFilters[k].sharedMesh;
						combine[k].transform = meshFilters[k].transform.localToWorldMatrix;

						k++;
					}

					mainMesh.mesh = new Mesh();
					mainMesh.mesh.CombineMeshes(combine);
				}
				else
				{
					rend = transforms[1].gameObject.GetComponent<MeshRenderer>();
				}
				//NOTE(Jitse): Set the scaling value; 80f was chosen by testing which size would be most appropriate.
				//NOTE(cont.): Lowering or raising this value respectively decreases or increases the object size.
				var scale = 80f / Math.Max(Math.Max(rend.bounds.size.x, rend.bounds.size.y), rend.bounds.size.x);

				//TODO(Jitse): Test stuff with the radius of the bounds for scaling, if there seem to be problems with the current solution.
				//Vector3 center = rend.bounds.center;
				//float radius = rend.bounds.extents.magnitude;

				//TODO(Jitse): Is this necessary? Current indication is yes.
				//NOTE(Jitse): Ensure every child object has the correct position within the object.
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 0; j < children.Length; j++)
				{
					children[j].localPosition = Vector3.zero;
				}

				//NOTE(Jitse): Setting correct parameters of the object.
				var rotation = object3d.transform.localRotation.eulerAngles;
				rotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(rotation);
				object3d.transform.localPosition = new Vector3(valueX, valueY, -50);
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(12);
				object3d.SetActive(false);

				break;
			}
		}

		//NOTE(Jitse): Makes sure to not call this again when importing a new object.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void Update()
	{
		if (object3d != null && rotate)
		{
			object3d.transform.Rotate(new Vector3(0, 0, 0.1f), Space.Self);
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

	public void ToggleRotate()
	{
		rotate = !rotate;
	}

	public void ScaleValueChanged()
	{
		var scale = sliderScale.value;
		object3d.GetComponent<Transform>().localScale = new Vector3(scale, scale, scale);
	}

	public void XValueChanged()
	{
		var x = (int) sliderX.value;
		var object3dRect = object3d.GetComponent<Transform>();
		var oldPos = object3dRect.localPosition;
		oldPos.x = x;
		object3dRect.localPosition = oldPos;
	}

	public void YValueChanged()
	{
		var y = (int)sliderY.value;
		var object3dRect = object3d.GetComponent<Transform>();
		var oldPos = object3dRect.localPosition;
		oldPos.y = y;
		object3dRect.localPosition = oldPos;
	}
}
