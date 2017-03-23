using System;
using UnityEngine;
using UnityEngine.UI;

public class TextPanelEditor : MonoBehaviour 
{
	public Canvas canvas;
	public RectTransform resizePanel;
	public InputField title;
	public InputField body;
	public Button done;

	public bool answered;
	public string answerTitle;
	public string answerBody;

	void Update () 
	{
		resizeElement(title, 30);
		resizeElement(body, 100);

		resizePanel.sizeDelta = new Vector2(resizePanel.sizeDelta.x,
			title.GetComponent<RectTransform>().sizeDelta.y
			+ body.GetComponent<RectTransform>().sizeDelta.y
			//Padding, spacing, button, fudge factor
			+ 20 + 20 + 30 + 20);
	}

	public void resizeElement(InputField element, int minHeight)
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
		
		rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, Mathf.Max(currentHeight, minHeight));
	}

	public void Answer()
	{
		answered = true;
		answerTitle = title.text;
		answerBody = title.text;
	}
}
