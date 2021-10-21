using UnityEngine;
using VRstudios;

public class Controller : MonoBehaviour
{
	public XRController inputSource;

	public GameObject laser;
	public GameObject controllerUI;
	public GameObject hoveredGo;
	public GameObject cursor;

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

	private float lastRotateTime;
	private int lastRotateDirection;

	private int layerMask;

	void Start()
	{
		initialCursorScale = cursor.transform.localScale;
		measuringPlane = new Plane();

		XRInput.ButtonTriggerDownEvent += OnTriggerDown;
		XRInput.ButtonTriggerUpEvent += OnTriggerUp;
		XRInput.ButtonGripDownEvent += OnGripDown;
		XRInput.JoystickActiveEvent += OnRotateDown;

		layerMask = LayerMask.GetMask("UI", "WorldUI", "interactionPoints");
	}

	void Update()
	{
		//NOTE(Simon): Set laser direction based on controller type
		if (VRDevices.loadedControllerSet == XRInputControllerType.HTCVive)
		{
			laser.transform.localPosition = new Vector3(0, 0, 0.1f);
			laser.transform.localEulerAngles = new Vector3(90, 0, 0);
		}

		//NOTE(Kristof): Checking for hovered UI elements and adjusting laser length
		{
			var ray = CastRay();
			const float rayLength = 200f;

			if (Physics.Raycast(ray, out var hit, rayLength, layerMask))
			{
				uiHovering = true;
				hoveredGo = hit.transform.gameObject;
				laser.transform.localScale = new Vector3(laser.transform.localScale.x, hit.distance, laser.transform.localScale.z);
				SetCursorLocation(hit.point);
			}
			else
			{
				var reversedRay = ray.ReverseRay();
				if (Physics.Raycast(reversedRay, out hit, rayLength, layerMask))
				{
					uiHovering = true;
					hoveredGo = hit.transform.gameObject;
					laser.transform.localScale = new Vector3(laser.transform.localScale.x, rayLength - hit.distance, laser.transform.localScale.z);
					SetCursorLocation(hit.point);
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

		//NOTE(Simon): Check if controller is connected. If not, hide it.
		if (inputSource == XRController.Left)
		{
			laser.SetActive(VRDevices.hasLeftController);
		}

		if (inputSource == XRController.Right)
		{
			laser.SetActive(VRDevices.hasRightController);
		}
	}

	private void LateUpdate()
	{
		//NOTE(Simon): triggerPressed should only be true in the frame the trigger was pressed. So reset their state if state was changed last frame.
		if (inputSource == XRController.Left && VRDevices.hasLeftController
			|| inputSource == XRController.Right && VRDevices.hasRightController)
		{
			triggerPressed = false;
			triggerReleased = false;
		}
	}

	public void SetCursorLocation(Vector3 position)
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

	public Ray CastRay()
	{
		return new Ray(laser.transform.position, laser.transform.up);
	}

	private void OnRotateDown(XRControllerSide side, Vector2 value)
	{
		if (IsEventForThisController(side))
		{
			int direction = (int)Mathf.Sign(value.x);
			if (lastRotateTime + 0.5f < Time.time || direction != lastRotateDirection)
			{
				if (Mathf.Abs(value.x) > 0.5)
				{
					Debug.Log("Rotate down, event, " + side);
					OnRotate?.Invoke(direction);
					lastRotateDirection = direction;
					lastRotateTime = Time.time;
				}
			}
		}
	}

	private void OnGripDown(XRControllerSide side)
	{
		if (IsEventForThisController(side))
		{
			Debug.Log("Grip down, event, " + side);

			//NOTE(Simon): Attach to, or remove from controller the compass
			if (compassAttached)
			{
				Seekbar.AttachCompassToSeekbar();
				compassAttached = false;
			}
			else
			{
				Seekbar.AttachCompassToController(controllerUI);
				compassAttached = true;
			}
		}
	}

	private void OnTriggerDown(XRControllerSide side)
	{
		if (IsEventForThisController(side))
		{
			Debug.Log("Trigger down, event, " + side);
			//NOTE(Simon): Make laser fat when pressing trigger
			laser.transform.localScale = new Vector3(2, laser.transform.localScale.y, 2);
			triggerDown = true;
			triggerPressed = true;
		}
	}

	private void OnTriggerUp(XRControllerSide side)
	{
		if (IsEventForThisController(side))
		{
			Debug.Log("Trigger up, event, " + side);
			//NOTE(Simon): Make laser skinny when releasing trigger
			laser.transform.localScale = new Vector3(1, laser.transform.localScale.y, 1);
			triggerDown = false;
			triggerReleased = true;
		}
	}

	private bool IsEventForThisController(XRControllerSide side)
	{
		return side == XRControllerSide.Left && inputSource == XRController.Left
				|| side == XRControllerSide.Right && inputSource == XRController.Right;
	}
}
