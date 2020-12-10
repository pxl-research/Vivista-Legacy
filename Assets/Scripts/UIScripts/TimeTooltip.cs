using UnityEngine;
using UnityEngine.UI;

public class TimeTooltip : MonoBehaviour
{
	public Text text;
	public float time1;
	public float time2;

	public void SetTime(float newTime, Vector2 pos)
	{
		if (time1 != newTime)
		{
			text.text = MathHelper.FormatMillis(newTime);
			time1 = newTime;
		}

		pos.y += text.rectTransform.rect.height;
		transform.position = pos;
	}

	public void SetTime(float newTime1, float newTime2, Vector2 pos)
	{
		if (time1 != newTime1 || time2 != newTime2)
		{
			var newText = MathHelper.FormatMillis(newTime1);
			newText += " - ";
			newText += MathHelper.FormatMillis(newTime2);
			text.text = newText;
			time1 = newTime1;
			time2 = newTime2;
		}

		pos.y += text.rectTransform.rect.height;
		transform.position = pos;
	}

	public void ResetPosition()
	{
		transform.position = new Vector3(-1000, -1000);
	}
}
