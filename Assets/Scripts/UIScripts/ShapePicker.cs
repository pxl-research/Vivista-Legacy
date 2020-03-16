using UnityEngine;
using UnityEngine.UI;

public class ShapePicker : MonoBehaviour
{
	public RectTransform shapeHolder;
	private Image target;

	void Start()
	{
		var shapes = TagManager.Instance.GetAllShapes();

		for (int i = 0; i < shapes.Length; i++)
		{
			var go = new GameObject("color");
			go.transform.SetParent(shapeHolder, false);
			var image = go.AddComponent<Image>();
			image.sprite = shapes[i];
			var button = go.AddComponent<Button>();
			int index = i;
			button.onClick.AddListener(() => Answer(shapes[index]));
			button.transition = Selectable.Transition.None;
		}
	}

	public void Init(Image target)
	{
		var rectTransform = GetComponent<RectTransform>();
		rectTransform.position = target.rectTransform.position + new Vector3(0, 15);
		this.target = target;
	}

	void Answer(Sprite image)
	{
		target.sprite = image;
		Destroy(gameObject);
	}
}
