using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class AboutPanel : MonoBehaviour
{
	public Text versionText;

	public void Start()
	{
		string versionFilePath = Path.Combine(Application.streamingAssetsPath, "version.txt");
		string version = File.ReadAllText(versionFilePath);

		versionText.text = version;
	}

	public void OnClose()
	{
		Canvass.modalBackground.SetActive(false);
		Destroy(gameObject);
	}
}
