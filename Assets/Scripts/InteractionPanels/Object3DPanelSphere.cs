using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Object3DPanelSphere : MonoBehaviour
{
	public Text title;
	public GameObject object3d;
	public Material transparent;
	public Material hoverMaterial;
	public Button resetTransform;
	public SteamVR_Input_Sources inputSourceLeft = SteamVR_Input_Sources.LeftHand;
	public SteamVR_Input_Sources inputSourceRight = SteamVR_Input_Sources.RightHand;

	private GameObject objectRenderer;
	private GameObject objectHolder;
	private GameObject attachmentPoint;

	private string filePath = "";
	private string objectName = "";
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private Renderer rend;
	private UISphere uiSphere;

	//NOTE(Jitse): Values used for interacting with 3D object
	private Vector3 prevMousePos;
	private Vector3 prevObjectScale;
	private Quaternion initialObjectRotation;
	private Quaternion initialHandRotation;
	private Vector3 mouseDelta;

	private float startDistanceControllers;
	private float rotationSensitivity = 250f;
	private float scaleSensitivity = .1f;
	private float centerOffset = 90f;
	private float objectDistance = 1f;

	private bool isRotating;
	private bool isMoving;
	private bool isScaling;

	private bool mouseDown;
	private bool leftTriggerDown;
	private bool rightTriggerDown;
	private bool bothTriggersDown;

	private bool leftControllerFound;
	private bool rightControllerFound;

	private MeshCollider objectCollider;
	private MeshFilter mainMesh;
	private Controller controllerLeft;
	private Controller controllerRight;
	private Hand handLeft;
	private Hand handRight;
	private SteamVR_Behaviour_Skeleton leftSkeleton;
	private SteamVR_Behaviour_Skeleton rightSkeleton;
	private SteamVR_RenderModel leftModel;
	private SteamVR_RenderModel rightModel;

	//NOTE(Jitse): Camera culling mask layers
	private int objects3dLayer;
	private int interactionPointsLayer;

	public void Init(string newTitle, List<string> newUrls)
	{
		title.text = newTitle;

		if (newUrls.Count > 0)
		{
			filePath = newUrls[0];
		}

		objectRenderer = GameObject.Find("ObjectRenderer");
		objImporter = objectRenderer.GetComponent<ObjectImporter>();
		importOptions.hideWhileLoading = true;
		importOptions.inheritLayer = true;

		objects3dLayer = LayerMask.NameToLayer("3DObjects");
		interactionPointsLayer = LayerMask.NameToLayer("interactionPoints");

		resetTransform.onClick.AddListener(ResetTransform);

		var controllers = FindObjectsOfType<Controller>();
		var skeletons = FindObjectsOfType<SteamVR_Behaviour_Skeleton>();
		var models = FindObjectsOfType<SteamVR_RenderModel>();

		foreach (var controller in controllers)
		{
			if (controller.name == "LeftHand")
			{
				controllerLeft = controller;
				handLeft = controller.transform.GetComponent<Hand>();
			}
			else if (controller.name == "RightHand")
			{
				controllerRight = controller;
				handRight = controller.transform.GetComponent<Hand>();
			}
		}

		foreach (var skeleton in skeletons)
		{
			if (skeleton.name == "vr_glove_left_model_slim(Clone)")
			{
				leftSkeleton = skeleton;
				leftControllerFound = true;
			}
			else
			{
				rightSkeleton = skeleton;
				rightControllerFound = true;
			}
		}

		foreach (var model in models)
		{
			if (model.transform.parent.name == "LeftRenderModel Slim(Clone)")
			{
				leftModel = model;
			}
			else
			{
				rightModel = model;
			}
		}

		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateDown += TriggerDownLeft;
		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateUp += TriggerUpLeft;
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateDown += TriggerDownRight;
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateUp += TriggerUpRight;
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
					maxX = Math.Max(maxX, boundsSize.x);
					maxY = Math.Max(maxY, boundsSize.y);
					maxZ = Math.Max(maxZ, boundsSize.z);

					if (j > 0)
					{
						bounds.Encapsulate(meshes[j].bounds);
					}
				}

				var objectCenter = bounds.center;

				//NOTE(Jitse): Set the scaling value; this value was chosen by testing which size would be most appropriate.
				//NOTE(cont.): Lowering or raising this value respectively decreases or increases the object size.
				const float desiredScale = 0.5f;
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
				object3d.transform.localPosition = new Vector3(0, 0, 0);
				object3d.transform.localScale = new Vector3(scale, scale, scale);
				object3d.SetLayer(objects3dLayer);

				//NOTE(Jitse): Combine the meshes, so we can assign it to the MeshCollider for hit detection
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
				//var rigidbody = objectHolder.AddComponent<Rigidbody>();
				var interactable = objectHolder.AddComponent<Interactable>();
				//objectCollider.convex = true;
				interactable.attachEaseIn = true;
				interactable.hideHandOnAttach = false;
				interactable.hideHighlight = new GameObject[] { object3d };

				//NOTE(Jitse): If the user has the preview panel active when the object has been loaded, do not hide the object
				//NOTE(cont.): Also get SphereUIRenderer to position the 3D object in the center of the sphere panel
				if (isActiveAndEnabled)
				{
					uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
					objectHolder.transform.localPosition = new Vector3(0, 1, objectDistance);
					objectHolder.transform.RotateAround(Camera.main.transform.position, Vector3.up, uiSphere.offset + centerOffset);
				} 
				else
				{
					objectHolder.SetActive(false);
				}
				break;
			}
		}

		//NOTE(Jitse): After completion, remove current event handler, so that it won't be called again when another Init is called.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void OnEnable()
	{
		if (objectHolder == null)
		{
			objectName = Path.GetFileName(Path.GetDirectoryName(filePath));

			if (File.Exists(filePath))
			{
				//NOTE(Jitse): Create a parent object for the 3D object, to ensure it has the correct position for rotation
				if (GameObject.Find("/ObjectRenderer/holder_" + objectName) == null)
				{
					objectHolder = new GameObject("holder_" + objectName);
					objectHolder.transform.parent = objectRenderer.transform;
					objectHolder.layer = objects3dLayer;
					
					objImporter.ImportingComplete += SetObjectProperties;
					objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);

					//NOTE(Jitse): Use SphereUIRenderer to get the offset to position the 3D object in the center of the window.
					//NOTE(cont.): Get SphereUIRenderer object here, because it would be inactive otherwise.
					if (uiSphere == null && object3d != null)
					{
						uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
						objectHolder.transform.localPosition = new Vector3(0, 1, objectDistance);
						objectHolder.transform.RotateAround(Camera.main.transform.position, Vector3.up, uiSphere.offset + centerOffset);
					}
				}
			}
		}

		//NOTE(Jitse): Prevents null reference errors, which could occur if the object file could not be found
		if (objectHolder != null)
		{
			objectHolder.SetActive(true);
			
			Camera.main.cullingMask |= 1 << objects3dLayer;
			Camera.main.cullingMask &= ~(1 << interactionPointsLayer);
		}
	}

	private void OnDisable()
	{
		//NOTE(Jitse): Prevents null reference errors, which could occur if the object file could not be found
		if (objectHolder != null)
		{
			objectHolder.SetActive(false);
			Camera.main.cullingMask |= 1 << interactionPointsLayer;
			Camera.main.cullingMask &= ~(1 << objects3dLayer);
		}
	}

	private void Update()
	{
		//NOTE(Jitse): If an active controller hasn't been found on Init, try to find it every frame until it is found.
		if (!leftControllerFound 
			&& controllerLeft != null
			&& controllerLeft.isActiveAndEnabled)
		{
			var skeletons = FindObjectsOfType<SteamVR_Behaviour_Skeleton>();
			var models = FindObjectsOfType<SteamVR_RenderModel>();

			foreach (var skeleton in skeletons)
			{
				if (skeleton.name == "vr_glove_left_model_slim(Clone)")
				{
					leftSkeleton = skeleton;
					leftControllerFound = true;
					break;
				}
			}

			foreach (var model in models)
			{
				if (model.transform.parent.name == "LeftRenderModel Slim(Clone)")
				{
					leftModel = model;
					break;
				}
			}
		}
		if (!rightControllerFound 
			&& controllerRight != null 
			&& controllerRight.isActiveAndEnabled)
		{
			var skeletons = FindObjectsOfType<SteamVR_Behaviour_Skeleton>();
			var models = FindObjectsOfType<SteamVR_RenderModel>();

			foreach (var skeleton in skeletons)
			{
				if (skeleton.name == "vr_glove_right_model_slim(Clone)")
				{
					rightSkeleton = skeleton;
					rightControllerFound = true;
					break;
				}
			}

			foreach (var model in models)
			{
				if (model.transform.parent.name == "RightRenderModel Slim(Clone)")
				{
					rightModel = model;
					break;
				}
			}
		}
	}

	private void LateUpdate()
	{
		mouseDelta = Input.mousePosition - prevMousePos;
		//NOTE(Jitse): If the object hasn't been loaded yet.
		if (objectCollider != null)
		{
			//NOTE(Jitse): If mouse is over the object, change the collider's material to emphasize that the object can be interacted with.
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			bool isControllerHovering = (controllerLeft != null && (controllerLeft.object3dHovering || handLeft.hoveringInteractable)) 
										|| (controllerRight != null && (controllerRight.object3dHovering || handRight.hoveringInteractable));

			bool isMouseHovering = (objectCollider.Raycast(ray, out _, Mathf.Infinity) && controllerLeft == null && controllerRight == null);

			if ((isMouseHovering || isControllerHovering) && !(isMoving || isRotating || isScaling))
			{
				rend.material = hoverMaterial;
			}
			else
			{
				rend.material = transparent;
			}
		}

		//NOTE(Jitse): Object 3D interactions when not in VR
		if (controllerLeft == null && controllerRight == null)
		{
			//NOTE(Jitse): Rotate objectHolders by rotating them around their child object.
			if (isRotating)
			{
				var speedHorizontal = Input.GetAxis("Mouse X") * rotationSensitivity * Time.deltaTime;
				var speedVertical = Input.GetAxis("Mouse Y") * rotationSensitivity * Time.deltaTime;

				objectHolder.transform.RotateAround(object3d.transform.position, Vector3.down, speedHorizontal);
				objectHolder.transform.RotateAround(object3d.transform.position, object3d.transform.right, speedVertical);
			}

			//NOTE(Jitse): Move objects
			if (isMoving)
			{
				objectHolder.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(prevMousePos.x, prevMousePos.y, objectDistance));
			}

			if (isScaling)
			{
				var increase = (mouseDelta.y + mouseDelta.x) * scaleSensitivity / 10;
				var scaling = objectHolder.transform.localScale;
				var position = objectHolder.transform.position;
				scaling.x = Mathf.Clamp(scaling.x + increase, 0.5f, 5);
				scaling.y = Mathf.Clamp(scaling.y + increase, 0.5f, 5);
				scaling.z = Mathf.Clamp(scaling.z + increase, 0.5f, 5);

				objectHolder.transform.position = position;
				objectHolder.transform.localScale = scaling;
			}

			GetMouseButtonStates();
			prevMousePos = Input.mousePosition;
		}
		//NOTE(Jitse): Object 3D interactions when in VR
		//TODO(Jitse): Instead of object teleporting to hand, make them slowly move towards the hand when trigger down (maybe using controller.cursor position?)
		else
		{
			if (bothTriggersDown)
			{
				float distance = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
				float scale = 1 + (distance - startDistanceControllers);
				var newScale = prevObjectScale;
				newScale *= scale;
				objectHolder.transform.localScale = newScale;
			}

			if (leftTriggerDown)
			{
				var handRotation = initialHandRotation * controllerLeft.transform.rotation;
				var handAttachmentPoint = Vector3.MoveTowards(controllerLeft.transform.position, controllerLeft.cursor.transform.position, 0.01f);
				objectHolder.transform.position = handAttachmentPoint;
				objectHolder.transform.rotation = handRotation * initialObjectRotation;
			}
			else if (rightTriggerDown)
			{
				var handRotation = initialHandRotation * controllerRight.transform.rotation;
				var handAttachmentPoint = Vector3.MoveTowards(controllerRight.transform.position, controllerRight.cursor.transform.position, 0.01f);
				objectHolder.transform.position = handAttachmentPoint;
				objectHolder.transform.rotation = handRotation * initialObjectRotation;
			}
		}

		if (!(mouseDown || leftTriggerDown || rightTriggerDown))
		{
			MouseLook.Instance.forceInactive = false;
			isRotating = false;
			isMoving = false;
			isScaling = false;
		}
	}

	private void GetMouseButtonStates()
	{
		if (object3d != null)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			//NOTE(Jitse): Left mouse button click for rotation
			if (Input.GetMouseButtonDown(0))
			{
				if (objectCollider.Raycast(ray, out _, Mathf.Infinity))
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

			//NOTE(Jitse): Right mouse button click for movement
			if (Input.GetMouseButtonDown(1))
			{
				if (objectCollider.Raycast(ray, out _, Mathf.Infinity))
				{
					MouseLook.Instance.forceInactive = true;
					isMoving = true;
					mouseDown = true;
				}
			}
			else if (Input.GetMouseButtonUp(1))
			{
				mouseDown = false;
			}

			//NOTE(Jitse): Middle mouse button click for scaling
			if (Input.GetMouseButtonDown(2))
			{
				if (objectCollider.Raycast(ray, out _, Mathf.Infinity))
				{
					isScaling = true;
					mouseDown = true;
					prevMousePos = Input.mousePosition;
				}
			}
			else if (Input.GetMouseButtonUp(2))
			{
				mouseDown = false;
			}
		}
	}

	private void TriggerDownLeft(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (objectHolder != null)
		{
			//NOTE(Jitse): Find closest vertex hit by multiplying the hit triangle index by 3 and determining largest x, y or z from the barycentricCoordinate.
			Ray ray = new Ray(controllerLeft.laser.transform.position, controllerLeft.laser.transform.up);
			RaycastHit hit;
			if (!rightTriggerDown && Physics.Raycast(ray, out hit, Mathf.Infinity))
			{
				CalculateAttachmentPoint(hit);
			}

			//NOTE(Jitse): Grab object if controller cursor is over object or if object near hand
			if (controllerLeft.object3dHovering || (handLeft.hoveringInteractable))
			{
				isMoving = true;
				controllerLeft.laser.SetActive(false);

				if (rightTriggerDown)
				{
					bothTriggersDown = true;
					isScaling = true;
					isMoving = false;

					prevObjectScale = objectHolder.transform.localScale;
					startDistanceControllers = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
				}
				else
				{
					initialObjectRotation = objectHolder.transform.rotation;
					initialHandRotation = controllerLeft.transform.rotation;

					leftTriggerDown = true;
				}
			}
		}
	}

	private void TriggerDownRight(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (objectHolder != null)
		{
			//NOTE(Jitse): Find closest vertex hit by multiplying the hit triangle index by 3 and determining largest x, y or z from the barycentricCoordinate.
			Ray ray = new Ray(controllerRight.laser.transform.position, controllerRight.laser.transform.up);
			RaycastHit hit;
			if (!leftTriggerDown && Physics.Raycast(ray, out hit, Mathf.Infinity))
			{
				CalculateAttachmentPoint(hit);
			}

			//NOTE(Jitse): Grab object if controller cursor is over object or if object near hand
			if (controllerRight.object3dHovering || (handRight.hoveringInteractable))
			{
				isMoving = true;
				controllerRight.laser.SetActive(false);

				if (leftTriggerDown)
				{
					bothTriggersDown = true;
					isScaling = true;
					isMoving = false;

					prevObjectScale = objectHolder.transform.localScale;
					startDistanceControllers = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
				}
				else
				{
					initialObjectRotation = objectHolder.transform.rotation;
					initialHandRotation = controllerRight.transform.rotation;

					rightTriggerDown = true;
				}
			}
		}
	}

	private void TriggerUpLeft(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (objectHolder != null)
		{
			leftTriggerDown = false;
			isScaling = false;

			controllerLeft.laser.SetActive(true);

			if (bothTriggersDown)
			{
				isMoving = true;
				rightTriggerDown = true;
				bothTriggersDown = false;

				initialObjectRotation = objectHolder.transform.rotation;
				initialHandRotation = controllerRight.transform.rotation;
			} 
			else
			{
				objectHolder.transform.parent = objectRenderer.transform;
				Destroy(attachmentPoint);
			}
		}
	}

	private void TriggerUpRight(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (objectHolder != null)
		{
			rightTriggerDown = false;
			isScaling = false;

			controllerRight.laser.SetActive(true);

			if (bothTriggersDown)
			{
				isMoving = true;
				leftTriggerDown = true;
				bothTriggersDown = false;

				initialObjectRotation = objectHolder.transform.rotation;
				initialHandRotation = controllerLeft.transform.rotation;
			}
			else
			{
				objectHolder.transform.parent = objectRenderer.transform;
				Destroy(attachmentPoint);
			}
		}
	}

	private void ResetTransform()
	{
		objectHolder.transform.localScale = Vector3.one;
		objectHolder.transform.localRotation = Quaternion.Euler(Vector3.zero);
		objectHolder.transform.localPosition = new Vector3(0, 1, objectDistance);
		objectHolder.transform.RotateAround(Camera.main.transform.position, Vector3.up, uiSphere.offset + centerOffset);
	}

	private void CalculateAttachmentPoint(RaycastHit hit)
	{
		if (hit.transform.gameObject.layer == objects3dLayer)
		{
			var barycentricCoordinate = hit.barycentricCoordinate;
			int index = hit.triangleIndex * 3;

			if (barycentricCoordinate.x > barycentricCoordinate.y)
			{
				if (barycentricCoordinate.z > barycentricCoordinate.x)
				{
					index += 2;
				}
			}
			else if (barycentricCoordinate.y > barycentricCoordinate.z)
			{
				index += 1;
			}
			else
			{
				index += 2;
			}

			var triangleIndex = mainMesh.mesh.triangles[index];
			var vertexHit = mainMesh.mesh.vertices[triangleIndex];
			attachmentPoint = new GameObject("AttachmentPoint");
			attachmentPoint.transform.parent = objectRenderer.transform;
			objectHolder.transform.parent = attachmentPoint.transform;
			attachmentPoint.transform.position = vertexHit;
		}
	}
}
