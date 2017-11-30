using UnityEngine;

public class NewPanel : MonoBehaviour 
{
	public bool answered;
	public bool answerNew;
	public bool answerOpen;

	public void New()
	{
		answered = true;
		answerNew = true;
	}

	public void Open()
	{
		answered = true;
		answerOpen = true;
	}
}
