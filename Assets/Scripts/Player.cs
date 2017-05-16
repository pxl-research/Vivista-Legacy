using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum PlayerState
{
	Opening,
	Watching
}

public class InteractionPointPlayer
{
	public GameObject point;
	public GameObject panel;
	public Vector3 position;
	public Quaternion rotation;
	public InteractionType type;
	public string title;
	public string body;
	public double startTime;
	public double endTime;
}

public class Player : MonoBehaviour 
{
	private Vector3 prevMousePosition;
	private PlayerState playerState;
	
	private List<InteractionPointPlayer> interactionPoints;
	private FileLoader fileLoader;
	private VideoController videoController;

	public GameObject interactionPointPrefab;
	public GameObject openPanelPrefab;
	public GameObject imagePanelPrefab;
	public GameObject textPanelPrefab;

	private GameObject openPanel;

	private string openVideo;

	void Start () 
	{
		interactionPoints = new List<InteractionPointPlayer>();

		prevMousePosition = Input.mousePosition;
		
		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.videoController.GetComponent<VideoController>();
		OpenFilePanel();
		playerState = PlayerState.Opening;
	}
	
	void Update () 
	{
		if (playerState == PlayerState.Watching)
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}
		}

		if (playerState == PlayerState.Opening)
		{
			var panel = openPanel.GetComponent<OpenPanel>();
			if (panel.answered)
			{
				if(OpenFile(panel.answerFilename))
				{
					Destroy(openPanel);
					playerState = PlayerState.Watching;
					Canvass.modalBackground.SetActive(false);
				}
				else
				{
					Debug.Log("Couldn't open savefile");
				}
			}
		}
	}

	private bool OpenFile(string filename)
	{
		using (var fileContents = File.OpenText(Path.Combine(Application.persistentDataPath, filename)))
		{
			string str;
			try
			{
				str = fileContents.ReadToEnd();
			}
			catch (Exception e)
			{
				Debug.Log("Something went wrong while loading the file.");
				Debug.Log(e.ToString());
				return false;
			}
			
			var level = 0;
			var start = 0;
			var count = 0;
			var rising = true;

			var startVideo = str.IndexOf(':') + 1;
			var endVideo = str.IndexOf('\n') + 1;
			openVideo = str.Substring(startVideo, (endVideo - startVideo) - 2);
			
			var startPersp = str.IndexOf(':', endVideo) + 1;
			var endPersp = str.IndexOf('\n', endVideo) + 1;
			var perspective = (Perspective)Enum.Parse(typeof(Perspective), str.Substring(startPersp, (endPersp - startPersp) - 2));
			
			var stringObjects = new List<string>();
			
			for(var i = endPersp; i < str.Length; i++)
			{
				if (str[i] == '{')
				{
					if (level == 0)
					{
						start = i;
					}
					rising = true;
					level++;
				}
				if (str[i] == '}')
				{
					level--;
					rising = false;
				}

				count++;

				if (level == 0 && !rising)
				{
					stringObjects.Add(str.Substring(start, count - 1));
					count = 0;
					rising = true;
				}
				if (level < 0)
				{
					Debug.Log("Corrupted save file. Aborting");
					return false;
				}
			}

			fileLoader.LoadFile(openVideo);
			fileLoader.SetPerspective(perspective);

			var points = new List<InteractionpointSerialize>();

			foreach (var obj in stringObjects)
			{
				points.Add(JsonUtility.FromJson<InteractionpointSerialize>(obj));
			}

			for (var i = interactionPoints.Count - 1; i >= 0; i--)
			{
				RemoveInteractionPoint(interactionPoints[i]);
			}

			interactionPoints.Clear();

			foreach (var point in points)
			{
				//TODO(Simon): Fix rotation
				var newPoint = Instantiate(interactionPointPrefab, point.position, point.rotation);

				var newInteractionPoint = new InteractionPointPlayer
				{
					startTime = point.startTime,
					endTime = point.endTime,
					title = point.title,
					body = point.body,
					type = point.type,
					point = newPoint,
				};

				switch (newInteractionPoint.type)
				{
					case InteractionType.Text:
					{
						var panel = Instantiate(textPanelPrefab);
						panel.GetComponent<TextPanel>().Init(point.position, newInteractionPoint.title, newInteractionPoint.body);
						newInteractionPoint.panel = panel;
						break;
					}
					case InteractionType.Image:
					{
						var panel = Instantiate(imagePanelPrefab);
						panel.GetComponent<ImagePanel>().Init(point.position, newInteractionPoint.title, newInteractionPoint.body);
						newInteractionPoint.panel = panel;
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}

				AddInteractionPoint(newInteractionPoint);
			}
		}
		return true;
	}

	void OpenFilePanel()
	{
		openPanel = Instantiate(openPanelPrefab);
		openPanel.GetComponent<OpenPanel>().Init();
		openPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		playerState = PlayerState.Opening;
	}
	
	void AddInteractionPoint(InteractionPointPlayer point)
	{
		interactionPoints.Add(point);
	}
	
	void RemoveInteractionPoint(InteractionPointPlayer point)
	{
		interactionPoints.Remove(point);
		Destroy(point.point);
		if (point.panel != null)
		{
			Destroy(point.panel);
		}
	}
}
