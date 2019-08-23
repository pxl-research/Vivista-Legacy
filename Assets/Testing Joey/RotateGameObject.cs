using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RotateGameObject : MonoBehaviour {

	public GameObject objectToRotate;
	public Slider slider;

	private float previousValue;

	private void Awake()
	{
		this.slider.onValueChanged.AddListener(this.OnsliderChanged);

		this.previousValue = this.slider.value;
	}

	void OnsliderChanged(float value)
	{
		float delta = value - this.previousValue;
		this.objectToRotate.transform.Rotate(Vector3.right * delta * 360);

		this.previousValue = value;
	}
}
