using UnityEngine;
using UnityEngine.UI;

class UIHelper
{
	public static float CalculateTextFieldHeight(InputField element, float minHeight)
	{
		var maxWidth = element.transform.Find("Text").GetComponent<RectTransform>().rect.width;

		return CalculateTextFieldHeight(element.text, element.textComponent.font, element.textComponent.fontSize, maxWidth, minHeight);
	}

	public static float CalculateTextFieldHeight(string text, Font font, int fontSize, float maxWidth, float minHeight)
	{
		var style = new GUIStyle
		{
			font = font,
			fontSize = fontSize
		};
		var words = text.Split(' ');

		var currentWidth = 0f;

		var lineHeight = style.lineHeight;
		var currentHeight = 2 * lineHeight;

		for (var i = 0; i < words.Length; i++)
		{
			var size = style.CalcSize(i != words.Length - 1
				? new GUIContent(words[i] + " ")
				: new GUIContent(words[i]));
			currentWidth += size.x;

			if (currentWidth >= maxWidth)
			{
				var lines = Mathf.Floor(currentWidth / maxWidth);
				currentWidth = size.x % maxWidth;
				currentHeight += lines * style.lineHeight;
			}
		}

		return Mathf.Max(currentHeight, minHeight);
	}
}
