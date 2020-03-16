using UnityEngine;
using UnityEngine.UI;

public class TagItem : MonoBehaviour
{
	public Text name;
	public Image background;
	public Image shape;
	public Button deleteButton;

	public void Init(string name, Color color, int shapeIndex)
	{
		this.name.text = name;
		background.color = color;
		shape.sprite = TagManager.Instance.ShapeForIndex(shapeIndex);
	}
}
