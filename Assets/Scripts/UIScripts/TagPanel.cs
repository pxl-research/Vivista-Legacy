using System;
using UnityEngine;
using UnityEngine.UI;

public class TagPanel : MonoBehaviour
{
	public GameObject colorPickerPrefab;
	private GameObject colorPicker;
	public GameObject shapePickerPrefab;
	private GameObject shapePicker;
	public GameObject TagItemPrefab;

	public GameObject TagItemHolder;

	public InputField newName;
	public Image newColor;
	public Button newShape;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	void Start()
	{
		newName.onValueChanged.AddListener(OnEditName);
	}

	void Update()
	{

	}

	public void OnColorPicker(Image image)
	{
		if (colorPicker != null)
		{
			Destroy(colorPicker);
		}

		colorPicker = Instantiate(colorPickerPrefab);
		colorPicker.transform.SetParent(Canvass.main.transform);
		colorPicker.GetComponent<SimpleColorPicker>().Init(image);
	}

	public void OnShapePicker(Image image)
	{
		if (colorPicker != null)
		{
			Destroy(shapePicker);
		}

		shapePicker = Instantiate(shapePickerPrefab);
		shapePicker.transform.SetParent(Canvass.main.transform);
		shapePicker.GetComponent<ShapePicker>().Init(image);
	}

	public void OnAdd()
	{
		var name = newName.text;
		var color = newColor.color;
		var imagename = newShape.image.sprite.name;
		var imageIndex = Int32.Parse(imagename.Substring(imagename.LastIndexOf('_') + 1));

		var success = TagManager.Instance.AddTag(name, color, imageIndex);

		if (success)
		{
			newName.text = "";

			var tagGo = Instantiate(TagItemPrefab);
			var tagItem = tagGo.GetComponent<TagItem>();
			tagGo.transform.SetParent(TagItemHolder.transform);
			tagItem.Init(name, color, imageIndex);
			tagItem.deleteButton.onClick.AddListener(() => OnRemove(tagGo, name));
		}
		else
		{
			newName.image.color = errorColor;
		}
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
}
