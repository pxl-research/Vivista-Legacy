using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class OpenPanel : MonoBehaviour 
{
	public RectTransform fileList;
	public Text chosenFile;

	public GameObject filenameItemPrefab;

	public bool answered;
	public string answerFilename;

	private List<string> filenames = new List<string>();
	private List<GameObject> filenameItems = new List<GameObject>();
	private int selectedIndex = -1;

	public void Init()
	{
		var files = new DirectoryInfo(Application.persistentDataPath).GetFiles("*.*");
		foreach (var file in files)
		{
			filenames.Add(StripExtension(file.Name));
		}

		foreach (var filename in filenames)
		{
			var filenameListItem = Instantiate(filenameItemPrefab);
			filenameListItem.transform.SetParent(fileList, false);
			filenameListItem.GetComponentInChildren<Text>().text = filename;
			filenameItems.Add(filenameListItem);
		}
	}

	void Update ()
	{
		for (var i = 0; i < filenameItems.Count; i++)
		{
			var item = filenameItems[i];

			if (RectTransformUtility.RectangleContainsScreenPoint(item.GetComponent<RectTransform>(), Input.mousePosition))
			{
				item.GetComponentInChildren<Text>().color = Color.red;
			}
			else if (i == selectedIndex)
			{
				item.GetComponentInChildren<Text>().color = Color.red;
			}
			else
			{
				item.GetComponentInChildren<Text>().color = Color.black;
			}

			if (RectTransformUtility.RectangleContainsScreenPoint(item.GetComponent<RectTransform>(), Input.mousePosition)
				&& Input.GetMouseButtonDown(0))
			{
				var filename = item.GetComponentInChildren<Text>().text;
				chosenFile.text = "Chosen file: " + filename;
				answerFilename = filename + ".json";
				selectedIndex = i;
			}
		}
	}

	public void Answer()
	{
		answered = true;
	}

	public static string StripExtension(string filename)
	{
		return filename.LastIndexOf(".") > 0 ? filename.Substring(0, filename.LastIndexOf(".")) : filename;
	}
}
