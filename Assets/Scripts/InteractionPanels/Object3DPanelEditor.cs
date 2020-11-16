using System;
using System.Collections.Generic;
using System.IO;
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
	public string answerTexturesUrlRelative;

	public bool allowCancel => explorerPanel == null;

	private string oldObject3dName;
	private ExplorerPanel explorerPanel;
	private GameObject objectRenderer;

	private bool fileOpening;

	private List<string> files;
	private List<string> filePaths;
	private List<string> textures;

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

	public void Init(string initialTitle, List<string> initialUrl, string object3dName)
	{
		title.text = initialTitle;
		objectRenderer = GameObject.Find("ObjectRenderer");

		//NOTE(Jitse): If editing a point, save the old object name for possible deletion
		if (!String.IsNullOrEmpty(object3dName))
		{
			oldObject3dName = object3dName;
		}

		if (initialUrl != null)
		{
			if (initialUrl.Count > 0)
			{
				objectUrl.text = initialUrl[0];
			}
			if (initialUrl.Count > 1)
			{
				filePaths = new List<string>();
				files = new List<string>();
				textures = new List<string>();

				for (int i = 1; i < initialUrl.Count; i++)
				{
					filePaths.Add(initialUrl[i]);
					int index = initialUrl[i].IndexOf(object3dName, StringComparison.InvariantCulture) + object3dName.Length + 1;
					string relativePath = initialUrl[i].Substring(index);
					files.Add(relativePath);

					if (i > 1)
					{
						textures.Add(files[i - 1]);
					}
				}

				ShowFiles();
			}
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

		if (String.IsNullOrEmpty(objectUrl.text) || !File.Exists(objectUrl.text))
		{
			objectUrl.image.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerObjUrl = objectUrl.text;
			if (filePaths.Count > 0)
			{
				answerMatUrl = filePaths[0];
			}
			if (filePaths.Count > 1)
			{
				answerTexturesUrl = String.Join("\f", filePaths.ToArray(), 1, filePaths.Count - 1);
				answerTexturesUrlRelative = String.Join("\f", textures);
			}
			answerTitle = title.text;

			//NOTE(Jitse): Delete the old 3D object if there was one.
			if (oldObject3dName != null)
			{
				foreach (Transform object3d in objectRenderer.transform)
				{
					if (object3d.name == "holder_" + oldObject3dName)
					{
						Destroy(object3d.gameObject);
						break;
					}
				}
			}
		}
	}

	public void Browse()
	{
		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel, Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", "*.obj", "Select 3D object");

		fileOpening = true;
	}

	private void ResolveDependencies(string url)
	{
		string objLine;
		string matUrl = "";
		string folderPath = Path.GetDirectoryName(url);
		files = new List<string>();
		filePaths = new List<string>();
		textures = new List<string>();

		//NOTE(Jitse): Try to find a .mtl file reference.  
		//NOTE(cont.): This might loop through the entire .obj in some rare cases, if neither mtllib or usemtl is specified.
		using (var objFile = new StreamReader(url))
		{
			while ((objLine = objFile.ReadLine()) != null)
			{
				//NOTE(Jitse): Skip commented lines.
				if (objLine.StartsWith("#"))
				{
					continue;
				}

				//TODO(Jitse): Apparently (according to .obj wiki), more than one .mtl file may be referenced within the .obj file.
				//TODO(cont.): If updating this to allow multiple .mtl files, also update everywhere else where necessary.
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
		}

		if (matUrl != "")
		{
			string matUrlPath = Path.Combine(folderPath, matUrl);
			if (File.Exists(matUrlPath))
			{
				files.Add(matUrl);
				filePaths.Add(matUrlPath);

				//NOTE(Jitse): Find all texture references.  
				using (var mtlFile = new StreamReader(matUrlPath))
				{
					string mtlLine;
					while ((mtlLine = mtlFile.ReadLine()) != null)
					{
						//NOTE(Jitse): Skip commented lines.
						if (mtlLine.StartsWith("#"))
						{
							continue;
						}

						//NOTE(Jitse): Check if the line contains an extension
						mtlLine = mtlLine.Trim('\t');
						var extension = Path.GetExtension(mtlLine);

						if (extension.Length > 1 && !ContainsDigit(extension))
						{
							string textureFile = mtlLine.Substring(mtlLine.LastIndexOf(' ') + 1);
							textureFile = textureFile.Replace("\\\\", "\\");
							string textureFilePath = Path.Combine(folderPath, textureFile);

							if (File.Exists(textureFilePath))
							{
								if (!filePaths.Contains(textureFilePath))
								{
									filePaths.Add(Path.Combine(folderPath, textureFile));
									textures.Add(textureFile);
								}
							}
							else
							{
								textureFile = "# File not found: " + textureFile;
							}

							if (!files.Contains(textureFile))
							{
								files.Add(textureFile);
							}
						}
					}
				}
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
		dependencies.text = String.Join("\n", files);
		dependencies.fontStyle = FontStyle.Normal;
		var color = dependencies.color;
		color.a = 1f;
		dependencies.color = color;
	}

	private bool ContainsDigit(string text)
	{
		for (int i = 0; i < text.Length; i++)
		{
			if (Char.IsDigit(text[i]))
			{
				return true;
			}
		}
		return false;
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
