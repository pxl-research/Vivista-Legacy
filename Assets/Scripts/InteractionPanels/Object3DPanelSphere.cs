using AsImpL;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;

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

	private string filePath = "";
	private string objectName = "";
	private ImportOptions importOptions = new ImportOptions();

	private ObjectImporter objImporter;
	private Renderer rend;
	private UISphere uiSphere;

	//NOTE(Jitse): Values used for interacting with 3D object
	private Vector3 prevMousePos;
	private Vector3 prevObjectScale;
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

	private Controller controllerLeft;
	private Controller controllerRight;
	private Transform controllerLeftAttachmentPoint;
	private Transform controllerRightAttachmentPoint;
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
				foreach (Transform trans in controllerLeft.GetComponentsInChildren<Transform>())
				{
					if (trans.name == "HoverPoint")
					{
						controllerLeftAttachmentPoint = trans;
						break;
					}
				}
			}
			else if (controller.name == "RightHand")
			{
				controllerRight = controller;
				foreach (Transform trans in controllerRight.GetComponentsInChildren<Transform>())
				{
					if (trans.name == "HoverPoint")
					{
						controllerRightAttachmentPoint = trans;
						break;
					}
				}
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

				//TODO(Jitse): Is there a way to avoid using combined meshes for hit detection?
				{ 
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
				}

				objectCollider = objectHolder.AddComponent<MeshCollider>();
				objectCollider.convex = true;

				//NOTE(Jitse): If the user has the preview panel active when the object has been loaded, do not hide the object
				//NOTE(cont.): Also get SphereUIRenderer to position the 3D object in the center of the sphere panel
				if (isActiveAndEnabled)
				{
					uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
					objectHolder.transform.localPosition = new Vector3(0, 0, objectDistance);
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
						objectHolder.transform.localPosition = new Vector3(0, 0, objectDistance);
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

			bool isControllerHovering = (controllerLeft != null && controllerLeft.object3dHovering) || (controllerRight != null && controllerRight.object3dHovering);
			if ((objectCollider.Raycast(ray, out _, Mathf.Infinity) || isControllerHovering) && !(isMoving || isRotating || isScaling))
			{
				rend.material = hoverMaterial;
			}
			else
			{
				rend.material = transparent;
			}

			
			if (controllerLeft != null && leftControllerFound)
			{
				if (controllerLeft.object3dHovering || leftTriggerDown || isScaling)
				{
					if (leftModel.gameObject.activeSelf)
					{
						controllerLeft.laser.SetActive(false);
						leftModel.gameObject.SetActive(false);
						leftSkeleton.SetTemporaryRangeOfMotion(EVRSkeletalMotionRange.WithoutController);
					}
				} 
				else
				{
					if (!leftModel.gameObject.activeSelf)
					{
						controllerLeft.laser.SetActive(true);
						leftModel.gameObject.SetActive(true);
						leftSkeleton.ResetTemporaryRangeOfMotion();
					}
				}
			}
			if (controllerRight != null && rightControllerFound)
			{
				if (controllerRight.object3dHovering || rightTriggerDown || isScaling)
				{
					if (rightModel.gameObject.activeSelf)
					{
						controllerRight.laser.SetActive(false);
						rightModel.gameObject.SetActive(false);
						rightSkeleton.SetTemporaryRangeOfMotion(EVRSkeletalMotionRange.WithoutController);
					}
				}
				else
				{
					if (!rightModel.gameObject.activeSelf)
					{
						controllerRight.laser.SetActive(true);
						rightModel.gameObject.SetActive(true);
						rightSkeleton.ResetTemporaryRangeOfMotion();
					}
				}
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

			//NOTE(Jitse): Move objects by rotating them around the VRCamera.
			if (isMoving)
			{
				var mousePos = prevMousePos;

				objectHolder.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, objectDistance));
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
				float distance = (controllerLeft.transform.position - objectHolder.transform.position).magnitude;
				Debug.Log($"{distance}\t{controllerLeft.transform.position}\t{objectHolder.transform.position}");
				if (distance > 0.2)
				{
					objectHolder.transform.position = controllerLeft.cursor.transform.position;
				} 
				else
				{
					objectHolder.transform.position = controllerLeft.laser.transform.position;
				}
				objectHolder.transform.rotation = controllerLeft.transform.rotation;
			}
			else if (rightTriggerDown)
			{
				float distance = (controllerRight.transform.position - objectHolder.transform.position).magnitude;
				Debug.Log($"{distance}\t{controllerRight.transform.position}\t{objectHolder.transform.position}");
				if (distance > 0.2)
				{
					objectHolder.transform.position = controllerRight.cursor.transform.position;
				}
				else
				{
					objectHolder.transform.position = controllerRight.laser.transform.position;
				}
				objectHolder.transform.rotation = controllerRight.transform.rotation;
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
		if (controllerLeft.object3dHovering && objectHolder != null)
		{
			isMoving = true;
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
				leftTriggerDown = true;
			}
		}
	}

	private void TriggerDownRight(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		if (controllerRight.object3dHovering && objectHolder != null)
		{
			rightTriggerDown = true;
			isMoving = true;
			if (leftTriggerDown)
			{
				bothTriggersDown = true;
				isScaling = true;
				isMoving = false;
				prevObjectScale = objectHolder.transform.localScale;
				startDistanceControllers = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;

			}
		}
	}
	private void TriggerUpLeft(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		leftTriggerDown = false;
		isScaling = false;
		if (bothTriggersDown)
		{
			isMoving = true;
			rightTriggerDown = true;
			bothTriggersDown = false;
		}
	}

	private void TriggerUpRight(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		rightTriggerDown = false;
		isScaling = false;
		if (bothTriggersDown)
		{
			isMoving = true;
			leftTriggerDown = true;
			bothTriggersDown = false;
		}
	}

	private void ResetTransform()
	{
		objectHolder.transform.localRotation = Quaternion.Euler(Vector3.zero);
		objectHolder.transform.localPosition = new Vector3(0, 0, objectDistance);
		objectHolder.transform.RotateAround(Camera.main.transform.position, Vector3.up, uiSphere.offset + centerOffset);
		objectHolder.transform.localScale = Vector3.one;
	}
}
