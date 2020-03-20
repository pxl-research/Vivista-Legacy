using UnityEngine;
using UnityEngine.UI;

public class TagItem : MonoBehaviour
{
	public Text label;
	public Image background;
	public Image shape;
	public Button deleteButton;

	public void Init(string name, Color color, int shapeIndex)
	{
		label.text = name;
		label.color = color.IdealTextColor();
		background.color = color;
		shape.sprite = TagManager.Instance.ShapeForIndex(shapeIndex);
	}
}
