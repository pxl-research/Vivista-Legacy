using System;
using UnityEngine;
using UnityEngine.UI;

public class TextPanelEditor : MonoBehaviour 
{
	public InputField title;
	public InputField body;
	public Button done;

	public bool answered;
	public string answerTitle;
	public string answerBody;

	void Update () 
	{
		var bodyRect = body.GetComponent<RectTransform>();
		var titleRect = body.GetComponent<RectTransform>();
		resizeElement(title);
		//resizeElement(body);
	}

	public void resizeElement(InputField element)
	{
		var style = new GUIStyle
		{
			font = element.textComponent.font,
			fontSize = element.textComponent.fontSize
		};

		var rectTransform = element.GetComponent<RectTransform>();
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
		
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, Mathf.Max(currentHeight, 30));
	}

	public void Answer()
	{
		answered = true;
		answerTitle = title.text;
		answerBody = title.text;
	}
}
