using AsImpL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Hand = Valve.VR.InteractionSystem.Hand;

public class Object3DPanelSphere : MonoBehaviour
{
	public Text title;
	public Text longLoadMessage;
	public GameObject object3d;
	public Material transparent;
	public Material hoverMaterial;
	public Button resetTransform;
	public Image mouseInteractionTutorial;
	public Image loadingCircle;
	public Image loadingCircleProgress;
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
	private Transform vrCamera;
	private Stopwatch importingStopwatch;

	//NOTE(Jitse): Values used for interacting with 3D object
	private Vector3 prevMousePos;
	private Vector3 prevObjectScale;
	private Vector3 mouseDelta;
	private Vector3 currentVelocity = Vector3.zero;

	private float startDistanceControllers;
	private float rotationSensitivity = 250f;
	private float scaleSensitivity = .1f;
	private float centerOffset = 90f;
	private float objectDistance = 1f;
	private float minScale = 0.2f;
	private float maxScale = 5f;

	private bool isRotating;
	private bool isMoving;
	private bool isScaling;

	private bool mouseDown;
	private bool bothTriggersDown;

	private MeshCollider objectCollider;
	private MeshFilter mainMesh;
	private Controller controllerLeft;
	private Controller controllerRight;
	private Hand handLeft;
	private Hand handRight;
	private Hand handGrab;
	private Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags & (~Hand.AttachmentFlags.SnapOnAttach) & (~Hand.AttachmentFlags.DetachOthers);
	private Interactable interactable;
	private GrabTypes grabType;

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

		vrCamera = GameObject.Find("VRCamera").transform;

		objects3dLayer = LayerMask.NameToLayer("3DObjects");
		interactionPointsLayer = LayerMask.NameToLayer("interactionPoints");

		resetTransform.onClick.AddListener(ResetTransform);

		var controllers = FindObjectsOfType<Controller>();

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

		//NOTE(Jitse): Don't show the interaction instructions when in VR.
		if (XRSettings.enabled)
		{
			mouseInteractionTutorial.enabled = false;
		}

		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateDown += (fromAction, fromSource) => TriggerDown(fromAction, fromSource, controllerLeft, handLeft);
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateDown += (fromAction, fromSource) => TriggerDown(fromAction, fromSource, controllerRight, handRight);
		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateUp += (fromAction, fromSource) => TriggerUp(fromAction, fromSource, controllerLeft, handLeft);
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateUp += (fromAction, fromSource) => TriggerUp(fromAction, fromSource, controllerRight, handRight);
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
				var interactable = objectHolder.AddComponent<Interactable>();
				interactable = objectHolder.AddComponent<Interactable>();
				interactable.attachEaseIn = true;
				interactable.hideHandOnAttach = false;
				interactable.hideSkeletonOnAttach = false;
				interactable.hideControllerOnAttach = false;
				interactable.highlightOnHover = false;
				interactable.handFollowTransform = false;

				//NOTE(Jitse): If the user has the preview panel active when the object has been loaded, do not hide the object
				//NOTE(cont.): Also get SphereUIRenderer to position the 3D object in the center of the sphere panel
				if (isActiveAndEnabled)
				{
					uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
					ResetTransform();
				} 
				else
				{
					objectHolder.SetActive(false);
				}

				break;
			}
		}

		//NOTE(Jitse): Deactivate loading circle
		loadingCircle.gameObject.SetActive(false);
		loadingCircleProgress.gameObject.SetActive(false);
		longLoadMessage.gameObject.SetActive(false);

		//NOTE(Jitse): After completion, remove current event handler, so that it won't be called again when another Init is called.
		objImporter.ImportingComplete -= SetObjectProperties;
	}

	private void OnEnable()
	{
		//NOTE(Jitse): When first opening the panel, import the object
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

					importingStopwatch = new Stopwatch();
					importingStopwatch.Start();
					objImporter.ImportingComplete += SetObjectProperties;
					objImporter.ImportModelAsync(objectName, filePath, objectHolder.transform, importOptions);

					//NOTE(Jitse): Use SphereUIRenderer to get the offset to position the 3D object in the center of the window.
					//NOTE(cont.): Get SphereUIRenderer object here, because it would be inactive otherwise.
					if (uiSphere == null && object3d != null)
					{
						uiSphere = GameObject.Find("SphereUIRenderer").GetComponent<UISphere>();
						ResetTransform();
					}
				}
			}
		}

		//NOTE(Jitse): Prevents null reference errors, which could occur if the object file could not be found
		if (objectHolder != null)
		{
			objectHolder.SetActive(true);
			if (uiSphere != null)
			{
				ResetTransform();
			}

			Camera.main.cullingMask |= 1 << objects3dLayer;
			Camera.main.cullingMask &= ~(1 << interactionPointsLayer);
		}
	}

	private void OnDisable()
	{
		if (XRSettings.enabled)
		{
			RestoreOriginalControllerSettings();
		}

		//NOTE(Jitse): Prevents null reference errors, which could occur if the object file could not be found
		if (objectHolder != null)
		{
			objectHolder.SetActive(false);
			Camera.main.cullingMask |= 1 << interactionPointsLayer;
			Camera.main.cullingMask &= ~(1 << objects3dLayer);
		}
	}

	private void RestoreOriginalControllerSettings()
	{
		if (handLeft.ObjectIsAttached(objectHolder))
		{
			handLeft.DetachObject(objectHolder);
		}
		if (handRight.ObjectIsAttached(objectHolder))
		{
			handRight.DetachObject(objectHolder);
		}

		controllerLeft.laser.SetActive(true);
		controllerRight.laser.SetActive(true);
		controllerLeft.cursor.SetActive(true);
		controllerRight.cursor.SetActive(true);
	}

	private void Update()
	{
		//NOTE(Jitse): Animate loading circle while importing object
		if (object3d == null && loadingCircle.IsActive())
		{
			var rotateSpeed = 200f;
			loadingCircleProgress.GetComponent<RectTransform>().Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

			if (importingStopwatch.IsRunning && importingStopwatch.ElapsedMilliseconds > 5000)
			{
				longLoadMessage.gameObject.SetActive(true);
			}
		}
	}

	private void LateUpdate()
	{
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
				if (rend.sharedMaterial != hoverMaterial)
				{
					rend.sharedMaterial = hoverMaterial;
				}
			}
			else
			{
				if (rend.sharedMaterial != transparent)
				{
					rend.sharedMaterial = transparent;
				}
			}
		}

		//NOTE(Jitse): Object 3D interactions when not in VR
		if (!XRSettings.enabled)
		{
			mouseDelta = Input.mousePosition - prevMousePos;

			//NOTE(Jitse): Rotate objectHolders by rotating them around their child object.
			if (isRotating)
			{
				var speedHorizontal = Input.GetAxis("Mouse X") * rotationSensitivity * Time.deltaTime;
				var speedVertical = Input.GetAxis("Mouse Y") * rotationSensitivity * Time.deltaTime;

				objectHolder.transform.RotateAround(object3d.transform.position, Vector3.down, speedHorizontal);
				objectHolder.transform.RotateAround(object3d.transform.position, object3d.transform.right, speedVertical);
			}

			if (isMoving)
			{
				objectHolder.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(prevMousePos.x, prevMousePos.y, objectDistance));
			}

			if (isScaling)
			{
				var increase = (mouseDelta.y + mouseDelta.x) * scaleSensitivity / 10;
				var scalingValue = Mathf.Clamp(objectHolder.transform.localScale.x + increase, minScale, maxScale);
				objectHolder.transform.localScale = new Vector3(scalingValue, scalingValue, scalingValue);
			}

			GetMouseButtonStates();
			prevMousePos = Input.mousePosition;
		}
		//NOTE(Jitse): Object 3D interactions when in VR
		else
		{
			//NOTE(Jitse): Scaling the object in VR
			if (bothTriggersDown)
			{
				float distance = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
				float scale = startDistanceControllers / distance;
				var newScale = prevObjectScale;
				newScale /= scale;

				var scalingValue = Mathf.Clamp(newScale.x, minScale, maxScale);
				objectHolder.transform.localScale = new Vector3(scalingValue, scalingValue, scalingValue);
			}
			else
			{
				//NOTE(Jitse): Hide the respective controller's laser if it's close to the object, to help visualize that it is within range to grab
				HideOrShowLaser();

				if (interactable == null)
				{
					interactable = objectHolder.GetComponentInChildren<Interactable>();
				}

				//NOTE(Jitse): If the object is out of reach, smoothly move it towards the grabbing hand.
				if (handGrab != null && handGrab.hoveringInteractable == null)
				{
					objectHolder.transform.position = Vector3.SmoothDamp(objectHolder.transform.position, handGrab.transform.position, ref currentVelocity, 0.2f);
				}
				else if (interactable != null)
				{
					if (handGrab != null)
					{
						bool isGrabbing = grabType == GrabTypes.Trigger;

						if (interactable.attachedToHand == null && isGrabbing)
						{
							//NOTE(Jitse): Set this here, to ensure the interactable has the right options set.
							interactable.hideHandOnAttach = false;
							interactable.hideSkeletonOnAttach = false;
							interactable.hideControllerOnAttach = false;
							interactable.highlightOnHover = false;
							interactable.handFollowTransform = false;

							handGrab.HoverLock(interactable);
							handGrab.AttachObject(objectHolder, grabType, attachmentFlags);
						}
						else if (!isGrabbing)
						{
							handGrab.DetachObject(objectHolder);
							handGrab.HoverUnlock(interactable);
						}
					}
					else
					{
						if (handLeft.ObjectIsAttached(objectHolder))
						{
							handLeft.DetachObject(objectHolder);
						}
						else if (handRight.ObjectIsAttached(objectHolder))
						{
							handRight.DetachObject(objectHolder);
						}
					}
				}
			}
		}

		if (!(mouseDown || handGrab != null))
		{
			MouseLook.Instance.forceInactive = false;
			isRotating = false;
			isMoving = false;
			isScaling = false;
		}
	}

	private void HideOrShowLaser()
	{
		bool leftLaser = !(handLeft.hoveringInteractable || bothTriggersDown);
		controllerLeft.laser.SetActive(leftLaser);
		controllerLeft.cursor.SetActive(leftLaser);

		bool rightLaser = !(handRight.hoveringInteractable || bothTriggersDown);
		controllerRight.laser.SetActive(rightLaser);
		controllerRight.cursor.SetActive(rightLaser);
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

	private void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, Controller controller, Hand hand)
	{
		if (objectHolder != null)
		{
			//NOTE(Jitse): Grab object if controller cursor is over object or if object near hand
			if (hand.hoveringInteractable)
			{
				//NOTE(Jitse): If neither hand is currently grabbing the object
				if (handGrab == null)
				{
					isMoving = true;

					handGrab = hand;
					grabType = GrabTypes.Trigger;
				}
				else
				{
					bothTriggersDown = true;
					isScaling = true;
					isMoving = false;

					handGrab.DetachObject(objectHolder);
					prevObjectScale = objectHolder.transform.localScale;
					startDistanceControllers = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
				}
			}
			else
			{
				if (controller.object3dHovering || hand.hoveringInteractable)
				{
					if (handGrab == null)
					{
						if (hand.hoveringInteractable == null)
						{
							isMoving = true;

							handGrab = hand;
							grabType = GrabTypes.Trigger;
						}
					}
					else
					{
						bothTriggersDown = true;
						isScaling = true;
						isMoving = false;

						prevObjectScale = objectHolder.transform.localScale;
						startDistanceControllers = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
					}

					controller.laser.SetActive(false);
				}
			}
		}
	}

	private void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, Controller controller, Hand hand)
	{
		if (objectHolder != null)
		{
			isScaling = false;

			controller.laser.SetActive(true);

			if (bothTriggersDown)
			{
				if (hand == handGrab)
				{
					handGrab.DetachObject(objectHolder);
					handGrab.HoverUnlock(interactable);
					handGrab = hand.otherHand;
				}

				isMoving = true;
				bothTriggersDown = false;
			}
			else
			{
				if (handGrab == hand)
				{
					handGrab = null;
					grabType = GrabTypes.None;
				}
			}
		}
	}

	private void ResetTransform()
	{
		objectHolder.transform.localScale = Vector3.one;
		objectHolder.transform.localRotation = Quaternion.Euler(Vector3.zero);
		if (XRSettings.enabled)
		{
			objectHolder.transform.localPosition = new Vector3(0, vrCamera.localPosition.y, objectDistance);
		}
		else
		{
			objectHolder.transform.localPosition = new Vector3(0, 0, objectDistance);
		}
		objectHolder.transform.RotateAround(Camera.main.transform.position, Vector3.up, uiSphere.offset + centerOffset);
	}

	private void OnDestroy()
	{
		Destroy(objectHolder);
	}
}
