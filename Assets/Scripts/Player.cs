using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
//using Valve.VR;
using VRstudios;

public enum PlayerState
{
	Opening,
	Watching,
}

public class InteractionPointPlayer
{
	public GameObject point;
	public GameObject panel;
	public Vector3 position;
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public int tagId;
	public bool mandatory;
	public bool isSeen;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

public class Player : MonoBehaviour
{
	public static Player Instance;
	public static PlayerState playerState;
	public static List<Hittable> hittables;

	public GameObject interactionPointPrefab;
	public GameObject indexPanelPrefab;
	public GameObject chapterSelectorPrefab;
	public GameObject imagePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject videoPanelPrefab;
	public GameObject multipleChoicePrefab;
	public GameObject audioPanelPrefab;
	public GameObject findAreaPanelPrefab;
	public GameObject multipleChoiceAreaPanelPrefab;
	public GameObject multipleChoiceImagePanelPrefab;
	public GameObject tabularDataPanelPrefab;
	public GameObject chapterPanelPrefab;
	public GameObject cameraOrigin;

	public GameObject XRInputPrefab;

	public GameObject controllerLeft;
	public GameObject controllerRight;
	private Controller trackedControllerLeft;
	private Controller trackedControllerRight;

	private List<InteractionPointPlayer> interactionPoints;
	private List<InteractionPointPlayer> shownInteractionPoints;
	private List<InteractionPointPlayer> mandatoryInteractionPoints;
	private FileLoader fileLoader;
	private VideoController videoController;
	private ChapterSelectorPanel chapterSelector;
	public ChapterTransitionPanel chapterTransitionPanel;
	private GameObject indexPanel;
	public GameObject chapterSelectorHolderVR;

	public AudioMixer mixer;

	private SaveFileData data;

	private InteractionPointPlayer activeInteractionPoint;
	private string openVideo;

	private float pauseFadeTime = 0.5f;
	private bool mandatoryPauseActive;
	private bool chapterTransitionActive;
	private Chapter previousChapter;
	public GameObject mandatoryPauseMessage;
	public GameObject mandatoryPauseMessageVR;

	private EventSystem mainEventSystem;

	void Awake()
	{
		hittables = new List<Hittable>();
		Physics.autoSimulation = false;

		SaveFile.MoveProjectsToCorrectFolder();
	}

	void Start()
	{
		Instance = this;

		Canvass.sphereUIWrapper.SetActive(false);
		Canvass.sphereUIRenderer.SetActive(false);
		Seekbar.instance.compass.SetActive(false);
		Seekbar.instanceVR.compass.SetActive(false);

		interactionPoints = new List<InteractionPointPlayer>();
		shownInteractionPoints = new List<InteractionPointPlayer>();
		mandatoryInteractionPoints = new List<InteractionPointPlayer>();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.controller;
		videoController.OnSeek += OnSeek;
		videoController.mixer = mixer;

		OpenIndexPanel();

		mainEventSystem = EventSystem.current;

		trackedControllerLeft = controllerLeft.GetComponent<Controller>();
		trackedControllerRight = controllerRight.GetComponent<Controller>();
		trackedControllerLeft.OnRotate += RotateCamera;
		trackedControllerRight.OnRotate += RotateCamera;
	}

	void Update()
	{
		bool isVR = XRSettings.isDeviceActive;
		Seekbar.instance.gameObject.SetActive(!isVR);
		Seekbar.instanceVR.gameObject.SetActive(isVR);

		//NOTE(Simon): Sync videoController/videoMesh pos with camera pos. 
		videoController.transform.position = Camera.main.transform.position;

		var interactionpointRay = new Ray();
		//NOTE(Kristof): Deciding on which object the Ray will be based on
		//NOTE(Simon): Prefers right over left controller
		{
			if (trackedControllerLeft != null && trackedControllerLeft.triggerPressed)
			{
				interactionpointRay = trackedControllerLeft.CastRay();
			}

			if (trackedControllerRight != null && trackedControllerRight.triggerPressed)
			{
				interactionpointRay = trackedControllerRight.CastRay();
			}

			if (!XRSettings.isDeviceActive && Input.GetMouseButtonUp(0))
			{
				interactionpointRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			}
		}

		if (playerState == PlayerState.Watching)
		{
			RefreshShownInteractionPoints();

			if (Input.GetKeyDown(KeyCode.Space) && !isVR)
			{
				videoController.TogglePlay();
			}

			if (isVR)
			{
				Seekbar.instanceVR.RenderBlips(shownInteractionPoints);
			}
			else
			{
				Seekbar.instance.RenderBlips(shownInteractionPoints);
			}

			//Note(Simon): Interaction with points
			if (!chapterTransitionActive)
			{
				var reversedRay = interactionpointRay.ReverseRay();
				//Note(Simon): Create a reversed raycast to find positions on the sphere with 
				Physics.Raycast(reversedRay, out var hit, 100, 1 << LayerMask.NameToLayer("interactionPoints"));

				//NOTE(Simon): Update visible interactionpoints
				foreach (var point in shownInteractionPoints)
				{
					point.point.SetActive(true);
					point.point.GetComponent<InteractionPointRenderer>().SetPingActive(!point.isSeen);
				}

				foreach (var point in GetInactiveInteractionPoints())
				{
					point.point.SetActive(false);
				}

				//NOTE(Simon): Activate hit interactionPoint
				if (activeInteractionPoint == null && hit.transform != null)
				{
					var pointGO = hit.transform.gameObject;
					InteractionPointPlayer point = null;

					for (int i = 0; i < interactionPoints.Count; i++)
					{
						if (pointGO == interactionPoints[i].point)
						{
							point = interactionPoints[i];
							break;
						}
					}

					ActivateInteractionPoint(point);
				}

				//NOTE(Simon): Disable active interactionPoint if playback was started through seekbar
				if (videoController.playing && activeInteractionPoint != null)
				{
					DeactivateActiveInteractionPoint();
				}
			}

			//NOTE(Simon): Handle mandatory interactionPoints
			if (!chapterTransitionActive)
			{
				double timeToNextPause = Double.MaxValue;
				var interactionsInChapter = MandatoryInteractionsForTime(videoController.currentTime);

				//NOTE(Simon): Find the next unseen mandatory interaction in this chapter
				for (int i = 0; i < interactionsInChapter.Count; i++)
				{
					if (!interactionsInChapter[i].isSeen && interactionsInChapter[i].endTime > videoController.currentTime)
					{
						timeToNextPause = interactionsInChapter[i].endTime - videoController.currentTime;
						break;
					}
				}

				if (timeToNextPause < pauseFadeTime)
				{
					videoController.SetPlaybackSpeed(0f);
					if (!mandatoryPauseActive)
					{
						ShowMandatoryInteractionMessage();
						mandatoryPauseActive = true;
					}
				}
				else
				{
					if (mandatoryPauseActive)
					{
						mandatoryPauseActive = false;
						HideMandatoryInteractionMessage();
					}

					videoController.SetPlaybackSpeed(1f);
				}
			}

			//NOTE(Simon): Handle chapter transitions
			if (!chapterTransitionActive && !mandatoryPauseActive)
			{
				var currentChapter = ChapterManager.Instance.ChapterForTime(videoController.currentTime);
				if (currentChapter != previousChapter)
				{
					videoController.SetPlaybackSpeed(0);
					chapterTransitionActive = true;
					chapterTransitionPanel.SetChapter(currentChapter);
					chapterTransitionPanel.StartTransition(OnChapterTransitionFinish);
					previousChapter = currentChapter;
				}
			}
		}

		if (playerState == PlayerState.Opening)
		{
			var panel = indexPanel.GetComponent<IndexPanel>();

			if (panel.answered)
			{
				var projectPath = Path.Combine(Application.persistentDataPath, panel.answerVideoId);
				if (OpenFile(projectPath))
				{
					Destroy(indexPanel);
					playerState = PlayerState.Watching;
					Canvass.modalBackground.SetActive(false);

					chapterTransitionActive = false;
					chapterSelector = Instantiate(chapterSelectorPrefab, Canvass.main.transform, false).GetComponent<ChapterSelectorPanel>();
					if (isVR)
					{
						chapterSelector.transform.SetParent(chapterSelectorHolderVR.transform, false);
						chapterSelector.transform.localPosition = Vector3.zero;
					}
					chapterSelector.Init(videoController);
				}
				else
				{
					Debug.LogError("Couldn't open savefile");
				}
			}
		}

		//NOTE(Simon): Interaction with Hittables
		{
			Physics.Raycast(interactionpointRay, out var hit, 100, LayerMask.GetMask("UI", "WorldUI"));

			var controllerList = new List<Controller>
			{
				trackedControllerLeft,
				trackedControllerRight
			};

			//NOTE(Simon): Reset all hittables
			foreach (var hittable in hittables)
			{
				if (hittable == null)
				{
					continue;
				}

				//NOTE(Jitse): Check if a hittable is being held down
				if (!(controllerList[0].triggerDown || controllerList[1].triggerDown))
				{
					hittable.hitting = false;
				}

				hittable.hovering = false;
			}

			//NOTE(Simon): Set hover state when hovered by controllers
			foreach (var con in controllerList)
			{
				if (con.uiHovering && con.hoveredGo != null)
				{
					var hittable = con.hoveredGo.GetComponent<Hittable>();
					if (hittable != null)
					{
						hittable.hovering = true;
					}
				}
			}

			//NOTE(Simon): Set hitting and hovering in hittables
			if (hit.transform != null)
			{
				var hittable = hit.transform.GetComponent<Hittable>();
				if (hittable != null)
				{
					hittable.hitting = true;
				}
			}
		}
	}

	private void OnChapterTransitionFinish()
	{
		Debug.Log("Animation done");
		videoController.SetPlaybackSpeed(1);
		
		chapterTransitionActive = false;
	}

	private bool OpenFile(string projectPath)
	{
		data = SaveFile.OpenFile(projectPath);

		openVideo = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), SaveFile.videoFilename));
		fileLoader.LoadFile(openVideo);

		var tagsPath = Path.Combine(Application.persistentDataPath, data.meta.guid.ToString());
		var tags = SaveFile.ReadTags(tagsPath);
		TagManager.Instance.SetTags(tags);

		var chaptersPath = Path.Combine(Application.persistentDataPath, data.meta.guid.ToString());
		var chapters = SaveFile.ReadChapters(chaptersPath);
		ChapterManager.Instance.SetChapters(chapters);

		//NOTE(Simon): Sort all interactionpoints based on their timing
		data.points.Sort((x, y) => x.startTime != y.startTime
										? x.startTime.CompareTo(y.startTime)
										: x.endTime.CompareTo(y.endTime));

		foreach (var point in data.points)
		{
			var newPoint = Instantiate(interactionPointPrefab);

			var newInteractionPoint = new InteractionPointPlayer
			{
				startTime = point.startTime,
				endTime = point.endTime,
				title = point.title,
				body = point.body,
				filename = point.filename,
				type = point.type,
				point = newPoint,
				tagId = point.tagId,
				mandatory = point.mandatory,
				returnRayOrigin = point.returnRayOrigin,
				returnRayDirection = point.returnRayDirection
			};

			bool isValidPoint = true;

			switch (newInteractionPoint.type)
			{
				case InteractionType.None:
				{
					isValidPoint = false;
					Debug.Log("InteractionPoint with Type None encountered");
					break;
				}
				case InteractionType.Text:
				{
					var panel = Instantiate(textPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					panel.GetComponent<TextPanel>().Init(newInteractionPoint.title, newInteractionPoint.body);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Image:
				{
					var panel = Instantiate(imagePanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					var filenames = newInteractionPoint.filename.Split('\f');
					var urls = new List<string>();
					foreach (var file in filenames)
					{
						string url = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), file));
						urls.Add(url);
					}
					panel.GetComponent<ImagePanel>().Init(newInteractionPoint.title, urls);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Video:
				{
					var panel = Instantiate(videoPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					string url = Path.Combine(Application.persistentDataPath, data.meta.guid.ToString(), newInteractionPoint.filename);
					panel.GetComponent<VideoPanel>().Init(newInteractionPoint.title, url);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.MultipleChoice:
				{
					var split = newInteractionPoint.body.Split(new[] { '\f' }, 2);
					int correct = Int32.Parse(split[0]);
					var panel = Instantiate(multipleChoicePrefab, Canvass.sphereUIPanelWrapper.transform);
					panel.GetComponent<MultipleChoicePanelSphere>().Init(newInteractionPoint.title, correct, split[1].Split('\f'));
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Audio:
				{
					var panel = Instantiate(audioPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					string url = Path.Combine(Application.persistentDataPath, data.meta.guid.ToString(), newInteractionPoint.filename);
					panel.GetComponent<AudioPanel>().Init(newInteractionPoint.title, url);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.FindArea:
				{
					var panel = Instantiate(findAreaPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					var areas = Area.ParseFromSave(newInteractionPoint.filename, newInteractionPoint.body);

					panel.GetComponent<FindAreaPanelSphere>().Init(newInteractionPoint.title, areas);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.MultipleChoiceArea:
				{
					var split = newInteractionPoint.body.Split(new[] { '\f' }, 2);
					int correct = Int32.Parse(split[0]);
					string areaJson = split[1];
					var panel = Instantiate(multipleChoiceAreaPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					var areas = Area.ParseFromSave(newInteractionPoint.filename, areaJson);

					panel.GetComponent<MultipleChoiceAreaPanelSphere>().Init(newInteractionPoint.title, areas, correct);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.MultipleChoiceImage:
				{
					var panel = Instantiate(multipleChoiceImagePanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					var filenames = newInteractionPoint.filename.Split('\f');
					int correct = Int32.Parse(newInteractionPoint.body);
					var urls = new List<string>();
					foreach (string file in filenames)
					{
						string url = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), file));
						urls.Add(url);
					}
					panel.GetComponent<MultipleChoiceImagePanelSphere>().Init(newInteractionPoint.title, urls, correct);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.TabularData:
				{
					var panel = Instantiate(tabularDataPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					string[] body = newInteractionPoint.body.Split(new[] { '\f' }, 3);
					int rows = Int32.Parse(body[0]);
					int columns = Int32.Parse(body[1]);

					panel.GetComponent<TabularDataPanelSphere>().Init(newInteractionPoint.title, rows, columns, body[2].Split('\f'));
					
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Chapter:
				{
					var panel = Instantiate(chapterPanelPrefab, Canvass.sphereUIPanelWrapper.transform);
					int chapterId = Int32.Parse(newInteractionPoint.body);
					panel.GetComponent<ChapterPanelSphere>().Init(newInteractionPoint.title, chapterId, this);
					newInteractionPoint.panel = panel;
					break;
				}
				default:
				{
					isValidPoint = false;
					Debug.LogError("Invalid interactionPoint encountered");
					break;
				}
			}

			if (isValidPoint)
			{
				if (newInteractionPoint.mandatory)
				{
					mandatoryInteractionPoints.Add(newInteractionPoint);
				}

				AddInteractionPoint(newInteractionPoint);
			}
			else
			{
				Destroy(newPoint);
			}
		}

		//NOTE(Simon): For mandatoryInteractionPoints it makes more sense to sort by endTime
		mandatoryInteractionPoints.Sort((x, y) => x.endTime.CompareTo(y.endTime));

		StartCoroutine(UpdatePointPositions());

		if (VRDevices.loadedSdk == VRDevices.LoadedSdk.None)
		{
			Seekbar.instance.compass.SetActive(true);
		}
		else
		{
			Seekbar.instanceVR.compass.SetActive(true);
		}

		return true;
	}

	private void OpenIndexPanel()
	{
		indexPanel = Instantiate(indexPanelPrefab);
		indexPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		playerState = PlayerState.Opening;
	}

	private void ActivateInteractionPoint(InteractionPointPlayer point)
	{
		float pointAngle = Mathf.Rad2Deg * Mathf.Atan2(point.position.x, point.position.z) - 90;
		Canvass.sphereUIRenderer.GetComponent<UISphere>().Activate(pointAngle);

		point.panel.SetActive(true);

		//NOTE(Simon): We do this here, because each interactionType has its own script. So that mean adding the event in many places
		var button = point.panel.transform.Find("CloseSpherePanelButton").GetComponent<Button>();
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(DeactivateActiveInteractionPoint);

		activeInteractionPoint = point;
		point.isSeen = true;

		videoController.Pause();

		InteractionPointers.Instance.ShouldRender(false);

		//NOTE(Simon): No two eventsystems can be active at the same, so disable the main one. The main one is used for all screenspace UI.
		//NOTE(Simon): The other eventsystem, that remains active, handles input for the spherical UI.
		mainEventSystem.enabled = false;
	}

	public void DeactivateActiveInteractionPoint()
	{
		if (activeInteractionPoint != null)
		{
			Canvass.sphereUIRenderer.GetComponent<UISphere>().Deactivate();
			activeInteractionPoint.panel.SetActive(false);
			activeInteractionPoint = null;
			videoController.Play();

			InteractionPointers.Instance.ShouldRender(true);

			mainEventSystem.enabled = true;
		}
	}

	public void SuspendInteractionPoint()
	{
		Canvass.sphereUIRenderer.GetComponent<UISphere>().Suspend();
		mainEventSystem.enabled = true;
	}

	public void UnsuspendInteractionPoint()
	{
		Canvass.sphereUIRenderer.GetComponent<UISphere>().Unsuspend();
		mainEventSystem.enabled = false;
	}

	private void AddInteractionPoint(InteractionPointPlayer point)
	{
		point.point.GetComponent<InteractionPointRenderer>().Init(point);
		interactionPoints.Add(point);
	}

	private void RemoveInteractionPoint(InteractionPointPlayer point)
	{
		interactionPoints.Remove(point);
		Destroy(point.point);
		if (point.panel != null)
		{
			Destroy(point.panel);
		}
	}

	public float OnSeek(double time)
	{
		double desiredTime = time;
		var interactionsInChapter = MandatoryInteractionsForTime(desiredTime);

		//NOTE(Simon): Find the first unseen mandatory interaction in this chapter, and set desiredTime to its endTime if we have seeked beyond that endTime
		for (int i = 0; i < interactionsInChapter.Count; i++)
		{
			if (!interactionsInChapter[i].isSeen && interactionsInChapter[i].endTime < desiredTime)
			{
				desiredTime = interactionsInChapter[i].endTime - 0.1;
				break;
			}
		}

		return (float)desiredTime;
	}

	//NOTE(Simon): This filter returns all mandatory interactions in this chapter
	public List<InteractionPointPlayer> MandatoryInteractionsForTime(double desiredTime)
	{
		var interactionsInChapter = new List<InteractionPointPlayer>();
		if (mandatoryInteractionPoints.Count == 0)
		{
			return interactionsInChapter;
		}

		float nextChapterTime = ChapterManager.Instance.NextChapterTime(desiredTime);
		float currentChapterTime = ChapterManager.Instance.CurrentChapterTime(desiredTime);

		for (int i = 0; i < mandatoryInteractionPoints.Count; i++)
		{
			//NOTE(Simon): Ignore interactions before this chapter
			if (mandatoryInteractionPoints[i].endTime < currentChapterTime)
			{
				continue;
			}

			//NOTE(Simon): Ignore interactions after this chapter
			if (mandatoryInteractionPoints[i].endTime > nextChapterTime)
			{
				break;
			}

			interactionsInChapter.Add(mandatoryInteractionPoints[i]);
		}

		return interactionsInChapter;
	}

	public void RotateCamera(int direction)
	{
		cameraOrigin.transform.localEulerAngles += direction * new Vector3(0, 30, 0);
	}

	public void BackToBrowser()
	{
		StartCoroutine(DisableVR());

		previousChapter = null;

		Seekbar.instance.compass.SetActive(false);
		Seekbar.instanceVR.compass.SetActive(false);
		Seekbar.ClearBlips();
		if (chapterSelector != null)
		{
			Destroy(chapterSelector.gameObject);
		}

		videoController.Pause();

		if (activeInteractionPoint != null)
		{
			DeactivateActiveInteractionPoint();
		}

		for (var j = interactionPoints.Count - 1; j >= 0; j--)
		{
			RemoveInteractionPoint(interactionPoints[j]);
		}

		interactionPoints.Clear();
		mandatoryInteractionPoints.Clear();

		OpenIndexPanel();
	}

	public void ShowMandatoryInteractionMessage()
	{
		if (XRSettings.isDeviceActive)
		{
			mandatoryPauseMessageVR.SetActive(true);
		}
		else
		{
			mandatoryPauseMessage.SetActive(true);
		}
	}

	public void HideMandatoryInteractionMessage()
	{
		if (XRSettings.isDeviceActive)
		{
			mandatoryPauseMessageVR.SetActive(false);
		}
		else
		{
			mandatoryPauseMessage.SetActive(false);
		}
	}

	public Controller[] GetControllers()
	{
		return new [] { trackedControllerLeft, trackedControllerRight};
	}

	public List<InteractionPointPlayer> GetShownInteractionPoints()
	{
		return shownInteractionPoints;
	}

	private void RefreshShownInteractionPoints()
	{
		shownInteractionPoints.Clear();
		
		for (int i = 0; i < interactionPoints.Count; i++)
		{
			bool pointActive = videoController.currentTime >= interactionPoints[i].startTime
								&& videoController.currentTime <= interactionPoints[i].endTime;
			if (pointActive)
			{
				shownInteractionPoints.Add(interactionPoints[i]);
			}
		}
	}

	public List<InteractionPointPlayer> GetInactiveInteractionPoints()
	{
		var interactions = new List<InteractionPointPlayer>();

		for (int i = 0; i < interactionPoints.Count; i++)
		{
			bool pointActive = videoController.currentTime >= interactionPoints[i].startTime
								&& videoController.currentTime <= interactionPoints[i].endTime;
			if (!pointActive)
			{
				interactions.Add(interactionPoints[i]);
			}
		}

		return interactions;
	}

	//NOTE(Simon): This needs to be a coroutine so that we can wait a frame before recalculating point positions. If this were run in the first frame, collider positions would not be up to date yet.
	private IEnumerator UpdatePointPositions()
	{
		//NOTE(Simon): wait one frame
		yield return null;

		foreach (var interactionPoint in interactionPoints)
		{
			var ray = new Ray(interactionPoint.returnRayOrigin, interactionPoint.returnRayDirection);

			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("Default")))
			{
				var drawLocation = hit.point;
				var trans = interactionPoint.point.transform;

				trans.position = drawLocation;
				trans.LookAt(Camera.main.transform);
				//NOTE(Kristof): Turn it around so it actually faces the camera
				trans.localEulerAngles = new Vector3(0, trans.localEulerAngles.y + 180, 0);

				interactionPoint.position = drawLocation;
			}
		}
	}

	//https://stackoverflow.com/questions/36702228/enable-disable-vr-from-code
	public IEnumerator EnableVR()
	{
		Instantiate(XRInputPrefab);
		if (XRGeneralSettings.Instance.Manager.activeLoader == null)
		{
			StartCoroutine(XRGeneralSettings.Instance.Manager.InitializeLoader());
		}

		if (XRGeneralSettings.Instance.Manager.activeLoader != null)
		{
			XRGeneralSettings.Instance.Manager.StartSubsystems();
			VRDevices.loadedSdk = VRDevices.LoadedSdk.OpenVr;

			VRDevices.BeginHandlingVRDeviceEvents();
			controllerLeft.SetActive(true);
			controllerRight.SetActive(true);
		}
		else
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.None;
			controllerLeft.SetActive(false);
			controllerRight.SetActive(false);
		}

		trackedControllerLeft = controllerLeft.GetComponent<Controller>();
		trackedControllerRight = controllerRight.GetComponent<Controller>();

		yield return null;
	}

	public IEnumerator DisableVR()
	{
		if (VRDevices.loadedSdk != VRDevices.LoadedSdk.None)
		{
			Destroy(XRInput.singleton.gameObject);
			yield return new WaitForEndOfFrame();
			XRGeneralSettings.Instance.Manager.StopSubsystems();
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();
			VRDevices.loadedSdk = VRDevices.LoadedSdk.None;
			controllerLeft.SetActive(false);
			controllerRight.SetActive(false);
		}
	}
}
