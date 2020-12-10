using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TagPicker : MonoBehaviour
{
	public Sprite tagBackground;

	public RectTransform tagSuggestionWrapper;
	public Button pickTagButton;
	public InputField tagFilter;

	public Text tagItemText;
	public Image tagItemShape;
	public Image tagItemColor;

	private Tag currentTag;
	public int currentTagId => currentTag?.id ?? -1;

	public void Init(int tagId)
	{
		currentTag = TagManager.Instance.GetTagById(tagId);
		UpdateTag();
	}

	void Start()
	{
		pickTagButton.onClick.AddListener(OnPickTagStart);
		tagFilter.onValueChanged.AddListener(OnTagFilterUpdate);
	}

	void OnPickTagStart()
	{
		OnTagFilterUpdate("");
		pickTagButton.gameObject.SetActive(false);
		tagFilter.gameObject.SetActive(true);
		EventSystem.current.SetSelectedGameObject(tagFilter.gameObject);
		tagSuggestionWrapper.gameObject.SetActive(true);
	}

	void OnTagFilterUpdate(string text)
	{
		foreach (Transform suggestion in tagSuggestionWrapper)
		{
			Destroy(suggestion.gameObject);
		}

		var tags = new List<Tag> {Tag.Default};
		tags.AddRange(TagManager.Instance.Filter(text));

		for (int i = 0; i < tags.Count; i++)
		{
			var itemWrapper = new GameObject("TagItem");

			var itemWrapperTransform = itemWrapper.AddComponent<RectTransform>();
			itemWrapperTransform.SetParent(tagSuggestionWrapper);
			itemWrapperTransform.sizeDelta = new Vector2(itemWrapperTransform.sizeDelta.x, 38);

			var image = itemWrapper.AddComponent<Image>();
			image.sprite = tagBackground;
			image.color = tags[i].color;
			image.type = Image.Type.Sliced;

			var button = itemWrapper.AddComponent<Button>();
			var index = i;
			button.onClick.AddListener(() => OnPickTagEnd(tags[index]));

			var label = new GameObject("Label");

			var labelTransform = label.AddComponent<RectTransform>();
			labelTransform.SetParent(itemWrapperTransform);
			labelTransform.anchorMin = new Vector2(0, 0);
			labelTransform.anchorMax = new Vector2(0.9f, 1);
			labelTransform.offsetMin = new Vector2(5, 0);
			labelTransform.offsetMax = new Vector2(-5, 0);

			var labelText = label.AddComponent<Text>();
			labelText.fontSize = 14;
			labelText.text = tags[i].name;
			labelText.color = tags[i].color.IdealTextColor();
			labelText.alignment = TextAnchor.MiddleLeft;
			labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

			var shape = new GameObject("Shape");

			var shapeTransform = shape.AddComponent<RectTransform>();
			shapeTransform.SetParent(itemWrapperTransform);
			shapeTransform.anchorMin = new Vector2(0.9f, 0);
			shapeTransform.anchorMax = new Vector2(1, 1);
			shapeTransform.sizeDelta = new Vector2(10, 0);
			shapeTransform.offsetMin = new Vector2(0, 5);
			shapeTransform.offsetMax = new Vector2(-5, -5);

			var shapeImage = shape.AddComponent<Image>();
			shapeImage.sprite = TagManager.Instance.ShapeForIndex(tags[i].shapeIndex);
			if (tags[i].id == -1)
			{
				shapeImage.sprite = null;
			}
		}
	}

	void OnPickTagEnd(Tag tag)
	{
		currentTag = tag;

		pickTagButton.gameObject.SetActive(true);
		tagFilter.gameObject.SetActive(false);
		tagSuggestionWrapper.gameObject.SetActive(false);

		UpdateTag();
	}

	void UpdateTag()
	{
		tagItemText.text = currentTag.name;
		tagItemText.color = currentTag.color.IdealTextColor();
		tagItemColor.color = currentTag.color;
		tagItemShape.sprite = TagManager.Instance.ShapeForIndex(currentTag.shapeIndex);

		if (currentTag.id == -1)
		{
			tagItemShape.sprite = null;
		}
	}
}
