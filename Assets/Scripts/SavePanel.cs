using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SavePanel : MonoBehaviour 
{
	public Canvas canvas;
	public RectTransform resizePanel;
	public InputField filename;
	public Button done;
	public Text fileExistsWarning;

	public bool answered;
	public string answerFilename;

	private string prevName;
	private bool fileExists;
	
	void Update () 
	{
		/*resizePanel.sizeDelta = new Vector2(resizePanel.sizeDelta.x,
			filename.GetComponent<RectTransform>().sizeDelta.y
			//Padding, spacing,, title, button, fudge factor
			+ 20 + 30 + 30 + 30 + 30);
		*/
		canvas.transform.rotation = Camera.main.transform.rotation;

		if (filename.text != prevName && !string.IsNullOrEmpty(filename.text))
		{
			prevName = filename.text;
			answerFilename = filename.text + ".json";
			var files = new DirectoryInfo(Application.persistentDataPath).GetFiles("*.*");

			fileExists = false;
			foreach(var file in files)
			{
				if (file.Name == answerFilename)
				{
					fileExists = true;
					break;
				}
			}
		}

		fileExistsWarning.enabled = fileExists;
		done.interactable = !fileExists;
	}

	public void Answer()
	{
		if (!fileExists)
		{
			answered = true;
		}
	}
}
