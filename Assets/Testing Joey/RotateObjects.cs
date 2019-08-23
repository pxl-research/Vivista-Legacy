using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class RotateObjects : MonoBehaviour {

	private float rotationSpeed = 1.0f;

	void OnMouseDrag()
	{
		float XaxisRotation = Input.GetAxis("Mouse X") * rotationSpeed;
		float YaxisRotation = Input.GetAxis("Mouse Y") * rotationSpeed;
		transform.Rotate(Vector3.down, XaxisRotation);
		transform.Rotate(Vector3.right, YaxisRotation);
	}
}
