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

		var ray = new Ray(controller.transform.position, controller.transform.forward);
		RaycastHit hit;
		Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("Seekbar"));

		if (hit.transform != null)
		{
			laser.transform.localPosition = new Vector3(0, 0, 1.07f);
			laser.transform.localScale = new Vector3(laser.transform.localScale.x, hit.distance, laser.transform.localScale.z);
		}
		else
		{
			laser.transform.localPosition = new Vector3(0, 0, 50.175f);
			laser.transform.localScale = new Vector3(laser.transform.localScale.x, 100f, laser.transform.localScale.z);
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
}
