using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using Valve.VR;

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
	public static PlayerState playerState;
	public static List<Hittable> hittables;

	public GameObject interactionPointPrefab;
	public GameObject indexPanelPrefab;
	public GameObject imagePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject videoPanelPrefab;
	public GameObject multipleChoicePrefab;
	public GameObject audioPanelPrefab;
	public GameObject findAreaPanelPrefab;
	public GameObject multipleChoiceAreaPanelPrefab;
	public GameObject multipleChoiceImagePanelPrefab;
	public GameObject tabularDataPanelPrefab;
	public GameObject cameraRig;
	public GameObject projectorPrefab;

	public GameObject controllerLeft;
	public GameObject controllerRight;
	private Controller trackedControllerLeft;
	private Controller trackedControllerRight;

	private int interactionPointCount;

	private List<InteractionPointPlayer> interactionPoints;
	private List<InteractionPointPlayer> mandatoryInteractionPoints;
	private List<GameObject> videoPositions;
	private FileLoader fileLoader;
	private VideoController videoController;
	private List<GameObject> videoList;

	public AudioMixer mixer;

	private GameObject indexPanel;
	private Transform videoCanvas;
	private GameObject projector;

	private SaveFileData data;

	private bool isSeekbarOutOfView;
	private InteractionPointPlayer activeInteractionPoint;
	private string openVideo;

	private float mandatoryPauseFadeTime = 1f;
	private bool mandatoryPauseActive;
	public GameObject mandatoryPauseMessage;
	public GameObject mandatoryPauseMessageVR;

	private const float timeToInteract = 0.75f;
	private bool isInteractingWithPoint;
	private float interactionTimer;
	private EventSystem mainEventSystem;

	void Awake()
	{
		hittables = new List<Hittable>();
		Physics.autoSimulation = false;
	}

	void Start()
	{
		StartCoroutine(EnableVr());
		Canvass.sphereUIWrapper.SetActive(false);
		Canvass.sphereUIRenderer.SetActive(false);

		trackedControllerLeft = controllerLeft.GetComponent<Controller>();
		trackedControllerRight = controllerRight.GetComponent<Controller>();
		trackedControllerLeft.OnRotate += RotateCamera;
		trackedControllerRight.OnRotate += RotateCamera;

		interactionPoints = new List<InteractionPointPlayer>();
		mandatoryInteractionPoints = new List<InteractionPointPlayer>();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.controller;
		videoController.OnSeek += OnSeek;
		videoController.mixer = mixer;
		VideoControls.videoController = videoController;

		OpenFilePanel();

		mainEventSystem = EventSystem.current;
	}

	void Update()
	{
		//NOTE(Kristof): VR specific behaviour
		{
			if (XRSettings.enabled)
			{
				videoController.transform.position = Camera.main.transform.position;
				Canvass.seekbar.gameObject.SetActive(true);

				//NOTE(Kristof): Rotating the seekbar
				{
					//NOTE(Kristof): Seekbar rotation is the same as the seekbar's angle on the circle
					var seekbarAngle = Vector2.SignedAngle(new Vector2(Canvass.seekbar.transform.position.x, Canvass.seekbar.transform.position.z), Vector2.up);

					var fov = Camera.main.fieldOfView;
					//NOTE(Kristof): Camera rotation tells you to which angle on the circle the camera is looking towards
					var cameraAngle = Camera.main.transform.eulerAngles.y;

					//NOTE(Kristof): Calculate the absolute degree angle from the camera to the seekbar
					var distanceLeft = Mathf.Abs((cameraAngle - seekbarAngle + 360) % 360);
					var distanceRight = Mathf.Abs((cameraAngle - seekbarAngle - 360) % 360);

					var angle = Mathf.Min(distanceLeft, distanceRight);

					if (isSeekbarOutOfView)
					{
						if (angle < 2.5f)
						{
							isSeekbarOutOfView = false;
						}
					}
					else
					{
						if (angle > fov)
						{
							isSeekbarOutOfView = true;
						}
					}

					if (isSeekbarOutOfView)
					{
						var newAngle = Mathf.LerpAngle(seekbarAngle, cameraAngle, 0.025f);

						//NOTE(Kristof): Angle needs to be reversed, in Unity postive angles go clockwise while they go counterclockwise in the unit circle (cos and sin)
						//NOTE(Kristof): We also need to add an offset of 90 degrees because in Unity 0 degrees is in front of you, in the unit circle it is (1,0) on the axis
						var radianAngle = (-newAngle + 90) * Mathf.PI / 180;
						var x = 1.8f * Mathf.Cos(radianAngle);
						var y = Camera.main.transform.position.y - 2f;
						var z = 1.8f * Mathf.Sin(radianAngle);

						Canvass.seekbar.transform.position = new Vector3(x, y, z);
						Canvass.seekbar.transform.eulerAngles = new Vector3(30, newAngle, 0);
					}
				}

				//NOTE(Kristof): Rotating the Crosshair canvas
				{
					Ray cameraRay = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
					Canvass.crosshair.transform.position = cameraRay.GetPoint(90);
					Canvass.crosshair.transform.LookAt(Camera.main.transform);
				}
			}
			else
			{
				Canvass.seekbar.gameObject.SetActive(false);
				Canvass.crosshair.gameObject.SetActive(false);
			}
		}

		//NOTE(Simon): Enable/disable crosshair based on usage of controllers
		if (VRDevices.loadedControllerSet != VRDevices.LoadedControllerSet.NoControllers)
		{
			Crosshair.Disable();
		}
		else
		{
			Crosshair.Enable();
		}

		//NOTE(Simon): Zoom when not using HMD
		if (!XRSettings.enabled)
		{
			if (Input.mouseScrollDelta.y != 0)
			{
				Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - Input.mouseScrollDelta.y * 5, 20, 120);
			}
		}

		Ray interactionpointRay;
		//NOTE(Kristof): Deciding on which object the Ray will be based on
		//TODO(Simon): Prefers right over left controller
		{
			var controllerRay = new Ray();

			if (trackedControllerLeft != null && trackedControllerLeft.triggerPressed)
			{
				controllerRay = trackedControllerLeft.CastRay();
			}

			if (trackedControllerRight != null && trackedControllerRight.triggerPressed)
			{
				controllerRay = trackedControllerRight.CastRay();
			}

			interactionpointRay = VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers ? controllerRay : Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
		}

		isInteractingWithPoint = false;

		if (playerState == PlayerState.Watching)
		{
			if (Input.GetKeyDown(KeyCode.Space) && VRDevices.loadedSdk == VRDevices.LoadedSdk.None)
			{
				videoController.TogglePlay();
			}

			Seekbar.instance.RenderBlips(interactionPoints, trackedControllerLeft, trackedControllerRight);

			//Note(Simon): Interaction with points
			{
				var reversedRay = interactionpointRay.ReverseRay();
				//Note(Simon): Create a reversed raycast to find positions on the sphere with 

				RaycastHit hit;
				Physics.Raycast(reversedRay, out hit, 100, 1 << LayerMask.NameToLayer("interactionPoints"));

				//NOTE(Simon): Update visible interactionpoints
				for (int i = 0; i < interactionPoints.Count; i++)
				{
					bool pointActive = videoController.currentTime >= interactionPoints[i].startTime 
									&& videoController.currentTime <= interactionPoints[i].endTime;
					interactionPoints[i].point.SetActive(pointActive);
					interactionPoints[i].point.transform.GetChild(1).gameObject.SetActive(!interactionPoints[i].isSeen);
				}

				//NOTE(Simon): Interact with inactive interactionpoints
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

					//NOTE(Kristof): Using controllers
					if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
					{ 
						ActivateInteractionPoint(point);
					}
					//NOTE(Kristof): Not using controllers
					else
					{
						isInteractingWithPoint = true;

						if (interactionTimer > timeToInteract)
						{
							ActivateInteractionPoint(point);
						}
					}
				}

				//NOTE(Simon): Disable active interactionPoint if playback was started through seekbar
				if (videoController.playing && activeInteractionPoint != null)
				{
					DeactivateActiveInteractionPoint();
				}
			}

			//NOTE(Simon): Handle mandatory interactionPoints
			{
				double timeToNextPause = Double.MaxValue; 
				//NOTE(Simon): Find the next unseen mandatory interaction
				for (int i = 0; i < mandatoryInteractionPoints.Count; i++)
				{
					if (!mandatoryInteractionPoints[i].isSeen && mandatoryInteractionPoints[i].endTime > videoController.currentTime)
					{
						timeToNextPause = mandatoryInteractionPoints[i].endTime - videoController.currentTime;
						break;
					}
				}

				//NOTE(Simon): Set the playbackspeed. Speed will get lower the closer to the next pause we are.
				if (timeToNextPause < mandatoryPauseFadeTime)
				{
					float speed = (float)(timeToNextPause / mandatoryPauseFadeTime);
					if (timeToNextPause < .05f)
					{
						speed = 0;
					}

					videoController.SetPlaybackSpeed(speed);
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
					SetCanvasesActive(true);
					if (VRDevices.loadedSdk > VRDevices.LoadedSdk.None)
					{
						StartCoroutine(FadevideoCanvasOut(videoCanvas));
						EventManager.OnSpace();
						videoPositions.Clear();
						Seekbar.ReattachCompass();
					}
				}
				else
				{
					Debug.LogError("Couldn't open savefile");
				}
			}
		}

		//NOTE(Simon): Interaction with Hittables
		{
			RaycastHit hit;
			Physics.Raycast(interactionpointRay, out hit, 100, LayerMask.GetMask("UI", "WorldUI"));

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

			//NOTE(Simon): Set hover state when hvoered by controllers
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
				//NOTE(Kristof): Interacting with controller
				if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
				{
					hittable.hitting = true;
				}
				//NOTE(Kristof): Interacting without controllers
				else
				{
					isInteractingWithPoint = true;
					hittable.hovering = true;
					if (interactionTimer >= timeToInteract)
					{
						interactionTimer = -1;
						hittable.hitting = true;
					}
				}
			}
		}

		//NOTE(Simon): Interaction interactionTimer and Crosshair behaviour
		if (isInteractingWithPoint)
		{
			interactionTimer += Time.deltaTime;
			Crosshair.SetFillAmount(interactionTimer / timeToInteract);
		}
		else
		{
			interactionTimer = 0;
			Crosshair.SetFillAmount(interactionTimer / timeToInteract);
		}
	}

	private bool OpenFile(string projectPath)
	{
		data = SaveFile.OpenFile(projectPath);

		openVideo = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), SaveFile.videoFilename));
		fileLoader.LoadFile(openVideo);

		var tagsPath = Path.Combine(Application.persistentDataPath, data.meta.guid.ToString());
		var tags = SaveFile.ReadTags(tagsPath);
		TagManager.Instance.SetTags(tags);

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
					var panel = Instantiate(multipleChoicePrefab, Canvass.sphereUIPanelWrapper.transform);
					panel.GetComponent<MultipleChoicePanelSphere>().Init(newInteractionPoint.title, newInteractionPoint.body.Split('\f'));
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
					var correct = Int32.Parse(split[0]);
					var areaJson = split[1];
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
					var correct = Int32.Parse(newInteractionPoint.body);
					var urls = new List<string>();
					foreach (var file in filenames)
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
				default:
				{
					isValidPoint = false;
					Debug.Log("Invalid interactionPoint encountered");
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

		mandatoryInteractionPoints.Sort((x, y) => x.endTime.CompareTo(y.endTime));

		StartCoroutine(UpdatePointPositions());

		return true;
	}

	private void OpenFilePanel()
	{
		indexPanel = Instantiate(indexPanelPrefab);
		indexPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		playerState = PlayerState.Opening;
	}

	private void ActivateInteractionPoint(InteractionPointPlayer point)
	{
		var pointAngle = Mathf.Rad2Deg * Mathf.Atan2(point.position.x, point.position.z) - 90;
		Canvass.sphereUIRenderer.GetComponent<UISphere>().Activate(pointAngle);

		point.panel.SetActive(true);
		var button = point.panel.transform.Find("CloseSpherePanelButton").GetComponent<Button>();
		button.onClick.AddListener(DeactivateActiveInteractionPoint);
		activeInteractionPoint = point;
		point.isSeen = true;
		//point.point.GetComponentInChildren<TextMesh>().color = Color.black;
		//point.point.GetComponent<Renderer>().material.color = new Color(0.75f, 0.75f, 0.75f, 1);

		videoController.Pause();

		//NOTE(Simon): No two eventsystems can be active at the same, so disable the main one. The main one is used for all screenspace UI.
		//NOTE(Simon): The other eventsystem, that remains active, handles input for the spherical UI.
		mainEventSystem.enabled = false;
	}

	public void DeactivateActiveInteractionPoint()
	{
		Canvass.sphereUIRenderer.GetComponent<UISphere>().Deactivate();
		activeInteractionPoint.panel.SetActive(false);
		activeInteractionPoint = null;
		videoController.Play();

		mainEventSystem.enabled = true;
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
		point.point.transform.LookAt(Vector3.zero, Vector3.up);
		point.point.transform.RotateAround(point.point.transform.position, point.point.transform.up, 180);

		//NOTE(Simon): Add a number to interaction points
		point.point.transform.GetChild(0).gameObject.SetActive(true);
		point.point.GetComponentInChildren<TextMesh>().text = (++interactionPointCount).ToString();
		point.panel.SetActive(false);
		interactionPoints.Add(point);

		SetInteractionPointTag(point);
	}

	private void SetInteractionPointTag(InteractionPointPlayer point)
	{
		var shape = point.point.GetComponent<SpriteRenderer>();
		var text = point.point.GetComponentInChildren<TextMesh>();
		var tag = TagManager.Instance.GetTagById(point.tagId);

		shape.sprite = TagManager.Instance.ShapeForIndex(tag.shapeIndex);
		shape.color = tag.color;
		text.color = tag.color.IdealTextColor();
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

	private void SetCanvasesActive(bool active)
	{
		var seekbarCollider = Canvass.seekbar.gameObject.GetComponent<BoxCollider>();

		if (XRSettings.enabled)
		{
			Canvass.main.enabled = active;
		}
		Canvass.seekbar.enabled = active;
		seekbarCollider.enabled = active;
	}

	public void OnSeek(double time)
	{
		var desiredTime = time;
		//NOTE(Simon): Find the first unseen mandatory interaction, and set desiredTime to its endTime if we have seeked beyond that endTime
		for (int i = 0; i < mandatoryInteractionPoints.Count; i++)
		{
			if (!mandatoryInteractionPoints[i].isSeen && mandatoryInteractionPoints[i].endTime < desiredTime)
			{
				desiredTime = mandatoryInteractionPoints[i].endTime - 0.1;
				break;
			}
		}

		videoController.SeekNoTriggers(desiredTime);
	}

	public void OnVideoBrowserHologramUp()
	{
		if (videoList == null)
		{
			StartCoroutine(LoadVideos());
			projector.GetComponent<AnimateProjector>().TogglePageButtons(indexPanel);
		}
	}

	public void OnVideoBrowserAnimStop()
	{
		if (!projector.GetComponent<AnimateProjector>().state)
		{
			projector.transform.localScale = Vector3.zero;

			for (var i = videoCanvas.childCount - 1; i >= 0; i--)
			{
				Destroy(videoCanvas.GetChild(i).gameObject);
			}
			videoList = null;
		}
	}

	public void RotateCamera(int direction)
	{
		cameraRig.transform.localEulerAngles += direction * new Vector3(0, 30, 0);
	}

	public void BackToBrowser()
	{
		SetCanvasesActive(false);
		EventManager.OnSpace();
		Seekbar.ClearBlips();
		projector.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

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
		interactionPointCount = 0;

		OpenFilePanel();
	}

	public void ShowMandatoryInteractionMessage()
	{
		if (XRSettings.enabled)
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
		if (XRSettings.enabled)
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

	private IEnumerator EnableVr()
	{
		var supportedDevices = XRSettings.supportedDevices;
		//NOTE(Simon): We start with None as the default in the player settings. But in player we want to load OpenVR first (and in the future other APIs?) so reverse the array
		Array.Reverse(supportedDevices);
		XRSettings.LoadDeviceByName(supportedDevices);
		
		//NOTE(Kristof): wait one frame to allow the device to be loaded
		yield return null;

		if (XRSettings.loadedDeviceName.Equals("OpenVR"))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.OpenVr;
			XRSettings.enabled = true;
			SteamVR.Initialize(true);

			//NOTE(Kristof): Instantiate the projector
			{
				projector = Instantiate(projectorPrefab);
				projector.transform.position = new Vector3(4.5f, 0, 0);
				projector.transform.eulerAngles = new Vector3(0, 270, 0);
				projector.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

				projector.GetComponent<AnimateProjector>().Subscribe(this);
			}

			//NOTE(Kristof): Hide the main and seekbar canvas when in VR (they are toggled back on again after the lobby)
			SetCanvasesActive(false);

			//NOTE(Kristof): Move crosshair to crosshair canvas to display it in worldspace
			{
				var ch = Canvass.main.transform.Find("Crosshair");
				ch.SetParent(Canvass.crosshair.transform);
				ch.localPosition = Vector3.zero;
				ch.localEulerAngles = Vector3.zero;
				ch.localScale = Vector3.one;
				ch.gameObject.layer = LayerMask.NameToLayer("WorldUI");
			}

			Canvass.seekbar.transform.position = new Vector3(1.8f, Camera.main.transform.position.y - 2f, 0);

			fileLoader.MoveSeekbarToVRPos();
			VRDevices.BeginHandlingVRDeviceEvents();
		}
		else if (XRSettings.loadedDeviceName.Equals(""))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.None;
			controllerLeft.SetActive(false);
			controllerRight.SetActive(false);
		}
	}

	private IEnumerator LoadVideos()
	{
		var panel = indexPanel.GetComponentInChildren<IndexPanel>();
		if (panel != null)
		{
			while (!panel.isFinishedLoadingVideos)
			{
				//NOTE(Kristof): Wait for IndexPanel to finish instantiating videos GameObjects
				yield return null;
			}

			//NOTE(Kristof): ask the IndexPanel to pass the loaded videos
			var videos = panel.LoadedVideos();
			if (videos != null)
			{
				videoPositions = videoPositions ?? new List<GameObject>();
				videoList = videos;

				videoCanvas = projector.transform.root.Find("VideoCanvas").transform;
				videoCanvas.gameObject.GetComponent<Canvas>().sortingLayerName = "UIPanels";
				StartCoroutine(FadevideoCanvasIn(videoCanvas));

				for (int i = 0; i < videoList.Count; i++)
				{
					//NOTE(Kristof): Determine the next angle to put a video
					//NOTE 45f			offset serves to skip the dead zone
					//NOTE (i) * 33.75	place a video every 33.75 degrees 
					//NOTE 90f			camera rig rotation offset
					var nextAngle = 45f + (i * 33.75f) + 90f;
					var angle = -nextAngle * Mathf.PI / 180;
					var x = 9.8f * Mathf.Cos(angle);
					var z = 9.8f * Mathf.Sin(angle);

					//NOTE(Kristof): Parent object that sets location
					if (videoPositions.Count < i + 1)
					{
						videoPositions.Add(new GameObject("videoPosition"));
					}
					videoPositions[i].transform.SetParent(videoCanvas);
					videoPositions[i].transform.localScale = Vector3.one;
					videoPositions[i].transform.localPosition = new Vector3(x, 0, z);
					videoPositions[i].transform.LookAt(Camera.main.transform);
					videoPositions[i].transform.localEulerAngles += new Vector3(-videoPositions[i].transform.localEulerAngles.x, 0, 0);

					//NOTE(Kristof): Positioning the video relative to parent object
					var trans = videoList[i].GetComponent<RectTransform>();
					trans.SetParent(videoPositions[i].transform);
					trans.anchorMin = Vector2.up;
					trans.anchorMax = Vector2.up;
					trans.pivot = new Vector2(0.5f, 0.5f);
					trans.gameObject.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
					trans.localPosition = Vector3.zero;
					trans.localEulerAngles = new Vector3(0, 180, 0);
					trans.localScale = new Vector3(0.018f, 0.018f, 0.018f);
				}
			}
		}
		yield return null;
	}

	public IEnumerator PageSelector(int i)
	{
		switch (i)
		{
			case -1:
				indexPanel.GetComponent<IndexPanel>().Previous();
				break;
			case 1:
				indexPanel.GetComponent<IndexPanel>().Next();
				break;
		}

		//NOTE(Kristof): Wait for IndexPanel to destroy IndexPanelVideos
		yield return null;

		for (var index = videoPositions.Count - 1; index >= 0; index--)
		{
			var pos = videoPositions[index];
			if (pos.transform.childCount == 0)
			{
				Destroy(pos);
				videoPositions.Remove(pos);
			}
		}
		StartCoroutine(LoadVideos());
		projector.GetComponent<AnimateProjector>().TogglePageButtons(indexPanel);
	}

	private static IEnumerator FadevideoCanvasIn(Transform videoCanvas)
	{
		var group = videoCanvas.GetComponent<CanvasGroup>();

		for (float i = 0; i <= 1; i += Time.deltaTime * 1.5f)
		{
			group.alpha = i;
			yield return null;
		}
		videoCanvas.root.Find("UICanvas").gameObject.SetActive(true);
	}

	private static IEnumerator FadevideoCanvasOut(Transform videoCanvas)
	{
		videoCanvas.root.Find("UICanvas").gameObject.SetActive(false);
		var group = videoCanvas.GetComponent<CanvasGroup>();

		for (float i = 1; i >= 0; i -= Time.deltaTime * 1.5f)
		{
			group.alpha = i;
			yield return null;
		}
		//NOTE(Kristof): Force Alpha to 0;
		group.alpha = 0;
	}
}
