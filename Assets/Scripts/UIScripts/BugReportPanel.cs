using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[RequireComponent(typeof(TabNav))]
public class BugReportPanel : MonoBehaviour
{
	public InputField problemInput;
	public InputField reproduction;
	public InputField email;
	public Text errorMessage;

	private class ReportResponse
	{
		public bool success;
	}

	private static Color defaultColor;
	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Start()
	{
		problemInput.onValueChanged.AddListener(delegate { OnInputChange(problemInput); });
		reproduction.onValueChanged.AddListener(delegate { OnInputChange(reproduction); });

		defaultColor = problemInput.image.color;
	}

	public void OnSubmit()
	{ 
		errorMessage.gameObject.SetActive(false);
		StartCoroutine(Submit());
	}

	public IEnumerator Submit()
	{
		var problem = problemInput.text;
		var repro = reproduction.text;
		var mail = email.text;
		bool errors = false;

		if (String.IsNullOrEmpty(problem))
		{
			this.problemInput.image.color = errorColor;
			errors = true;
		}
		if (String.IsNullOrEmpty(repro))
		{
			reproduction.image.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			var form = new WWWForm();

			form.AddField("problem", problem);
			form.AddField("repro", repro);
			form.AddField("email", mail);

			using (var request = UnityWebRequest.Post(Web.bugReportUrl, form))
			{
				yield return request.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					errorMessage.gameObject.SetActive(true);
					yield break;
				}

				var response = JsonUtility.FromJson<ReportResponse>(request.downloadHandler.text);

				if (response.success)
				{
					Canvass.modalBackground.SetActive(false);
					Destroy(gameObject);
				}
				else
				{
					errorMessage.gameObject.SetActive(true);
				}
			}
		}
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = defaultColor;
		errorMessage.gameObject.SetActive(false);
	}

	public void Close()
	{
		Destroy(gameObject);
		Canvass.modalBackground.SetActive(false);
	}
}
