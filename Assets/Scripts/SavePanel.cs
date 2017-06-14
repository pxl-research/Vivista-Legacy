using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class SavePanel : MonoBehaviour 
{
	public Canvas canvas;
	public RectTransform resizePanel;
	public InputField filenameInput;
	public InputField titleInput;
	public InputField descriptionInput;
	public Button done;
	public Text fileExistsWarning;

	public bool answered;
	public string answerFilename;
	public string answerTitle;
	public string answerDescription;

	private string prevName;
	private bool fileExists;
	
	public void Init(string filename, string title, string description)
	{
		if (filename != null)
		{
			filenameInput.text = filename.Substring(0, filename.LastIndexOf('.'));
		}
		if (title != null)
		{
			titleInput.text = title;
		}
		if (description != null)
		{
			descriptionInput.text = description;
		}
	}

	void Update () 
	{
		if (filenameInput.text != prevName && !string.IsNullOrEmpty(filenameInput.text))
		{
			prevName = filenameInput.text;
			answerFilename = filenameInput.text + ".json";
			var files = new DirectoryInfo(Application.persistentDataPath).GetFiles("*.*");

			foreach(var file in files)
			{
				if (String.Compare(file.Name, answerFilename, true) == 0)
				{
					fileExists = true;
					break;
				}

				fileExists = false;
			}
		}

		fileExistsWarning.enabled = fileExists;
		done.GetComponentInChildren<Text>().text = fileExists ? "Overwrite?" : "Save";
	}

	public void Answer()
	{
		answerTitle = filenameInput.text;
		answerDescription = descriptionInput.text;
		answered = true;
	}
}
