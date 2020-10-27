using Dummiesman;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanel : MonoBehaviour
{
	public Text title;

	private GameObject objectRenderer;
	private GameObject object3d;

	public void Init(string newTitle, string fullPath)
	{
		title.text = newTitle;
		objectRenderer = GameObject.Find("ObjectRenderer");

		object3d = new OBJLoader().Load(fullPath);
		object3d.transform.parent = objectRenderer.transform;
		object3d.transform.localScale = new Vector3(50, 50, 50);
		object3d.transform.localRotation = new Quaternion(0, 0, 0, 0);
		object3d.transform.localPosition = new Vector3(0, 0, -50);
		object3d.SetLayer(12);
	}
}
