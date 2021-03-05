using UnityEngine;
using UnityEngine.UI;

public class TimelineTooltip : MonoBehaviour
{
	public Text text;

	public void SetText(string newText, Vector2 pos)
	{
		if (newText != text.text)
		{
			text.text = newText;
		}

		pos.y += text.rectTransform.rect.height;
		transform.position = pos;
	}

	public void ResetPosition()
	{
		transform.position = new Vector3(-1000, -1000);
	}
}
