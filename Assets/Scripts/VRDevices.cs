using System;
using System.Linq;
using UnityEngine;

public static class VRDevices
{
	public enum LoadedSdk
	{
		None,
		OpenVr,
		Oculus
	}

	public enum LoadedControllerSet
	{
		NoControllers,
		Vive,
		Oculus
	}

	//NOTE(Kristof): This enum is used to store the last controller that send an input, only the active controller draws the laser
	public enum ActiveController
	{
		LeftController,
		RightController
	}

	public static LoadedSdk loadedSdk;
	public static LoadedControllerSet loadedControllerSet;

	public static bool hasLeftController;
	public static bool hasRightController;
	public static bool hasRemote;

	//NOTE(Kristof): These functions are no londer needed, use LoadedControllerSet
	//NOTE(Kristof): This functions checks if there are controllers that can be used for Raycasting
	public static bool HasControllerDevice()
	{
		return hasLeftController || hasRightController;
	}

	//NOTE(Kristof): These functions are no londer needed, use LoadedControllerSet
	//NOTE(Kristof): This function also checks for the remote
	public static bool HasAnyDevice()
	{
		return hasLeftController || hasRightController || hasRemote;
	}

	public static void DetectDevices()
	{
		var devices = Input.GetJoystickNames();

		switch (loadedSdk)
		{
			case LoadedSdk.Oculus:
				hasLeftController = devices.Contains("Oculus Touch Controller - Left");
				hasRightController = devices.Contains("Oculus Touch Controller - Right");
				hasRemote = devices.Contains("Oculus Remote");
				loadedControllerSet = LoadedControllerSet.Oculus;
				break;

			case LoadedSdk.OpenVr:
				//NOTE(Kristof): Better way to do this?
				if (devices.Contains("OpenVR Controller(Oculus Rift CV1 (Left Controller)) - Left") || devices.Contains("OpenVR Controller(Oculus Rift CV1 (Right Controller)) - Right"))
				{
					hasLeftController = devices.Contains("OpenVR Controller(Oculus Rift CV1 (Left Controller)) - Left");
					hasRightController = devices.Contains("OpenVR Controller(Oculus Rift CV1 (Right Controller)) - Right");
					//NOTE(kristof): OpenVR doesn't seem to detect the remote
					hasRemote = false;

					loadedControllerSet = LoadedControllerSet.Oculus;
				}
				else if (devices.Contains("OpenVR Controller(Vive. Controller MV) - Left") || devices.Contains("OpenVR Controller(Vive. Controller MV) - Right"))
				{
					hasLeftController = devices.Contains("OpenVR Controller(Vive. Controller MV) - Left");
					hasRightController = devices.Contains("OpenVR Controller(Vive. Controller MV) - Right");

					loadedControllerSet = LoadedControllerSet.Vive;
				}
				else
				{
					hasLeftController = false;
					hasRightController = false;
					hasRemote = false;
					loadedControllerSet = LoadedControllerSet.NoControllers;
				}

				break;

			case LoadedSdk.None:
				break;

			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
