using System;
using System.CodeDom;
using UnityEngine;

public class Controller : MonoBehaviour
{
	public GameObject laser;
	public GameObject model;
	public GameObject controllerUI;
	public GameObject hovered;
	public GameObject cursor;

	public bool uiHovering;
	public bool compassAttached;

	private MeshRenderer trigger;
	private MeshRenderer thumbstick;
	public Material highlightMaterial;
	private Material baseMaterial;
	private SteamVR_TrackedController controller;

	private bool gripDown;

	// Use this for initialization
	void Start()
	{
		controller = GetComponent<SteamVR_TrackedController>();
		controller.Gripped += (o, e) => gripDown = !gripDown;
	}

	// Update is called once per frame
	void Update()
	{
		//NOTE(Lander):Difference rotation and position for Vive and Touch controllers
		{
			if (VRDevices.loadedControllerSet == VRDevices.LoadedControllerSet.Vive)
			{
				laser.transform.localPosition = new Vector3(0, 0, 0.1f);
				laser.transform.localEulerAngles = new Vector3(90, 0, 0);
			}
		}

		//NOTE(Kristof): Fatty laser when pressing trigger
		{
			if (controller.triggerPressed)
			{
				laser.transform.localScale = new Vector3(2, laser.transform.localScale.y, 2);
			}
			else
			{
				laser.transform.localScale = new Vector3(1, laser.transform.localScale.y, 1);
			}
		}

		//NOTE(Kristof): Showing controllerUI when gripped 
		{
			var compass = Seekbar.compass.transform;

			var controllerUI = transform.Find("ControllerUI");
			//controllerUI.SetActive(GetComponent<SteamVR_TrackedController>().gripped && Player.playerState == PlayerState.Watching);

			if (compass && controllerUI)
			{
				if (gripDown && !compassAttached)
				{
					compass.parent = transform.Find("ControllerUI");
					compass.localScale = new Vector3(0.001f, 0.001f, 0.001f);
					compass.localPosition = Vector3.zero;
					compass.localEulerAngles = Vector3.zero;
					compass.gameObject.SetActive(true);
					compass.Find("CompassForeground").gameObject.SetActive(false);
					compassAttached = true;
					gripDown = false;
				}
				else if (gripDown && compassAttached)
				{
					
					//Seekbar.ReattachCompass();
					compass.gameObject.SetActive(false);
					compassAttached = false;
					gripDown = false;
				}
			}
		}
		var ray = CastRay();
		RaycastHit hit;

		//NOTE(Kristof): Checking for hovered UI elements and adjusting laser length
		{
			Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("UI", "WorldUI"));

			if (hit.transform != null)
			{
				uiHovering = true;
				hovered = hit.transform.gameObject;
				laser.transform.localScale = new Vector3(laser.transform.localScale.x, hit.distance, laser.transform.localScale.z);
			}
			else
			{
				uiHovering = false;
				hovered = null;
				laser.transform.localScale = new Vector3(laser.transform.localScale.x, 100f, laser.transform.localScale.z);
			}
		}

		//NOTE(Kristof): Moving cursor to hit location
		{
			cursor.transform.position = Vector3.zero;
			cursor.SetActive(false);

			Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("UI", "WorldUI", "interactionPoints"));
			if (hit.transform != null)
			{
				var r = 0.02f * hit.distance;
				if (r >= 0.04f)
				{
					cursor.SetActive(true);
					cursor.transform.position = hit.point;
					cursor.transform.localScale = new Vector3(r, r, r);
				}
			}
		}
	}

	public void TutorialHighlight()
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

		//TODO(Kristof): Thumbstick is only used for the Ocoulus Touch controllers, The Vive controllers use trackpad. Needs to be added
		if (thumbstick == null)
		{
			var thumbstickGo = model.transform.Find("thumbstick");
			if (thumbstickGo != null)
			{
				thumbstick = thumbstickGo.gameObject.GetComponent<MeshRenderer>();
			}
		}

		if (thumbstick != null)
		{
			baseMaterial = thumbstick.material;
			thumbstick.materials = new[] { baseMaterial, highlightMaterial };
		}
	}

	public void ResetMaterial()
	{
		if (trigger != null)
		{
			trigger.materials = new[] { baseMaterial };
		}

		if (thumbstick != null)
		{
			thumbstick.materials = new[] { baseMaterial };
		}
	}

	public Ray CastRay()
	{
		return new Ray(laser.transform.position, laser.transform.up);
	}
}
