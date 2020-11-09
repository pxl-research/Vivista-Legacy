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
	public InputField scalingInput;
	public InputField xInput;
	public InputField yInput;

	public bool answered;
	public string answerTitle;
	public string answerObjUrl;
	public string answerMatUrl;
	public string answerTexturesUrl;

	public bool allowCancel => explorerPanel == null;

	private ExplorerPanel explorerPanel;
	private GameObject objectRenderer;

	private bool fileOpening;

	private Button[] scalingButtons;
	private Button[] xButtons;
	private Button[] yButtons;

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

		scalingButtons = scalingInput.GetComponentsInChildren<Button>(true);
		scalingButtons[0].onClick.AddListener(IncreaseScaling);
		scalingButtons[1].onClick.AddListener(DecreaseScaling);

		xButtons = xInput.GetComponentsInChildren<Button>(true);
		xButtons[0].onClick.AddListener(IncreaseX);
		xButtons[1].onClick.AddListener(DecreaseX);

		yButtons = yInput.GetComponentsInChildren<Button>(true);
		yButtons[0].onClick.AddListener(IncreaseY);
		yButtons[1].onClick.AddListener(DecreaseY);

		title.onValueChanged.AddListener(_ => OnInputChange(title));
		objectUrl.onValueChanged.AddListener(_ => OnInputChange(objectUrl));
		scalingInput.onValueChanged.AddListener(_ => OnScalingValueChanged(scalingInput));
		xInput.onValueChanged.AddListener(_ => OnXYValueChanged(xInput));
		yInput.onValueChanged.AddListener(_ => OnXYValueChanged(yInput));
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
					mtlLine = mtlLine.Trim('\t');
					var extension = Path.GetExtension(mtlLine);
					
					if (extension.Length > 1 && !ContainsDigit(extension))
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

	private void IncreaseScaling()
	{
		double value = Convert.ToDouble(scalingInput.text);
		if (value < 0.1)
		{
			value += 0.01;
		}
		else if (value < 1)
		{
			value += 0.1;
		}
		else
		{
			value += 1;
		}
		scalingInput.text = $"{value}";
	}

	private void IncreaseX()
	{
		int value = Convert.ToInt32(xInput.text);
		if (Math.Abs(value) < 100)
		{
			value += 1;
		}
		else
		{
			value += 5;
		}
		if (value > 500)
		{
			value = 500;
		}
		xInput.text = $"{value}";
	}

	private void IncreaseY()
	{
		int value = Convert.ToInt32(yInput.text);
		if (Math.Abs(value) < 100)
		{
			value += 1;
		}
		else
		{
			value += 5;
		}
		if (value > 500)
		{
			value = 500;
		}
		yInput.text = $"{value}";
	}

	private void DecreaseScaling()
	{
		double value = Convert.ToDouble(scalingInput.text);
		if (value <= 0.1)
		{
			value -= 0.01;
			if (value <= 0)
			{
				value = 0.01;
			}
		}
		else if (value <= 1)
		{
			value -= 0.1;
			if (value < 0.1)
			{
				value = 0.1;
			}
		}
		else
		{
			value -= 1;
			if (value < 1)
			{
				value = 1f;
			}
		}
		scalingInput.text = $"{value}";
	}

	private void DecreaseX()
	{
		int value = Convert.ToInt32(xInput.text);
		if (Math.Abs(value) <= 100)
		{
			value -= 1;
		}
		else
		{
			value -= 5;
		}
		if (value < -500)
		{
			value = -500;
		}
		xInput.text = $"{value}";
	}

	private void DecreaseY()
	{
		int value = Convert.ToInt32(yInput.text);
		if (Math.Abs(value) <= 100)
		{
			value -= 1;
		}
		else
		{
			value -= 5;
		}
		if (value < -500)
		{
			value = -500;
		}
		yInput.text = $"{value}";
	}


	public void HoverScaling()
	{
		SetButtonStates(scalingButtons);
	}

	public void HoverX()
	{
		SetButtonStates(xButtons);
	}

	public void HoverY()
	{
		SetButtonStates(yButtons);
	}

	private void SetButtonStates(Button[] buttons)
	{
		bool oldState = buttons[0].IsActive();
		buttons[0].gameObject.SetActive(!oldState);
		buttons[1].gameObject.SetActive(!oldState);
	}

	public void OnScalingValueChanged(InputField input)
	{
		if (input.text.Length == 0)
		{
			input.text = "1";
		} 
		else
		{
			double value = Convert.ToDouble(input.text);
			if (value > 100)
			{
				input.text = "100";
			}
			else if (value <= 0)
			{
				input.text = "0";
			}
		}
	}

	public void OnXYValueChanged(InputField input)
	{
		if (input.text.Length == 0)
		{
			input.text = "0";
		}
		else
		{
			int value = Convert.ToInt32(input.text);
			if (value > 500)
			{
				input.text = "500";
			}
			else if (value < -500)
			{
				input.text = "-500";
			}
		}
	}
}
