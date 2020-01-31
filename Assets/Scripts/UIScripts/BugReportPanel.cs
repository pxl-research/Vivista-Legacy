using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BugReportPanel : MonoBehaviour
{
	public InputField description;
	public InputField reproduction;
	public InputField email;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Start()
	{
		description.onValueChanged.AddListener(OnInputChange);
		reproduction.onValueChanged.AddListener(OnInputChange);
	}

	public void OnSubmit()
	{
		var desc = description.text;
		var repro = reproduction.text;
		var mail = email.text;
		bool errors = false;

		if (String.IsNullOrEmpty(desc))
		{
			description.image.color = errorColor;
			errors = true;
		}
		if (String.IsNullOrEmpty(repro))
		{
			reproduction.image.color = errorColor;
			errors = true;
		}

		if (errors)
		{
			return;
		}
		
		var success = SendReport();

		if (success)
		{
			Destroy(gameObject);
		}
	}

	public void OnInputChange(string arg0)
	{
		var input = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
		input.image.color = Color.white;
	}

	public bool SendReport()
	{
		return true;
	}
}
