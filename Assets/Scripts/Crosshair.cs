using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
	private static Image crosshair;
	private static Image crosshairTimer;

	void Start()
	{
		crosshair = GameObject.Find("Crosshair").GetComponent<Image>();
		crosshairTimer = crosshair.transform.Find("CrosshairTimer").GetComponent<Image>();
	}

	void Update()
	{

	}

	public static void Disable()
	{
		crosshair.enabled = false;
		crosshairTimer.enabled = false;
	}

	public static void Enable()
	{
		crosshair.enabled = true;
		crosshairTimer.enabled = true;
	}

	public static void SetFillAmount(float percentage)
	{
		crosshairTimer.fillAmount = percentage;
		crosshair.fillAmount = 1 - percentage;
	}
}
