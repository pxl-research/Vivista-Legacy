using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Object3DPanelSphere : MonoBehaviour
{
	public Text title;
	public GameObject object3d;
	public Material transparent;

	private GameObject objectRenderer;
	private GameObject objectHolder;
	private Camera cameraVr;

	[SerializeField]
	private string filePath = "";
	[SerializeField]
	private string objectName = "";
	[SerializeField]
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private int valueX;
	private int valueY;
	private bool rotate;
	private Renderer rend;

	//NOTE(Jitse): Values used for interacting with 3D object
	private float sensitivity;
	private Vector3 mouseReference;
	private Vector3 mouseOffset;
	private Vector3 rotation;
	private bool isRotating;

	public void Init(string newTitle, List<string> newUrls, float[] newParameters)
	{
		title.text = newTitle;
		valueX = Convert.ToInt32(newParameters[1]);
		valueY = Convert.ToInt32(newParameters[2]);
		sensitivity = 0.4f;

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		if (objImporter == null)
		{
			objImporter = objectRenderer.AddComponent<ObjectImporter>();
		}
		objImporter.ImportingComplete += SetObjectProperties;

		cameraVr = GameObject.Find("VRCamera").GetComponent<Camera>();

		if (newUrls.Count > 0)
		{
			filePath = newUrls[0];
			objectName = Path.GetFileName(Path.GetDirectoryName(filePath));

			if (File.Exists(filePath))
			{
				//NOTE(Jitse): Create a parent object for the 3D object, to ensure it has the correct position for rotation
				if (GameObject.Find("/ObjectRenderer/holder_" + objectName) == null)
				{
					objectHolder = new GameObject("holder_" + objectName);
					objectHolder.transform.parent = objectRenderer.transform;
					objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);
				}
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
				//NOTE(Jitse): If the object consists of more than 1 child objects, we want to combine the meshes
				if (transforms.Length > 2)
				{
					rend = object3d.AddComponent<MeshRenderer>();
					//NOTE(Jitse): We don't want to see the combined "parent" mesh, because we already see the separate children meshes with their respective materials, so we assign a transparent material
					rend.material = transparent;
					var mainMesh = object3d.AddComponent<MeshFilter>();

					//NOTE(Jitse): Combine the meshes of the object into one mesh, to correctly calculate the bounds
					MeshFilter[] meshFilters = object3d.GetComponentsInChildren<MeshFilter>();
					CombineInstance[] combine = new CombineInstance[meshFilters.Length];

					int k = 1;
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

				//NOTE(Jitse): Set the scaling value; 100f was chosen by testing which size would be most appropriate.
				//NOTE(cont.): Lowering or raising this value respectively decreases or increases the object size.
				var scale = 100f / Math.Max(Math.Max(rend.bounds.size.x, rend.bounds.size.y), rend.bounds.size.x);

				//NOTE(Jitse): Ensure every child object has the correct position within the object.
				//NOTE(cont.): Set object position to the bounding box center, this fixes when objects have an offset from their pivot point.
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 1; j < children.Length; j++)
				{
					children[j].localPosition = -rend.bounds.center;
				}

				//NOTE(Jitse): Setting correct parameters of the object.
				var objRotation = object3d.transform.localRotation.eulerAngles;
				objRotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(objRotation);
				rotation = objRotation;
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(12);
				object3d.SetActive(false);
				EventTrigger trigger = object3d.AddComponent<EventTrigger>();
				EventTrigger.Entry entryPointerDown = new EventTrigger.Entry();
				entryPointerDown.eventID = EventTriggerType.PointerDown;
				entryPointerDown.callback.AddListener((eventData) => { OnPointerDown(); });
				EventTrigger.Entry entryPointerUp = new EventTrigger.Entry();
				entryPointerUp.eventID = EventTriggerType.PointerUp;
				entryPointerUp.callback.AddListener((eventData) => { OnPointerUp(); });
				trigger.triggers.Add(entryPointerDown);
				trigger.triggers.Add(entryPointerUp);
				if (objectHolder == null)
				{
					objectHolder = GameObject.Find("/ObjectRenderer/holder_" + objectName);
				}
				objectHolder.transform.localPosition = new Vector3(valueX, valueY, 70);

				break;
			}
		}

		//NOTE(Jitse): Makes sure to not call this again when importing a new object.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void OnEnable()
	{
		if (object3d != null)
		{
			object3d.SetActive(true);
		}

		cameraVr.cullingMask |= 1 << LayerMask.NameToLayer("3DObjects");
		cameraVr.cullingMask &= ~(1 << LayerMask.NameToLayer("interactionPoints"));
	}

	private void OnDisable()
	{
		if (object3d != null)
		{
			object3d.SetActive(false);
		}

		cameraVr.cullingMask |= 1 << LayerMask.NameToLayer("interactionPoints");
		cameraVr.cullingMask &= ~(1 << LayerMask.NameToLayer("3DObjects"));
	}

	private void Update()
	{
		if (isRotating)
		{
			mouseOffset = (Input.mousePosition - mouseReference);
			rotation.y = -(mouseOffset.x + mouseOffset.y) * sensitivity;
			object3d.transform.Rotate(rotation);
			mouseReference = Input.mousePosition;
		}
	}

	public void OnPointerUp()
	{
		isRotating = true;
		mouseReference = Input.mousePosition;
		Debug.Log("Pointer down 3D object");
	}

	public void OnPointerDown()
	{
		isRotating = false;
		Debug.Log("Pointer up 3D object");
	}
}
