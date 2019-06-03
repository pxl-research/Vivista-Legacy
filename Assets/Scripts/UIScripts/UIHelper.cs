using UnityEngine;
using UnityEngine.UI;

class UIHelper
{
	public static float CalculateInputFieldHeight(InputField element, float minLines)
	{
		var elementRect = element.GetComponent<RectTransform>();

		var outerHeight = elementRect.rect.height;
		var innerHeight = element.textComponent.GetComponent<RectTransform>().rect.height;

		return Mathf.Max(LayoutUtility.GetPreferredHeight(elementRect) + (outerHeight - innerHeight), minLines * element.textComponent.font.lineHeight);
	}

	public static float CalculateTextFieldHeight(string text, Font font, int fontSize, float maxWidth, float minHeight)
	{
		var style = new GUIStyle
		{
			font = font,
			fontSize = fontSize
		};
		var paragraphs = text.Split('\n');

		var currentWidth = 0f;

		var lineHeight = style.fontSize;
		var currentHeight = 2 * lineHeight;

		for (var i = 0; i < paragraphs.Length; i++)
		{
			var words = paragraphs[i].Split(' ');

			for (int j = 0; j < words.Length; j++)
			{
				var size = style.CalcSize(j != words.Length - 1
					? new GUIContent(words[j] + " ")
					: new GUIContent(words[j]));
				currentWidth += size.x;

				if (currentWidth >= maxWidth)
				{
					var lines = Mathf.Floor(currentWidth / maxWidth);
					currentWidth = size.x % maxWidth;
					currentHeight += (int)(lines * style.lineHeight);// * 0.9f);
				}
			}

			currentHeight += (int)(style.lineHeight);// * 0.9f);
		}

		return Mathf.Max(currentHeight, minHeight);
	}
}
