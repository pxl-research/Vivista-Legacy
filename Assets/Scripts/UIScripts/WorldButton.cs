using UnityEngine;
using UnityEngine.UI;

public class WorldButton : MonoBehaviour
{
	public void OnHoverStart()
	{
		GetComponent<Image>().color = new Color(0, 255, 217, 80);
	}

	public void OnHoverEnd()
	{
		GetComponent<Image>().color = new Color(255, 255, 217, 80);
	}
}
