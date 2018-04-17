using UnityEngine;

public class Controller : MonoBehaviour
{
	public GameObject laser;
	public GameObject model;
	public GameObject controllerUI;

	public Material highlightMaterial;

	private MeshRenderer trigger;
	private Material baseMaterial;

	private SteamVR_TrackedController controller;

	public bool uiHovering;
	public GameObject hovered;

	// Use this for initialization
	void Start()
	{
		controller = GetComponent<SteamVR_TrackedController>();
	}

	// Update is called once per frame
	void Update()
	{
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
			controllerUI.SetActive(GetComponent<SteamVR_TrackedController>().gripped && Player.playerState == PlayerState.Watching);
		}
		var ray = CastRay();
		RaycastHit hit;

		//NOTE(Kristof): Shortening the laser when it hits the UI
		{
			Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("UI") + LayerMask.GetMask("WorldUI"));

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
		}

		//NOTE(Kristof): Checking for hovered UI elements
		{
			Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("UI") + LayerMask.GetMask("WorldUI"));

			if (hit.transform != null)
			{
				uiHovering = true;
				hovered = hit.transform.gameObject;
			}
			else
			{
				uiHovering = false;
				hovered = null;
			}
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
}
