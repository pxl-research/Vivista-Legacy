using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour 
{
	public Vector3 mousePos;
	public float mouseRotX;
	public float mouseRotY;

	public float MaxX = 360;
	public float MinX = -360;
	public float MaxY = 80;
	public float MinY = -80;

	public float sensivity = 1;

	public Quaternion originalRotation;
	// Use this for initialization
	void Start () 
	{
		mousePos = Input.mousePosition;
		originalRotation = transform.localRotation;
	}
	
	// Update is called once per frame
	void Update () 
	{
		var mouseDelta = Input.mousePosition - mousePos;
		mousePos = Input.mousePosition;

		mouseRotX = mouseRotX + (mouseDelta.x * sensivity);
		mouseRotY = mouseRotY + (mouseDelta.y * sensivity);
		mouseRotX = ClampAngle(mouseRotX, MinX, MaxX);
		mouseRotY = ClampAngle(mouseRotY, MinY, MaxY);

		var newRotx = Quaternion.AngleAxis(mouseRotX, Vector3.up);
		var newRoty = Quaternion.AngleAxis(mouseRotY, -Vector3.right);

		transform.localRotation = originalRotation * newRotx * newRoty;
	}

public static float ClampAngle (float angle, float min, float max)
{
	if (angle < -360F)
	{
		angle += 360F;
	}
	if (angle > 360F)
	{
		angle -= 360F;
	}
	 return Mathf.Clamp (angle, min, max);
}
}
