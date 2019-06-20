using UnityEngine;
using UnityEngine.UI;

public class TextPanel : MonoBehaviour
{
	public Text title;
	public Text body;

	public void Init(string newTitle, string newBody)
	{
		title.text = newTitle;
		body.text = newBody;
	}

	public void Move(Vector3 position)
	{
	} 
}