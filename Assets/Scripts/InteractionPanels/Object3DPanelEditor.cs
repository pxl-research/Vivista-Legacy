using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanelEditor : MonoBehaviour
{
	public Button done;

	public InputField objectUrl;
	public Text dependencies;
	public InputField title;

	public bool answered;
	public string answerTitle;
	public string answerObjUrl;
	public string answerMatUrl;
	public string answerTexturesUrl;

	public bool allowCancel => explorerPanel == null;

	private ExplorerPanel explorerPanel;
	private GameObject objectRenderer;

	private bool fileOpening;

	private List<string> files;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
	}

	void Update()
	{
		if (fileOpening)
		{
			if (explorerPanel != null)
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					Destroy(explorerPanel.gameObject);
				}

				if (explorerPanel.answered)
				{
					objectUrl.text = explorerPanel.answerPath;

					ResolveDependencies(objectUrl.text);

					Destroy(explorerPanel.gameObject);
				}
			}

			
		}
	}

	public void Init(string initialTitle, string initialUrl, string object3dName)
	{
		title.text = initialTitle;
		objectRenderer = GameObject.Find("ObjectRenderer");

		var objects3d = objectRenderer.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < objects3d.Length; i++)
		{
			var tempObject = objects3d[i];
			Debug.Log(tempObject.ToString());
			if (tempObject.name == object3dName)
			{
				Destroy(tempObject.gameObject);
				break;
			}
		}

		if (initialUrl != null)
		{
			objectUrl.text = initialUrl;
		}

		title.onValueChanged.AddListener(_ => OnInputChange(title));
		objectUrl.onValueChanged.AddListener(_ => OnInputChange(objectUrl));
	}

	public void Answer()
	{
		bool errors = false;
		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (String.IsNullOrEmpty(objectUrl.text))
		{
			objectUrl.image.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerObjUrl = objectUrl.text;
			answerMatUrl = "";
			answerTexturesUrl = "";
			answerTitle = title.text;
		}
	}

	public void Browse()
	{
		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", "*.obj", "Select 3D object");

		fileOpening = true;
	}

	private void ResolveDependencies(string url)
	{
		string objLine;
		string matUrl = "";
		files = new List<string>();

		//NOTE(Jitse): Try to find a .mtl file reference.  
		//NOTE(cont.): This might loop through the entire .obj in some rare cases, if neither mtllib or usemtl is specified.
		StreamReader objFile = new StreamReader(url);
		while ((objLine = objFile.ReadLine()) != null)
		{
			//NOTE(Jitse): Skip commented lines.
			if (objLine.StartsWith("#"))
			{
				continue;
			}

			//TODO(Jitse): Apparently (according to .obj wiki), more than one .mtl file may be referenced within the .obj file.
			if (objLine.StartsWith("mtllib"))
			{
				matUrl = objLine.Split(' ')[1];
				break;
			}
			if (objLine.StartsWith("usemtl"))
			{
				break;
			}
		}

		objFile.Close();

		if (matUrl != "")
		{
			string matUrlPath = Path.Combine(Path.GetDirectoryName(url), matUrl);
			if (File.Exists(matUrlPath))
			{
				files.Add(matUrl);

				string mtlLine;

				//NOTE(Jitse): Find all texture references.  
				StreamReader mtlFile = new StreamReader(matUrlPath);
				while ((mtlLine = mtlFile.ReadLine()) != null)
				{
					//NOTE(Jitse): Skip commented lines.
					if (mtlLine.StartsWith("#"))
					{
						continue;
					}

					//NOTE(Jitse): Check if the line contains an extension
					var extension = "";
					mtlLine = mtlLine.Trim('\t');
					extension = Path.GetExtension(mtlLine);
					
					if (extension.Length > 1 && !extension.Any(Char.IsDigit))
					{
						string textureFile = mtlLine.Substring(mtlLine.LastIndexOf(' ') + 1);
						textureFile = textureFile.Replace("\\\\", "\\");
						if (!File.Exists(Path.Combine(Path.GetDirectoryName(url), textureFile)))
						{
							textureFile = "# File not found: " + textureFile;
						}
						if (!files.Contains(textureFile))
						{
							
							files.Add(textureFile);
						}
					}
				}

				mtlFile.Close();
			} 
			else
			{
				matUrl = "# File not found: " + matUrl;
				files.Add(matUrl);
			}
		}

		ShowFiles();
	}

	private void ShowFiles()
	{
		dependencies.text = string.Join("\n", files);
		dependencies.fontStyle = FontStyle.Normal;
		Color color = dependencies.color;
		color.a = 1f;
		dependencies.color = color;
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
