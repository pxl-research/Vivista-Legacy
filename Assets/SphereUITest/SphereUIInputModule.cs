using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class SphereUIInputModule: StandaloneInputModule
{
	private new Camera camera;
	private RenderTexture uiTexture;

	private Vector2 cursorPos;
	private readonly MouseState mouseState = new MouseState();

	public SteamVR_TrackedController leftController;
	public SteamVR_TrackedController rightController;

	protected override void Awake()
	{
		camera = Camera.main;
		uiTexture = GetComponent<Camera>().targetTexture;
		base.Awake();
	}

	protected override MouseState GetMousePointerEventData(int id)
	{
		PointerEventData leftData;
		bool created = GetPointerData(kMouseLeftId, out leftData, true);

		leftData.Reset();

		if (created)
		{
			leftData.position = cursorPos;
		}

		var direction = new Vector3(0, 0);
		if (XRSettings.enabled)
		{
			if (VRDevices.loadedControllerSet == VRDevices.LoadedControllerSet.NoControllers)
			{
				direction = camera.ScreenPointToRay(new Vector2(Screen.width, Screen.height)).direction;
				Debug.Log("Gaze input");
			}
			else
			{
				if (VRDevices.hasLeftController)
				{
					direction = leftController.GetComponent<Controller>().CastRay().direction;
					Debug.Log("Left controller input");
				}
				else if (VRDevices.hasRightController)
				{
					direction = rightController.GetComponent<Controller>().CastRay().direction;
					Debug.Log("Right controller input");
				}
			}
		}
		else
		{
			direction = camera.ScreenPointToRay((Vector2)Input.mousePosition).direction;
			Debug.Log("Mouse input");
		}

		Vector2 pos;
		pos.x = uiTexture.width * (0.5f - Mathf.Atan2(direction.z, direction.x) / (2f * Mathf.PI));
		pos.y = uiTexture.height * (Mathf.Asin(direction.y) / Mathf.PI + 0.5f);
		cursorPos = pos;

		// For UV-mapped meshes, you could fire a ray against its MeshCollider 
		// and determine the UV coordinates of the struck point.

		leftData.delta = pos - leftData.position;
		leftData.position = pos;
		leftData.scrollDelta = Input.mouseScrollDelta;
		leftData.button = PointerEventData.InputButton.Left;

		eventSystem.RaycastAll(leftData, m_RaycastResultCache);
		var raycast = FindFirstRaycast(m_RaycastResultCache);
		leftData.pointerCurrentRaycast = raycast;
		m_RaycastResultCache.Clear();

		// copy the apropriate data into right and middle slots
		PointerEventData rightData;
		GetPointerData(kMouseRightId, out rightData, true);
		CopyFromTo(leftData, rightData);
		rightData.button = PointerEventData.InputButton.Right;

		PointerEventData middleData;
		GetPointerData(kMouseMiddleId, out middleData, true);
		CopyFromTo(leftData, middleData);
		middleData.button = PointerEventData.InputButton.Middle;

		mouseState.SetButtonState(PointerEventData.InputButton.Left, StateForMouseButton(0), leftData);
		mouseState.SetButtonState(PointerEventData.InputButton.Right, StateForMouseButton(1), rightData);
		mouseState.SetButtonState(PointerEventData.InputButton.Middle, StateForMouseButton(2), middleData);

		return mouseState;
	}
}