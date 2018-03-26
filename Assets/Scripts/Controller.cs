using System;
using UnityEngine;

public class Controller : MonoBehaviour
{

	public GameObject laser;

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
		try
		{
			trigger = transform.Find("Model").Find("trigger").gameObject.GetComponent<MeshRenderer>();
			baseMaterial = trigger.material;

			Material[] materialsArray = { baseMaterial, Resources.Load("ControllerHighlight") as Material };
			trigger.materials = materialsArray;
		}
		catch(NullReferenceException e)
		{
			Debug.LogWarning("Controller is not initialised!");
		}
	}

	public void ResetTriggerMaterial()
	{
		trigger.materials = new Material[] { baseMaterial };
	}
}
