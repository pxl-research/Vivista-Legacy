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
			laser.transform.localScale = new Vector3(2, 1, 2);
		}
		else
		{
			laser.transform.localScale = new Vector3(1, 1, 1);
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
			trigger.materials = new[] { baseMaterial, highlightMaterial};
		}
		
	}

	public void ResetTriggerMaterial()
	{
		trigger.materials = new[] {baseMaterial};
	}
}
