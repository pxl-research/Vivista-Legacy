using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
	public GameObject laser;
	public GameObject model;
	public Material highlightMaterial;

	private MeshRenderer trigger;
	private Material baseMaterial;

	private SteamVR_TrackedController controller;

	private static List<Controller> controllerList;
	private bool uiHovering;

	// Use this for initialization
	void Start()
	{
		controller = GetComponent<SteamVR_TrackedController>();

		controllerList = controllerList ?? new List<Controller>();
		controllerList.Add(this);
	}

	// Update is called once per frame
	void Update()
	{
		if (controller.triggerPressed)
		{
			laser.transform.localScale = new Vector3(2, laser.transform.localScale.y, 2);
		}
		else
		{
			laser.transform.localScale = new Vector3(1, laser.transform.localScale.y, 1);
		}

		var ray = CastRay();
		RaycastHit hit;
		Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("Seekbar"));

		//NOTE(Kristof): Shortening the laser when it hits the seekbar
		if (hit.transform != null)
		{
			laser.transform.localScale = new Vector3(laser.transform.localScale.x, hit.distance, laser.transform.localScale.z);
		}
		else
		{
			laser.transform.localScale = new Vector3(laser.transform.localScale.x, 100f, laser.transform.localScale.z);
		}
		if (VRDevices.loadedControllerSet == VRDevices.LoadedControllerSet.Vive)
		{
			laser.transform.localPosition = new Vector3(0, 0, 0.1f);
			laser.transform.localEulerAngles = new Vector3(90, 0, 0);
		}

		//NOTE(Kristof): Calling Hovering for hittable UI elements
		Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("UI"));

		foreach (var hittable in Player.hittables)
		{
			if (hit.transform != null && hit.transform.gameObject == hittable.ReturnObject())
			{
				uiHovering = true;
			}
			else
			{
				uiHovering = false;
			}
			OnHover(hittable);
		}
	}

	public void TriggerHighlight()
	{
		if (trigger == null)
		{
			var triggerGo = model.transform.Find("trigger");
			if (triggerGo != null)
			{
				trigger = triggerGo.gameObject.GetComponent<MeshRenderer>();
			}
		}

		if (trigger != null)
		{
			baseMaterial = trigger.material;
			trigger.materials = new[] { baseMaterial, highlightMaterial };
		}
	}

	public void ResetTriggerMaterial()
	{
		if (trigger != null)
		{
			trigger.materials = new[] { baseMaterial };
		}
	}

	public Ray CastRay()
	{
		return new Ray(laser.transform.position, laser.transform.up);
	}

	public static void OnHover(IHittable hittable)
	{
		if (controllerList[0].uiHovering || controllerList[1].uiHovering)
		{
			hittable.Hovering(true);
		}
		else
		{
			hittable.Hovering(false);
		}
	}
}
