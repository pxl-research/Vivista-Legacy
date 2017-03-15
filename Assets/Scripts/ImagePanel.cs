using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour 
{
	public Text title;
	public RawImage image;

	public void Init(string title, string imageLocation)
	{
		this.title.text = title;

		var data = File.ReadAllBytes(imageLocation);
		var texture = new Texture2D(0, 0);
		texture.LoadImage(data);
		
		this.image.texture = texture;
	}
}
