using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour 
{
	public Text title;
	public RawImage image;
	public Canvas canvas;
	public GameObject interactionPoint;

	public void Init(GameObject interactionPoint, string title, string imageLocation)
	{
		this.title.text = title;

		var data = File.ReadAllBytes(imageLocation);
		var texture = new Texture2D(0, 0);
		texture.LoadImage(data);
		
		this.image.texture = texture;
		this.canvas = GetComponent<Canvas>();
		this.interactionPoint = interactionPoint;

		var newPos = interactionPoint.transform.position;

		if (!Camera.main.orthographic)
		{
			newPos = Vector3.Lerp(newPos, Camera.main.transform.position, 0.3f);
			newPos.y += 0.01f;
		}
		else
		{
			newPos = Vector3.Lerp(newPos, Camera.main.transform.position, 0.001f);
			newPos.y += 0.015f;
		}
		canvas.GetComponent<RectTransform>().position = newPos;

	}

	public void Update()
	{
		this.canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
