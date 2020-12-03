using AsImpL;
using System;
using System.Collections.Generic;
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
	private Vector3 initialObjectPosition;
	private Vector3 initialHandPosition;
	private Vector3 currentVelocity = Vector3.zero;
	private Quaternion initialHandRotation;

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
	private bool leftTriggerDown;
	private bool rightTriggerDown;
	private bool bothTriggersDown;
	private bool leftControllerNear;
	private bool rightControllerNear;
	private bool handNearObject;

	private MeshCollider objectCollider;
	private MeshFilter mainMesh;
	private Controller controllerLeft;
	private Controller controllerRight;
	private Hand handLeft;
	private Hand handRight;
	private Hand handGrab;
	private Hand.AttachmentFlags attachmentFlags = Hand.defaultAttachmentFlags & (~Hand.AttachmentFlags.SnapOnAttach) & (~Hand.AttachmentFlags.DetachOthers) & (~Hand.AttachmentFlags.VelocityMovement);
	private Interactable interactable;
	private GrabTypes grabType;

	//NOTE(Jitse): Camera culling mask layers
	private int objects3dLayer;
	private int interactionPointsLayer;

	//NOTE(Jitse): Bool for checking current input method, currently used for depevelopment
	private bool isInputMethodGrab;

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

		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateDown += (fromAction, fromSource) => TriggerDown(fromAction, fromSource, "left");
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateDown += (fromAction, fromSource) => TriggerDown(fromAction, fromSource, "right");
		SteamVR_Actions.default_Trigger[inputSourceLeft].onStateUp += (fromAction, fromSource) => TriggerUp(fromAction, fromSource, "left");
		SteamVR_Actions.default_Trigger[inputSourceRight].onStateUp += (fromAction, fromSource) => TriggerUp(fromAction, fromSource, "right");
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
				var rigid = objectHolder.AddComponent<Rigidbody>();
				rigid.isKinematic = true;
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

	private void LateUpdate()
	{
		//NOTE(Jitse): If the object hasn't been loaded yet.
		if (objectCollider != null)
		{
			//NOTE(Jitse): If mouse is over the object, change the collider's material to emphasize that the object can be interacted with.
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			bool isControllerHovering = (controllerLeft != null && (controllerLeft.object3dHovering || handLeft.hoveringInteractable)) 
										|| (controllerRight != null && (controllerRight.object3dHovering || handRight.hoveringInteractable));

			bool isMouseHovering = (objectCollider.Raycast(ray, out _, Mathf.Infinity) 
									&& controllerLeft == null && controllerRight == null);

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
				var scaling = objectHolder.transform.localScale;
				var position = objectHolder.transform.position;
				scaling.x = Mathf.Clamp(scaling.x + increase, minScale, maxScale);
				scaling.y = Mathf.Clamp(scaling.y + increase, minScale, maxScale);
				scaling.z = Mathf.Clamp(scaling.z + increase, minScale, maxScale);

				objectHolder.transform.position = position;
				objectHolder.transform.localScale = scaling;
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

				if (newScale.x > minScale && newScale.x < maxScale)
				{
					objectHolder.transform.localScale = newScale;
				}
			}
			else
			{
				if (isInputMethodGrab)
				{
					//NOTE(Jitse): Hide the respective controller's laser if it's close to the object, to help visualize that it is within range to grab
					HideOrShowLaser();

					if (interactable == null)
					{
						interactable = objectHolder.GetComponentInChildren<Interactable>();
					}

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
				else
				{
					if (leftTriggerDown)
					{
						var newControllerPosition = initialHandPosition - controllerLeft.transform.position;
						objectHolder.transform.position = initialObjectPosition - newControllerPosition;

						var handRotation = initialHandRotation * Quaternion.Inverse(controllerLeft.transform.rotation);
						objectHolder.transform.rotation = Quaternion.Inverse(handRotation) * objectHolder.transform.rotation;
						initialHandRotation = controllerLeft.transform.rotation;
					}
					else if (rightTriggerDown)
					{
						var newControllerPosition = initialHandPosition - controllerRight.transform.position;
						objectHolder.transform.position = initialObjectPosition - newControllerPosition;

						var handRotation = initialHandRotation * Quaternion.Inverse(controllerRight.transform.rotation);
						objectHolder.transform.rotation = Quaternion.Inverse(handRotation) * objectHolder.transform.rotation;
						initialHandRotation = controllerRight.transform.rotation;
					}
				}
			}
		}

		if (!(mouseDown || leftTriggerDown || rightTriggerDown || handGrab != null))
		{
			MouseLook.Instance.forceInactive = false;
			isRotating = false;
			isMoving = false;
			isScaling = false;
		}
	}

	private void HideOrShowLaser()
	{
		if (handLeft.hoveringInteractable)
		{
			controllerLeft.laser.SetActive(false);
			leftControllerNear = true;
		}
		else
		{
			controllerLeft.laser.SetActive(true);
			leftControllerNear = false;
		}
		if (handRight.hoveringInteractable)
		{
			controllerRight.laser.SetActive(false);
			rightControllerNear = true;
		}
		else
		{
			controllerRight.laser.SetActive(true);
			rightControllerNear = false;
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

	private void TriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, string controllerType)
	{
		Controller controller;
		Hand hand;
		bool otherTriggerDown;
		bool controllerNear;

		if (controllerType.Equals("left"))
		{
			controller = controllerLeft;
			hand = handLeft;
			otherTriggerDown = rightTriggerDown;
			controllerNear = leftControllerNear;
		}
		else
		{
			controller = controllerRight;
			hand = handRight;
			otherTriggerDown = leftTriggerDown;
			controllerNear = rightControllerNear;
		}

		if (objectHolder != null)
		{
			//NOTE(Jitse): Grab object if controller cursor is over object or if object near hand
			//NOTE(cont.): Two different interaction methods
			if (isInputMethodGrab)
			{
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
					if (controller.object3dHovering || controllerNear)
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
			else
			{
				if (controller.object3dHovering || handNearObject)
				{
					isMoving = true;
					controller.laser.SetActive(false);

					if (otherTriggerDown)
					{
						bothTriggersDown = true;
						isScaling = true;
						isMoving = false;

						prevObjectScale = objectHolder.transform.localScale;
						startDistanceControllers = (controllerLeft.transform.position - controllerRight.transform.position).magnitude;
					}
					else
					{
						initialObjectPosition = objectHolder.transform.position;
						initialHandPosition = controller.transform.position;
						initialHandRotation = controller.transform.rotation;

						if (controllerType.Equals("left"))
						{
							leftTriggerDown = true;
						}
						else
						{
							rightTriggerDown = true;
						}
					}

					handNearObject = false;
				}
			}
		}
	}

	private void TriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource, string controllerType)
	{
		Controller controller;
		Controller otherController;
		Hand hand;

		if (controllerType.Equals("left"))
		{
			controller = controllerLeft;
			otherController = controllerRight;
			hand = handLeft;
		}
		else
		{
			controller = controllerRight;
			otherController = controllerLeft;
			hand = handRight;
		}

		if (objectHolder != null)
		{
			if (controllerType.Equals("left"))
			{
				leftTriggerDown = false;
			}
			else
			{
				rightTriggerDown = false;
			}

			isScaling = false;

			controller.laser.SetActive(true);

			//NOTE(Jitse): Different actions depending on the interaction mode
			if (isInputMethodGrab)
			{
				if (bothTriggersDown)
				{
					if (hand == handGrab)
					{
						handGrab.DetachObject(objectHolder);
						handGrab.HoverUnlock(interactable);
						handGrab = hand.otherHand;
					}

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
			else
			{
				if (bothTriggersDown)
				{
					if (controllerType.Equals("left"))
					{
						rightTriggerDown = true;
					}
					else
					{
						leftTriggerDown = true;
					}

					isMoving = true;
					bothTriggersDown = false;

					initialObjectPosition = objectHolder.transform.position;
					initialHandRotation = otherController.transform.rotation;
					initialHandPosition = otherController.transform.position;
				}
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

	public void ChangeInputMethod()
	{
		isInputMethodGrab = !isInputMethodGrab;
	}
}
