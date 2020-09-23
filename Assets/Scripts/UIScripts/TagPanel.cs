using System;
using UnityEngine;
using UnityEngine.UI;

public class TagPanel : MonoBehaviour
{
	public GameObject colorPickerPrefab;
	private GameObject colorPicker;
	public GameObject shapePickerPrefab;
	private GameObject shapePicker;
	public GameObject tagItemPrefab;

	public GameObject tagItemHolder;

	public InputField newName;
	public Image newColor;
	public Button newShape;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	void Start()
	{
		newName.onValueChanged.AddListener(OnEditName);

		var tags = TagManager.Instance.tags;

		for (int i = 0; i < tags.Count; i++)
		{
			AddTagItem(tags[i].name, tags[i].color, tags[i].shapeIndex);
		}
	}

	public void OnColorPicker(Image image)
	{
		DestroyActivePickers();
		
		colorPicker = Instantiate(colorPickerPrefab);
		colorPicker.transform.SetParent(Canvass.main.transform);
		colorPicker.GetComponent<SimpleColorPicker>().Init(image);
	}

	public void OnShapePicker(Image image)
	{
		DestroyActivePickers();

		shapePicker = Instantiate(shapePickerPrefab);
		shapePicker.transform.SetParent(Canvass.main.transform);
		shapePicker.GetComponent<ShapePicker>().Init(image);
	}

	public void OnAdd()
	{
		DestroyActivePickers();

		var name = newName.text;
		var color = newColor.color;
		var shapeName = newShape.image.sprite.name;
		var shapeIndex = Int32.Parse(shapeName.Substring(shapeName.LastIndexOf('_') + 1));

		var success = TagManager.Instance.AddTag(name, color, shapeIndex);

		if (success)
		{
			newName.text = "";

			AddTagItem(name, color, shapeIndex);
		}
		else
		{
			newName.image.color = errorColor;
		}
	}

	public void AddTagItem(string name, Color color, int shapeIndex)
	{
		var tagGo = Instantiate(tagItemPrefab);
		var tagItem = tagGo.GetComponent<TagItem>();
		tagGo.transform.SetParent(tagItemHolder.transform);
		tagItem.Init(name, color, shapeIndex);
		tagItem.deleteButton.onClick.AddListener(() => OnRemove(tagGo, name));
	}

	public void OnRemove(GameObject tagGo, string name)
	{
		TagManager.Instance.RemoveTag(name);
		Destroy(tagGo);
	}

	public void OnEditName(string _)
	{
		newName.image.color = Color.white;
	}

	public void Close()
	{
		Destroy(gameObject);
	}

	private void DestroyActivePickers()
	{
		if (colorPicker != null)
		{
			Destroy(colorPicker);
		}
		if (shapePicker != null)
		{
			Destroy(shapePicker);
		}

	}
}
