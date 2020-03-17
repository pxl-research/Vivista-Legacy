using UnityEngine;
using UnityEngine.UI;

public class SimpleColorPicker : MonoBehaviour
{
	public RectTransform colorHolder;
	[ColorUsage(false)]
	public Color[] colors;

	private Image target;

	void Start()
	{
		for (int i = 0; i < colors.Length; i++)
		{
			var go = new GameObject("color");
			go.transform.SetParent(colorHolder, false);
			var image = go.AddComponent<Image>();
			colors[i].a = 1;
			image.color = colors[i];
			var button = go.AddComponent<Button>();
			int index = i;
			button.onClick.AddListener(() => Answer(colors[index]));
			button.transition = Selectable.Transition.None;
		}
	}

	public void Init(Image target)
	{
		var rectTransform = GetComponent<RectTransform>();
		rectTransform.position = target.rectTransform.position + new Vector3(0, 15);
		this.target = target;
	}

	void Answer(Color color)
	{
		target.color = color;
		Destroy(gameObject);
	}
}
