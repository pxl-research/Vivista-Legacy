using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR;

public class MouseLook : MonoBehaviour 
{
	public Vector3 mousePos;
	public float mouseRotX;
	public float mouseRotY;

	public float maxX = 360;
	public float minX = -360;
	public float maxY = 80;
	public float minY = -80;

	public float sensivity = 0.5f;
	public bool mouseClickRequired = true;
	public bool LookEnabled = true;

	public Quaternion originalRotation;

	void Start () 
	{
		mousePos = Input.mousePosition;
		originalRotation = transform.localRotation;
	}
	
	void Update () 
	{
		var mouseDelta = Input.mousePosition - mousePos;
		mousePos = Input.mousePosition;

		if (!VRSettings.enabled)
		{
			if (LookEnabled && (!mouseClickRequired || Input.GetMouseButton(0)) && !EventSystem.current.IsPointerOverGameObject())
			{
				mouseRotX = mouseRotX + (mouseDelta.x * sensivity);
				mouseRotY = mouseRotY + (mouseDelta.y * sensivity);
				mouseRotX = ClampAngle(mouseRotX, minX, maxX);
				mouseRotY = ClampAngle(mouseRotY, minY, maxY);

				var newRotx = Quaternion.AngleAxis(mouseRotX, Vector3.up);
				var newRoty = Quaternion.AngleAxis(mouseRotY, -Vector3.right);

				transform.localRotation = originalRotation * newRotx * newRoty;
			}

			if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				LookEnabled = !LookEnabled;
			}
		}
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
