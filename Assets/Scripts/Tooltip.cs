using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public string tooltipContent;
	public RectTransform textHolder;
	public Text text;

	private bool openedBefore = false;

	void Start()
	{
		Assert.IsFalse(String.IsNullOrEmpty(tooltipContent));
		text.text = tooltipContent;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		textHolder.gameObject.SetActive(true);
		if (!openedBefore)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(textHolder);
			openedBefore = true;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		textHolder.gameObject.SetActive(false);
	}
}
