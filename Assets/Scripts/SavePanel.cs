using System;
using UnityEngine;
using UnityEngine.UI;

public class SavePanel : MonoBehaviour 
{
	public InputField titleInput;
	public InputField descriptionInput;
	public Button done;
	public Text fileExistsWarning;

	public bool answered;
	public string answerTitle;
	public string answerDescription;

	public bool fileExists;
	
	public void Init(string title, string description)
	{
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
		var titles = SaveFile.GetAllSavefileNames();

		foreach(var title in titles)
		{
			if (String.Compare(title, titleInput.text, true) == 0)
			{
				fileExists = true;
				break;
			}

			fileExists = false;
		}

		fileExistsWarning.enabled = fileExists;
		done.GetComponentInChildren<Text>().text = fileExists ? "Overwrite?" : "Save";
	}

	public void Answer()
	{
		answerTitle = titleInput.text;
		answerDescription = descriptionInput.text;
		answered = true;
	}
}
