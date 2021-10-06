using UnityEngine;
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
		this.message.text = message;
		confirmButton.GetComponentInChildren<Text>().text = confirmMessage;
		denyButton.GetComponentInChildren<Text>().text = denyMessage;

		confirmButton.onClick.AddListener(OnConfirm);
		denyButton.onClick.AddListener(OnDeny);
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
