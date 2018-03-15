using UnityEngine;

public class LaserBehaviour : MonoBehaviour
{

	public GameObject laser;

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
}
