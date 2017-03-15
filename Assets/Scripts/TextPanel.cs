using UnityEngine;
using UnityEngine.UI;

public class TextPanel : MonoBehaviour 
{
	public Text title;
	public Text body;

	void Init(string title, string body)
	{
		this.title.text = title;
		this.body.text = body;
	}
}
