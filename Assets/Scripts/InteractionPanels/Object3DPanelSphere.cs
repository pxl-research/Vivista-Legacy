using UnityEngine;
using UnityEngine.UI;

public class Object3DPanelSphere : MonoBehaviour
{
    public Text title;
	public void Init(string newTitle, string newUrl)
	{
		title.text = newTitle;
	}
}
