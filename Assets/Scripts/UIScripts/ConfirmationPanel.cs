using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class ConfirmationPanel : MonoBehaviour
{
	public Button confirmButton;
	public Button denyButton;
	public Text message;

	public bool answered;
	public bool answerValue;

	public void Init(string message, string confirmMessage, string denyMessage)
	{
		Assert.IsTrue(!String.IsNullOrEmpty(confirmMessage));

		this.message.text = message;
		confirmButton.GetComponentInChildren<Text>().text = confirmMessage;
		confirmButton.onClick.AddListener(OnConfirm);

		if (!String.IsNullOrEmpty(denyMessage))
		{
			denyButton.GetComponentInChildren<Text>().text = denyMessage;
			denyButton.onClick.AddListener(OnDeny);
		}
		else
		{
			denyButton.gameObject.SetActive(false);
			var confirmRect = confirmButton.GetComponent<RectTransform>();
			var denyRect = denyButton.GetComponent<RectTransform>();

			confirmRect.anchorMin = denyRect.anchorMin;
			confirmRect.anchorMax = denyRect.anchorMax;
			confirmRect.localPosition = denyRect.localPosition;
		}
	}

	public void OnConfirm()
	{
		answered = true;
		answerValue = true;
	}

	public void OnDeny()
	{
		answered = true;
		answerValue = false;
	}
}
