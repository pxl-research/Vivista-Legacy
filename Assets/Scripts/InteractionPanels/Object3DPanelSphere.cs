using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Valve.VR;

public class Object3DPanelSphere : MonoBehaviour
{
	public Text title;
	public GameObject object3d;
	public Material transparent;
	public Material hoverMaterial;
	public Button resetTransform;
	public SteamVR_Action_Boolean grabPinch;
	public SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.Any;

	private GameObject objectRenderer;
	private GameObject objectHolder;

	private string filePath = "";
	private string objectName = "";
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private Renderer rend;

	//NOTE(Jitse): Values used for interacting with 3D object
	private Vector3 cameraPosition;
	private float sensitivity = 0.25f;
	private float rotationSpeed = 5f;
	private float movementSpeed = 100;
	private Vector3 prevMousePos;
	private Vector3 mouseOffset;
	private bool isRotating;
	private bool isMoving;
	private bool isScaling;
	private bool mouseDown;
	private bool triggerDown;
	private MeshCollider objectCollider;
	private float oldPositionZ;

	private int objects3dLayer;
	private int interactionPointsLayer;

	public void Init(string newTitle, List<string> newUrls)
	{
		title.text = newTitle;

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		importOptions.hideWhileLoading = true;
		importOptions.inheritLayer = true;

		objImporter.ImportingComplete += SetObjectProperties;

		objects3dLayer = LayerMask.NameToLayer("3DObjects");
		interactionPointsLayer = LayerMask.NameToLayer("interactionPoints");

		cameraPosition = Camera.main.transform.position;

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
					objectHolder.layer = objects3dLayer;
					objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);
				}
			}
		}

		resetTransform.onClick.AddListener(ResetTransform);
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
				const float desiredScale = 45f;
				var scale = desiredScale / Math.Max(Math.Max(maxX, maxY), maxZ);

				//NOTE(Jitse): Ensure every child object has the correct position within the object.
				//NOTE(cont.): Set object position to the bounding box center, this fixes when objects have an offset from their pivot point.
				var children = object3d.GetComponentsInChildren<Transform>();
				for (int j = 1; j < children.Length; j++)
				{
					children[j].localPosition = -objectCenter;
				}

				//NOTE(Jitse): Setting correct parameters of the object.
				var objRotation = object3d.transform.localRotation.eulerAngles;
				objRotation.x = -90;
				object3d.transform.localRotation = Quaternion.Euler(objRotation);
				object3d.transform.localPosition = new Vector3(0, 0, 70);
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(objects3dLayer);

				objectHolder = GameObject.Find("/ObjectRenderer/holder_" + objectName);

				//NOTE(Jitse): If the user has the preview panel active when the object has been loaded, do not hide the object
				if (!isActiveAndEnabled)
				{
					objectHolder.SetActive(false);
				}

				//TODO(Jitse): Is there a way to avoid using combined meshes for hit detection?

				//NOTE(Jitse): Combine the meshes, so we can assign it to the MeshCollider for hit detection
				MeshFilter mainMesh;
				rend = objectHolder.AddComponent<MeshRenderer>();

				//NOTE(Jitse): We don't want to see the combined "parent" mesh, because we already see the separate children meshes with their respective materials, so we assign a transparent material
				rend.material = transparent;
				mainMesh = objectHolder.AddComponent<MeshFilter>();

				//NOTE(Jitse): Combine the meshes of the object into one mesh, to correctly calculate the bounds
				var meshFilters = object3d.GetComponentsInChildren<MeshFilter>();
				var combine = new CombineInstance[meshFilters.Length];

				int k = 0;
				while (k < meshFilters.Length)
				{
					combine[k].mesh = meshFilters[k].sharedMesh;
					combine[k].transform = meshFilters[k].transform.localToWorldMatrix;

					k++;
				}

				mainMesh.mesh = new Mesh();
				mainMesh.mesh.CombineMeshes(combine);

				objectCollider = objectHolder.AddComponent<MeshCollider>();
				objectCollider.convex = true;
				objectHolder.AddComponent<Rigidbody>();
				objectHolder.transform.position = new Vector3(5, 0, 10);
				break;
			}
		}

		//NOTE(Jitse): After completion, remove current event handler, so that it won't be called again when another Init is called.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void OnEnable()
	{
		if (objectHolder != null)
		{
			objectHolder.SetActive(true);
			Camera.main.cullingMask |= 1 << objects3dLayer;			Camera.main.cullingMask &= ~(1 << interactionPointsLayer);
		}
		if (grabPinch != null)
		{
			//grabPinch.AddOnChangeListener(OnTriggerPressedOrReleased, inputSource);
		}
	}

	private void OnDisable()
	{
		if (objectHolder != null)
		{
			objectHolder.SetActive(false);
			Camera.main.cullingMask |= 1 << interactionPointsLayer;
			Camera.main.cullingMask &= ~(1 << objects3dLayer);
		}
		if (grabPinch != null)
		{
			//grabPinch.RemoveOnChangeListener(OnTriggerPressedOrReleased, inputSource);
		}
	}

	private void Update()
	{
		//NOTE(Jitse): If the object hasn't been loaded yet.
		if (objectCollider != null)
		{
			//NOTE(Jitse): If mouse is over the object, change the collider's material to emphasize that the object can be interacted with.
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity) && !(isMoving || isRotating || isScaling))
			{
				rend.material = hoverMaterial;
			}
			else
			{
				rend.material = transparent;
			}
		}

		if (!(mouseDown || triggerDown))
		{
			isRotating = false;
			isMoving = false;
			isScaling = false;
		}

		if (isRotating)
		{
			object3d.transform.Rotate((Input.GetAxis("Mouse Y") * rotationSpeed), -(Input.GetAxis("Mouse X") * rotationSpeed), 0, Space.World);
		}

		if (isMoving)
		{
			var actualSpeed = movementSpeed * Time.deltaTime;
			if (Input.GetAxis("Mouse X") > 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.up, actualSpeed);
			}
			else if (Input.GetAxis("Mouse X") < 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.down, actualSpeed);
			}

			if (Input.GetAxis("Mouse Y") > 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.left, actualSpeed);
			}
			else if (Input.GetAxis("Mouse Y") < 0)
			{
				objectHolder.transform.RotateAround(cameraPosition, Vector3.right, actualSpeed);
			}
		}

		if (isScaling)
		{
			mouseOffset = Input.mousePosition - prevMousePos;
			var increase = (mouseOffset.y + mouseOffset.x) * sensitivity / 10;
			var scaling = objectHolder.transform.localScale;
			var position = objectHolder.transform.position;
			scaling.y += increase;
			scaling.x += increase;
			scaling.z += increase;
			if (scaling.x < 0)
			{
				scaling = Vector3.zero;
			}

			//NOTE(Jitse): We have to change the Z position of the object holder when scaling. (Temporary workaround.)
			//NOTE(cont.): The {offset value} has been chosen through testing.
			position.z = oldPositionZ + scaling.x * -60 + 60;
			objectHolder.transform.position = position;
			objectHolder.transform.localScale = scaling;
			prevMousePos = Input.mousePosition;
		}

		GetMouseButtonStates();
	}

	private void GetMouseButtonStates()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;

		if (Input.GetMouseButtonDown(0))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				isRotating = true;
				mouseDown = true;
				prevMousePos = Input.mousePosition;
			}
		}
		else if (Input.GetMouseButtonUp(0))
		{
			mouseDown = false;
		}

		if (Input.GetMouseButtonDown(1))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				isMoving = true;
				mouseDown = true;
			}
		}
		else if (Input.GetMouseButtonUp(1))
		{
			mouseDown = false;
		}

		if (Input.GetMouseButtonDown(2))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				oldPositionZ = objectHolder.transform.position.z;
				isScaling = true;
				mouseDown = true;
				prevMousePos = Input.mousePosition;
			}
		}
		else if (Input.GetMouseButtonUp(2))
		{
			mouseDown = false;
		}

		if (SteamVR_Actions.default_Grip.GetState(inputSource))
		{
			if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
			{
				isMoving = true;
				triggerDown = true;
				prevMousePos = ray.GetPoint(hit.distance);
			}
		}
		else
		{
			triggerDown = false;
		}
	}

	public void OnPointerUp()
	{
		isRotating = true;
		prevMousePos = Input.mousePosition;
	}

	public void OnPointerDown()
	{
		isRotating = false;
	}

	private void ResetTransform()
	{
		var objRotation = Vector3.zero;
		objRotation.x = -90;
		object3d.transform.localRotation = Quaternion.Euler(objRotation);
		objectHolder.transform.localScale = new Vector3(1, 1, 1);
		objectHolder.transform.localPosition = Vector3.zero;
		objectHolder.transform.rotation = Quaternion.Euler(Vector3.zero);
	}
}
