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

	public void init()
	{
		var files = new DirectoryInfo(Application.persistentDataPath).GetFiles("*.*");
		foreach (var file in files)
		{
			filenames.Add(StripExtension(file.Name));
		}

		foreach (var filename in filenames)
		{
			var filenameItem = Instantiate(filenameItemPrefab);
			filenameItem.transform.SetParent(fileList, false);
			filenameItem.GetComponentInChildren<Text>().text = filename;
			filenameItems.Add(filenameItem);
		}
	}

	void Update () 
	{
		foreach (var item in filenameItems)
		{
			var coords = new Vector3[4];
			item.GetComponent<RectTransform>().GetWorldCorners(coords);

			if (Input.mousePosition.x > coords[0].x && Input.mousePosition.x < coords[2].x
				&& Input.mousePosition.y > coords[0].y && Input.mousePosition.y < coords[2].y)
			{
				item.GetComponentInChildren<Text>().color = Color.red;
			}
			else
			{
				item.GetComponentInChildren<Text>().color = Color.black;
			}

			if (Input.mousePosition.x > coords[0].x && Input.mousePosition.x < coords[2].x
				&& Input.mousePosition.y > coords[0].y && Input.mousePosition.y < coords[2].y
				&& Input.GetMouseButtonDown(0))
			{
				var filename = item.GetComponentInChildren<Text>().text;
				chosenFile.text = "Chosen file: " + filename;
				answerFilename = filename + ".json";
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
