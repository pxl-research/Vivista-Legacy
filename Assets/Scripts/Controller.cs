using UnityEngine;
using Valve.VR;

public class Controller : MonoBehaviour
{
	public SteamVR_Input_Sources inputSource;

	public GameObject laser;
	//public GameObject model;
	public GameObject controllerUI;
	public GameObject hoveredGo;
	public GameObject cursor;
	//public Material highlightMaterial;

	public bool uiHovering;
	public bool compassAttached;

	public delegate void RotateHandler(int direction);
	public event RotateHandler OnRotate;

	//NOTE(Simon): true on the frame trigger is pressed
	public bool triggerPressed;
	//NOTE(Simon): true while trigger is down
	public bool triggerDown;
	//NOTE(Simon): true on the frame trigger is released
	public bool triggerReleased;

	private Vector3 initialCursorScale;
	private Plane measuringPlane;

	//private MeshRenderer trigger;
	//private MeshRenderer thumbstick;
	//private Material baseMaterial;

	private bool gripDown;

	// Use this for initialization
	void Start()
	{
		initialCursorScale = cursor.transform.localScale;
		measuringPlane = new Plane();

		SteamVR_Actions.default_Trigger[inputSource].onStateDown += OnTriggerDown;
		SteamVR_Actions.default_Trigger[inputSource].onStateUp += OnTriggerUp;
		SteamVR_Actions.default_Grip[inputSource].onStateUp += OnGripDown;
		SteamVR_Actions.default_RotateLeft[inputSource].onStateDown += OnRotateLeft;
		SteamVR_Actions.default_RotateRight[inputSource].onStateDown += OnRotateRight;
		
	}
	
	// Update is called once per frame
	void Update()
	{
		//NOTE(Simon): Set laser direction based on controller type
		if (VRDevices.loadedControllerSet == VRDevices.LoadedControllerSet.Vive)
		{
			laser.transform.localPosition = new Vector3(0, 0, 0.1f);
			laser.transform.localEulerAngles = new Vector3(90, 0, 0);
		}

		//NOTE(Kristof): Checking for hovered UI elements and adjusting laser length
		{
			RaycastHit hit;
			int layerMask = LayerMask.GetMask("UI", "WorldUI", "interactionPoints");
			var ray = CastRay();
			const float rayLength = 200f;

			if (Physics.Raycast(ray, out hit, rayLength, layerMask))
			{
				uiHovering = true;
				hoveredGo = hit.transform.gameObject;
				laser.transform.localScale = new Vector3(laser.transform.localScale.x, hit.distance, laser.transform.localScale.z);
				SetCursorLocation(hit.point, hit.distance);
			}
			else
			{
				var reversedRay = ray.ReverseRay();
				if (Physics.Raycast(reversedRay, out hit, rayLength, layerMask))
				{
					uiHovering = true;
					hoveredGo = hit.transform.gameObject;
					laser.transform.localScale = new Vector3(laser.transform.localScale.x, rayLength - hit.distance, laser.transform.localScale.z);
					SetCursorLocation(hit.point, rayLength - hit.distance);
				}
				else
				{
					HideCursor();
					uiHovering = false;
					hoveredGo = null;
					laser.transform.localScale = new Vector3(laser.transform.localScale.x, rayLength, laser.transform.localScale.z);
				}
			}
		}
	}

	private void LateUpdate()
	{
		//NOTE(Simon): triggerPressed should only be true in the frame the trigger was pressed. So reset their state if state was changed last frame.
		if (SteamVR_Actions.default_Trigger[inputSource].stateDown)
		{
			triggerPressed = false;
		}
		if (SteamVR_Actions.default_Trigger[inputSource].stateUp)
		{
			triggerReleased = false;
		}
	}

	public void SetCursorLocation(Vector3 position, float distance)
	{
		//NOTE(Simon): Radius proportional to laser length
		{
			measuringPlane.SetNormalAndPosition(laser.transform.up, laser.transform.position);
			var dist = measuringPlane.GetDistanceToPoint(position);
			cursor.SetActive(true);
			cursor.transform.position = position;
			cursor.transform.localScale = initialCursorScale * dist * .02f;
		}
	}

	public void HideCursor()
	{
		cursor.transform.position = Vector3.zero;
		cursor.SetActive(false);
	}

	//public void TutorialHighlight()
	//{
	//	if (trigger == null)
	//	{
	//		var triggerGo = model.transform.Find("trigger");
	//		if (triggerGo != null)
	//		{
	//			trigger = triggerGo.gameObject.GetComponent<MeshRenderer>();
	//		}
	//	}
	//
	//	if (trigger != null)
	//	{
	//		baseMaterial = trigger.material;
	//		trigger.materials = new[] { baseMaterial, highlightMaterial };
	//	}
	//
	//	//TODO(Kristof): Thumbstick is only used for the Ocoulus Touch controllers, The Vive controllers use trackpad. Needs to be added
	//	if (thumbstick == null)
	//	{
	//		var thumbstickGo = model.transform.Find("thumbstick");
	//		if (thumbstickGo != null)
	//		{
	//			thumbstick = thumbstickGo.gameObject.GetComponent<MeshRenderer>();
	//		}
	//	}
	//
	//	if (thumbstick != null)
	//	{
	//		baseMaterial = thumbstick.material;
	//		thumbstick.materials = new[] { baseMaterial, highlightMaterial };
	//	}
	//}
	//
	//public void ResetMaterial()
	//{
	//	if (trigger != null)
	//	{
	//		trigger.materials = new[] { baseMaterial };
	//	}
	//
	//	if (thumbstick != null)
	//	{
	//		thumbstick.materials = new[] { baseMaterial };
	//	}
	//}

	public Ray CastRay()
	{
		return new Ray(laser.transform.position, laser.transform.up);
	}

	private void OnRotateLeft(SteamVR_Action_Boolean steamVrActionBoolean, SteamVR_Input_Sources fromSource)
	{
		OnRotate?.Invoke(-1);
	}

	private void OnRotateRight(SteamVR_Action_Boolean steamVrActionBoolean, SteamVR_Input_Sources fromSource)
	{
		OnRotate?.Invoke(1);
	}

	private void OnGripDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		var compass = Seekbar.compass.transform;

		//NOTE(Simon): Attach to, or remove from controller the compass
		if (compass && controllerUI)
		{
			if (compassAttached)
			{
				Seekbar.ReattachCompass();
				compassAttached = false;
			}
			else
			{
				compass.SetParent(controllerUI.transform);
				compass.localScale = new Vector3(0.001f, 0.001f, 0.001f);
				compass.localPosition = Vector3.zero;
				compass.localEulerAngles = Vector3.zero;
				compass.GetChild(0).gameObject.SetActive(false);

				compass.gameObject.SetActive(true);
				compassAttached = true;
			}
		}
	}

	private void OnTriggerDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		//NOTE(Simon): Make laser fat when pressing trigger
		laser.transform.localScale = new Vector3(2, laser.transform.localScale.y, 2);
		triggerDown = true;
		triggerPressed = true;
	}

	private void OnTriggerUp(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
	{
		//NOTE(Simon): Make laser skinny when releasing trigger
		laser.transform.localScale = new Vector3(1, laser.transform.localScale.y, 1);
		triggerDown = false;
		triggerReleased = true;
	}

}
