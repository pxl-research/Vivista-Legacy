using UnityEngine;
using UnityEngine.XR;

public static class VRDevices
{
	public enum LoadedSdk
	{
		None,
		OpenVr
	}

	public enum LoadedControllerSet
	{
		NoControllers,
		Vive,
		Oculus
	}

	public static LoadedSdk loadedSdk;
	public static LoadedControllerSet loadedControllerSet;

	public static bool hasLeftController;
	public static bool hasRightController;

	public static void BeginHandlingVRDeviceEvents()
	{
		//NOTE(Simon): First unassign the event handler, to make sure only 1 is ever connected
		InputDevices.deviceConnected -= OnDeviceConnect;
		InputDevices.deviceConnected += OnDeviceConnect;
		InputDevices.deviceDisconnected -= OnDeviceDisconnect;
		InputDevices.deviceDisconnected += OnDeviceDisconnect;
	}

	public static void OnDeviceConnect(InputDevice device)
	{
		var name = device.name.ToLowerInvariant();
		if (name.Contains("left"))
		{
			Debug.Log("left controller connected: " + device.name);
			hasLeftController = true;
		}
		if (name.Contains("right"))
		{
			Debug.Log("right controller connected: " + device.name);
			hasRightController = true;
		}
		if (name.Contains("oculus"))
		{
			Debug.Log("oculus device connected: " + device.name);
			loadedControllerSet = LoadedControllerSet.Oculus;
		}
		else if (name.Contains("vive"))
		{
			Debug.Log("vive device connected: " + device.name);
			loadedControllerSet = LoadedControllerSet.Vive;
		}
	}

	public static void OnDeviceDisconnect(InputDevice device)
	{
		var name = device.name.ToLowerInvariant();
		if (name.Contains("left"))
		{
			hasLeftController = false;
		}
		if (name.Contains("right"))
		{
			hasRightController = false;
		}
	}

	//public static void SetControllerTutorialMode(GameObject controller, bool enabled)
	//{
	//	GameObject[] controllerArray = { controller };
	//	SetControllersTutorialMode(controllerArray, enabled);
	//}
	//
	//public static void SetControllersTutorialMode(GameObject[] controllers, bool enabled)
	//{
	//	foreach (var controller in controllers)
	//	{
	//		if (enabled)
	//		{
	//			controller.GetComponent<Controller>().TutorialHighlight();
	//		}
	//		else
	//		{
	//			controller.GetComponent<Controller>().ResetMaterial();
	//		}
	//	}
	//}
}
