using UnityEngine;

public class Controller : MonoBehaviour
{
	public GameObject laser;
	public GameObject model;
	public Material highlightMaterial;

	private MeshRenderer trigger;
	private Material baseMaterial;

	private SteamVR_TrackedController controller;

	// Use this for initialization
	void Start()
	{
		controller = GetComponent<SteamVR_TrackedController>();
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

		var ray = castRay();
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

	public Ray castRay()
	{
		return new Ray(laser.transform.position, laser.transform.up);
	}
}
