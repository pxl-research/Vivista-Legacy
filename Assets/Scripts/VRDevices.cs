using System;
using VRstudios;

public static class VRDevices
{
	public enum LoadedSdk
	{
		None,
		OpenVr
	}

	public static LoadedSdk loadedSdk;
	public static XRInputControllerType loadedControllerSet;

	public static bool hasLeftController;
	public static bool hasRightController;
	public static bool hasNoControllers => !hasLeftController || !hasRightController;

	public static void BeginHandlingVRDeviceEvents()
	{
		//NOTE(Simon): First unassign the event handler, to make sure only 1 is ever connected
		XRInput.ControllerConnectedCallback-= OnDeviceConnect;
		XRInput.ControllerConnectedCallback += OnDeviceConnect;
		XRInput.ControllerDisconnectedMethod -= OnDeviceDisconnect;
		XRInput.ControllerDisconnectedMethod += OnDeviceDisconnect;
	}

	public static void OnDeviceConnect(Guid id, XRControllerSide side, XRInputControllerType type)
	{
		if (side == XRControllerSide.Left)
		{
			hasLeftController = true;
		}
		if (side == XRControllerSide.Right)
		{
			hasRightController = true;
		}

		loadedControllerSet = type;
	}

	public static void OnDeviceDisconnect(Guid id, XRControllerSide side, XRInputControllerType type)
	{
		if (side == XRControllerSide.Left)
		{
			hasLeftController = false;
		}
		if (side == XRControllerSide.Right)
		{
			hasRightController = false;
		}
	}
}
