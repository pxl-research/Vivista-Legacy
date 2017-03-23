using UnityEngine;
using UnityEngine.UI;

class UIHelper
{
	public static float CalculateTextFieldHeight(InputField element, float minHeight)
	{
		var style = new GUIStyle
		{
			font = element.textComponent.font,
			fontSize = element.textComponent.fontSize
		};

		var words = element.text.Split(' ');
		var currentWidth = 0f;
		var maxWidth = element.transform.FindChild("Text").GetComponent<RectTransform>().rect.width;
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
