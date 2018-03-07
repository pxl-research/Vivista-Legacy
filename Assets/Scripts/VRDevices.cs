using System;
using System.Linq;
using UnityEngine;

public static class VRDevices
{
	public enum LoadedDevice
	{
		None,
		OpenVr,
		Oculus
	}

	public static LoadedDevice loadedDevice;

	public static bool hasLeftController;
	public static bool hasRightController;
	public static bool hasRemote;

	//NOTE(Kristof): This functions checks if there are controllers that can be used for Raycasting.
	public static bool HasControllerDevice()
	{
		return hasLeftController || hasRightController;
	}

	//NOTE(Kristof): This function also checks for the remote.
	public static bool HasAnyDevice()
	{
		return hasLeftController || hasRightController || hasRemote;
	}

	public static void DetectDevices()
	{
		var devices = Input.GetJoystickNames();

		switch (loadedDevice)
		{
			case LoadedDevice.Oculus:
				hasLeftController = devices.Contains("Oculus Touch Controller - Left");
				hasRightController = devices.Contains("Oculus Touch Controller - Right");
				hasRemote = devices.Contains("Oculus Remote");
				break;
			case LoadedDevice.OpenVr:
				hasLeftController = (devices.Contains("OpenVR Controller(Vive. Controller MV) - Left") ||
				                     devices.Contains("OpenVR Controller(Oculus Rift CV1 (Left Controller)) - Left"));
				hasRightController = (devices.Contains("OpenVR Controller(Vive. Controller MV) - Right") ||
				                      devices.Contains("OpenVR Controller(Oculus Rift CV1 (Left Controller)) - Right"));
				hasRemote = false;
				break;
			case LoadedDevice.None:
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
