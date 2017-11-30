using UnityEngine;

public enum Perspective
{
	Perspective360,
	Perspective180,
	PerspectiveFlat
}

public class PerspectivePanel : MonoBehaviour 
{
	public bool answered;
	public Perspective answerPerspective;

	public void Perspective360()
	{
		answered = true;
		answerPerspective = Perspective.Perspective360;
	}

	public void Perspective180()
	{
		answered = true;
		answerPerspective = Perspective.Perspective180;
	}

	public void PerspectiveFlat()
	{
		answered = true;
		answerPerspective = Perspective.PerspectiveFlat;
	}
}
