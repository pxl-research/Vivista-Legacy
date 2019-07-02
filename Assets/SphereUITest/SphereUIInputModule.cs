using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class SphereUIInputModule: StandaloneInputModule
{
	private new Camera camera;
	private RenderTexture uiTexture;

	private readonly MouseState mouseState = new MouseState();
	private Dictionary<int, PointerEventData> pointers;
	private Dictionary<int, Vector3> directions;
	private Dictionary<int, Vector2> positions;
	private Dictionary<int, Vector2> positionResults;
	private Dictionary<int, RaycastResult> raycastResults;
	private Dictionary<int, PointerEventData.FramePressState> clickStates;

	public Controller leftController;
	public Controller rightController;

	private const int gazeId = 1;
	private const int rightControllerId = 2;
	private const int leftControllerId = 3;


	protected override void Awake()
	{
		pointers = new Dictionary<int, PointerEventData>();
		directions = new Dictionary<int, Vector3>();
		positions = new Dictionary<int, Vector2>();
		positionResults = new Dictionary<int, Vector2>();
		raycastResults = new Dictionary<int, RaycastResult>();
		clickStates = new Dictionary<int, PointerEventData.FramePressState>();

		camera = Camera.main;
		uiTexture = GetComponent<Camera>().targetTexture;
		base.Awake();
	}

	protected PointerEventData.FramePressState StateForControllerTrigger(Controller controller)
	{
		var pressed = controller.triggerPressed;
		var released = controller.triggerReleased;

		if (pressed && released)
		{
			return PointerEventData.FramePressState.PressedAndReleased;
		}
		if (pressed)
		{
			return PointerEventData.FramePressState.Pressed;
		}
		if (released)
		{
			return PointerEventData.FramePressState.Released;
		}

		return PointerEventData.FramePressState.NotChanged;
	}

	protected override MouseState GetMousePointerEventData(int id)
	{
		PointerEventData leftData;
		GetPointerData(kMouseLeftId, out leftData, true);
		
		leftData.Reset();

		//NOTE(Simon): There could be more than 1 inputdevice (VR controllers for example), so store them all in a list
		directions.Clear();
		if (XRSettings.enabled)
		{
			if (VRDevices.loadedControllerSet == VRDevices.LoadedControllerSet.NoControllers)
			{
				directions.Add(gazeId, camera.ScreenPointToRay(new Vector2(Screen.width, Screen.height)).direction);
			}
			else
			{
				if (VRDevices.hasRightController)
				{
					directions.Add(leftControllerId, rightController.GetComponent<Controller>().CastRay().direction);
				}
				if (VRDevices.hasLeftController)
				{
					directions.Add(rightControllerId, leftController.GetComponent<Controller>().CastRay().direction);
				}
			}
		}
		if (Input.mousePresent)
		{
			directions.Add(kMouseLeftId, camera.ScreenPointToRay((Vector2)Input.mousePosition).direction);
		}

		positions.Clear();
		foreach (var direction in directions)
		{
			positions.Add(direction.Key, new Vector2
			{
				x = uiTexture.width * (0.5f - Mathf.Atan2(direction.Value.z, direction.Value.x) / (2f * Mathf.PI)),
				y = uiTexture.height * (Mathf.Asin(direction.Value.y) / Mathf.PI + 0.5f)
			});
		}

		raycastResults.Clear();
		positionResults.Clear();
		foreach (var position in positions)
		{
			var tempData = new PointerEventData(eventSystem);
			tempData.Reset();

			tempData.position = position.Value;

			eventSystem.RaycastAll(tempData, m_RaycastResultCache);
			var result = FindFirstRaycast(m_RaycastResultCache);
			if (result.isValid)
			{
				raycastResults.Add(position.Key, result);
				positionResults.Add(position.Key, position.Value);
			}
			m_RaycastResultCache.Clear();
		}

		pointers.Clear();
		foreach (var kvp in raycastResults)
		{
			PointerEventData prevData;
			GetPointerData(kvp.Key, out prevData, true);

			pointers.Add(kvp.Key, new PointerEventData(eventSystem)
			{
				delta = positionResults[kvp.Key] - prevData.position,
				position = positionResults[kvp.Key],
				scrollDelta = Input.mouseScrollDelta,
				button = PointerEventData.InputButton.Left,
				pointerCurrentRaycast = raycastResults[kvp.Key],
				pointerId = kvp.Key,
			});
		}

		// copy the apropriate data into right and middle slots
		//PointerEventData rightData;
		//GetPointerData(kMouseRightId, out rightData, true);
		//CopyFromTo(leftData, rightData);
		//rightData.button = PointerEventData.InputButton.Right;
		//
		//PointerEventData middleData;
		//GetPointerData(kMouseMiddleId, out middleData, true);
		//CopyFromTo(leftData, middleData);
		//middleData.button = PointerEventData.InputButton.Middle;

		clickStates.Clear();
		clickStates.Add(leftControllerId, StateForControllerTrigger(leftController));
		clickStates.Add(rightControllerId, StateForControllerTrigger(leftController));
		clickStates.Add(kMouseLeftId, StateForMouseButton(0));
		//clickStates.Add(gazeId, null);

		//var rightControllerState = StateForControllerTrigger(rightController);
		//var mouseButtonState = StateForMouseButton(0);

		//bool pressed = leftControllerState == PointerEventData.FramePressState.Pressed | rightControllerState == PointerEventData.FramePressState.Pressed | mouseButtonState == PointerEventData.FramePressState.Pressed;
		//bool released = leftControllerState == PointerEventData.FramePressState.Released | rightControllerState == PointerEventData.FramePressState.Released | mouseButtonState == PointerEventData.FramePressState.Released;
		//
		//PointerEventData.FramePressState totalState;
		//
		//if (pressed)
		//{
		//	totalState = PointerEventData.FramePressState.Pressed;
		//}
		//else if (released)
		//{
		//	totalState = PointerEventData.FramePressState.Released;
		//}
		//else
		//{
		//	totalState = PointerEventData.FramePressState.NotChanged;
		//}

		foreach (var kvp in pointers)
		{
			mouseState.SetButtonState(PointerEventData.InputButton.Left, clickStates[kvp.Key], pointers[kvp.Key]);
		}


		Debug.Log($"Pointercount: {pointers.Count}");
		foreach (var pointer in pointers)
		{
			Debug.Log($"key {pointer.Key} == {pointer.Value.pointerId} \t {pointer.Value.button} clicked {pointer.Value.clickCount} times. {pointer.Value.hovered.Count} items in the hoverstack.");
		}


		return mouseState;
	}
}