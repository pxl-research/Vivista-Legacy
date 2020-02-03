using System;
using UnityEngine;
using UnityEngine.UI;

public class BugReportPanel : MonoBehaviour
{
	public InputField description;
	public InputField reproduction;
	public InputField email;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Start()
	{
		description.onValueChanged.AddListener(delegate { OnInputChange(description); });
		reproduction.onValueChanged.AddListener(delegate { OnInputChange(reproduction); });
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

		if (!errors)
		{
			var success = SendReport(desc, repro, mail);

			if (success)
			{
				Destroy(gameObject);
			}
		}
	}

	private bool SendReport(string desc, string repro, string mail)
	{
		return true;
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
