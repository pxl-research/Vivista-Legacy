using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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
	public float interactionTimer;
	public bool isStartPoint = false;
	public bool isTouched;
	public GameObject blip;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

public class Player : MonoBehaviour
{
	public static PlayerState playerState;
	public static List<Hittable> hittables;

	public GameObject interactionPointPrefab;
	public GameObject startPointGroup;
	public GameObject indexPanelPrefab;
	public GameObject imagePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject videoPanelPrefab;
	public GameObject multipleChoicePrefab;
	public GameObject cameraRig;
	public GameObject localAvatarPrefab;
	public GameObject projectorPrefab;
	public GameObject compassBlipPrefab;

	public GameObject controllerLeft;
	public GameObject controllerRight;

	private int interactionPointCount;

	private List<InteractionPointPlayer> interactionPoints;
	private List<GameObject> startPoints;
	private List<GameObject> videoPositions;
	private FileLoader fileLoader;
	private VideoController videoController;
	private List<GameObject> videoList;
	private Image crosshair;
	private Image crosshairTimer;
	private Text blipCounter;

	private GameObject indexPanel;
	private Transform videoCanvas;
	private GameObject projector;

	private VRControllerState_t controllerLeftOldState;
	private VRControllerState_t controllerRightOldState;
	private SteamVR_TrackedController trackedControllerLeft;
	private SteamVR_TrackedController trackedControllerRight;

	private SaveFile.SaveFileData data;

	private bool isOutofView;
	private int activePoints;
	private string openVideo;
	private int remainingPoints;

	private const float timeToInteract = 0.75f;
	private Color GRAY = new Color(0.75f, 0.75f, 0.75f, 1);
	private bool interacting;
	private float _interactionTimer;

	private bool[] cameraRigMovable = new bool[2];

	void Awake()
	{
		hittables = new List<Hittable>();
	}

	void Start()
	{
		StartCoroutine(EnableVr());

		trackedControllerLeft = controllerLeft.GetComponent<SteamVR_TrackedController>();
		trackedControllerRight = controllerRight.GetComponent<SteamVR_TrackedController>();

		interactionPoints = new List<InteractionPointPlayer>();
		startPoints = new List<GameObject>();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.videoController.GetComponent<VideoController>();
		OpenFilePanel();
		playerState = PlayerState.Opening;
		crosshair = Canvass.main.transform.Find("Crosshair").GetComponent<Image>();
		crosshairTimer = crosshair.transform.Find("CrosshairTimer").GetComponent<Image>();

		//NOTE(Kristof): VR specific settings
		if (XRSettings.enabled)
		{
			//NOTE(Kristof): Instantiate the projector
			{
				projector = Instantiate(projectorPrefab);
				projector.transform.position = new Vector3(4.5f, 0, 0);
				projector.transform.eulerAngles = new Vector3(0, 270, 0);
				projector.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

				projector.GetComponent<AnimateProjector>().Subscribe(this);
			}

			//NOTE(Kristof): Hide the main and seekbar canvas when in VR (they are toggled back on again after tutorial mode)
			Togglecanvasses();

			//NOTE(Kristof): Moving crosshair to crosshair canvas to display it in worldspace
			{
				var ch = Canvass.main.transform.Find("Crosshair");
				ch.parent = Canvass.crosshair.transform;
				ch.localPosition = Vector3.zero;
				ch.localEulerAngles = Vector3.zero;
				ch.localScale = Vector3.one;
				ch.gameObject.layer = LayerMask.NameToLayer("WorldUI");
			}

			StartCoroutine(InstantiateStartPointGroup());

			Canvass.seekbar.transform.position = new Vector3(1.8f, Camera.main.transform.position.y - 2f, 0);
		}

		VideoControls.videoController = videoController;

		blipCounter = Seekbar.compass.GetComponentInChildren<Text>();
	}

	void Update()
	{
		VRDevices.DetectDevices();

		//NOTE(Kristof): VR specific behaviour
		{
			if (XRSettings.enabled)
			{
				videoController.transform.position = Camera.main.transform.position;

				//NOTE(Lander): enable the highlight in the tutorial mode
				VRDevices.SetControllersTutorialMode(new[] { controllerLeft, controllerRight }, videoController.videoState == VideoController.VideoState.Intro);

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

					if (isOutofView)
					{
						if (angle < 2.5f)
						{
							isOutofView = false;
						}
					}
					else
					{
						if (angle > fov)
						{
							isOutofView = true;
						}
					}

					if (isOutofView)
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

		//NOTE(Kristof): Controller specific behaviour
		{
			if (VRDevices.loadedControllerSet != VRDevices.LoadedControllerSet.NoControllers)
			{
				crosshair.enabled = false;
				crosshairTimer.enabled = false;
			}
			else
			{
				crosshair.enabled = true;
				crosshairTimer.enabled = true;
			}

			if (Input.mouseScrollDelta.y != 0)
			{
				Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - Input.mouseScrollDelta.y * 5, 20, 120);
			}
		}

		Ray ray;
		//NOTE(Kristof): Deciding on which object the Ray will be based on
		{
			Ray cameraRay = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
			Ray controllerRay = new Ray();

			const ulong ulTriggerValue = (ulong)1 << 33;

			if (trackedControllerLeft.controllerState.ulButtonPressed == controllerLeftOldState.ulButtonPressed + ulTriggerValue)
			{
				controllerRay = controllerLeft.GetComponent<Controller>().CastRay();
			}

			if (trackedControllerRight.controllerState.ulButtonPressed == controllerRightOldState.ulButtonPressed + ulTriggerValue)
			{
				controllerRay = controllerRight.GetComponent<Controller>().CastRay();
			}

			controllerLeftOldState = trackedControllerLeft.controllerState;
			controllerRightOldState = trackedControllerRight.controllerState;

			if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
			{
				ray = controllerRay;
			}
			else
			{
				ray = cameraRay;
			}
		}

		interacting = false;

		if (playerState == PlayerState.Watching)
		{
			if (Input.GetKeyDown(KeyCode.Space) && VRDevices.loadedSdk == VRDevices.LoadedSdk.None)
			{
				videoController.TogglePlay();
			}

			//Note(Simon): Interaction with points
			{
				var reversedRay = ray;
				//Note(Simon): Create a reversed raycast to find positions on the sphere with 
				reversedRay.origin = ray.GetPoint(100);
				reversedRay.direction = -ray.direction;

				RaycastHit hit;
				Physics.Raycast(reversedRay, out hit, 100, 1 << LayerMask.NameToLayer("interactionPoints"));

				var left = controllerLeft.GetComponent<Controller>();
				var right = controllerRight.GetComponent<Controller>();

				float forwardAngle;
				//Note(lander): Turn the blips with the correct angle.
				{
					if (left || right)
					{
						forwardAngle = right.compassAttached
							? right.transform.eulerAngles.y
							: left.transform.eulerAngles.y;
					}
					else
					{
						forwardAngle = Seekbar.compass.transform.parent.localEulerAngles.y;
					}
				}

				//NOTE(Kristof): The startpoints are removed in the for loop, so we need to loop in reverse
				for (var i = interactionPoints.Count - 1; i >= 0; i--)
				{
					var point = interactionPoints[i];

					var pointActive = point.startTime <= videoController.currentTime && point.endTime >= videoController.currentTime;
					point.point.SetActive(pointActive);

					var textMesh = point.point.GetComponentInChildren<TextMesh>();

					// Note(Lander): highlight the untouched interaction points
					if (!point.isStartPoint && pointActive && !point.isTouched)
					{
						if (textMesh != null)
						{
							textMesh.color = Color.black;
						}

						var blipAngle = point.point.transform.eulerAngles.y;

						// TODO(Lander): Rely on a start position of a video instead
						var angle = (XRSettings.enabled ? forwardAngle : 90) - blipAngle;

						if (point.blip == null)
						{
							point.blip = Seekbar.CreateBlip(-angle, Instantiate(compassBlipPrefab));
							remainingPoints++;
						}
						point.blip.transform.localEulerAngles = new Vector3(0, 0, angle);
					}
					else
					{
						if (textMesh != null)
						{
							textMesh.color = Color.white;
						}
						if (point.blip != null)
						{
							Destroy(point.blip);
							point.blip = null;
							remainingPoints--;
						}
					}

					if (!point.isStartPoint && point.isTouched)
					{
						point.point.GetComponent<Renderer>().material.color = GRAY;
					}
					blipCounter.text = remainingPoints != 0
						? remainingPoints.ToString()
						: "";

					//NOTE(Lander): current point is hit with the raycast
					if (hit.transform != null && hit.transform.gameObject == point.point)
					{
						//NOTE(Kristof): Interacting with controller
						if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
						{
							//NOTE(Kristof): The controllers only raycast on trigger down
							//NOTE(Kristof): Interacting with StartPoints
							if (point.isStartPoint)
							{
								videoController.videoState = VideoController.VideoState.Watching;
								startPointGroup.SetActive(false);
								Togglecanvasses();
								interactionPoints.RemoveRange(0, 4);
							}
							//NOTE(Kristof): Interacting with InteractionPoints
							else
							{
								point.panel.SetActive(!point.panel.activeSelf);

								if (point.panel.activeSelf)
								{
									activePoints++;
									point.isTouched = true;
								}
								else
								{
									activePoints--;
								}

								videoController.Pause();

								//NOTE(Kristof): Play the video when you deactivate the last point
								if (activePoints == 0 && VideoController.autoResume)
								{
									videoController.TogglePlay();
								}
							}
						}
						//NOTE(Kristof): Interacting without controllers
						else
						{
							interacting = true;

							if (_interactionTimer >= timeToInteract)
							{
								//NOTE(Kristof): Interacting with StartPoints
								if (point.isStartPoint)
								{
									videoController.videoState = VideoController.VideoState.Watching;
									startPointGroup.SetActive(false);
									Togglecanvasses();
								}
								//NOTE(Kristof): Interacting with InteractionPoints
								else
								{
									//NOTE(Kristof): Making a panel active
									if (!point.panel.activeSelf)
									{
										if (VRDevices.loadedSdk > VRDevices.LoadedSdk.None)
										{
											_interactionTimer = -1;
											activePoints++;
										}
										point.isTouched = true;
										point.panel.SetActive(true);
										videoController.Pause();
									}
									//NOTE(Kristof): Making a panel inactive
									//NOTE This only needs to be the done the same frame that the interactiontimer exceeds the timeToInteract, on this frame point.interactionTimer
									//NOTE will always be between timeToInteract and timeToInteract + deltaTime
									else if (timeToInteract < _interactionTimer && _interactionTimer < timeToInteract + Time.deltaTime)
									{
										point.panel.SetActive(false);
										activePoints--;

										_interactionTimer = -1;

										if (activePoints == 0 && VideoController.autoResume)
										{
											videoController.TogglePlay();
										}
									}
								}
							}
						}
					}
					//NOTE(Kristof): Gets executed for the point.panels that the user made active before but are not currently being interacted with (no hit)
					else if (point.panel != null && point.panel.activeSelf)
					{
						//NOTE(Kristof): Disable all panels when using the Seekbar play button to resume play
						if (videoController.playing)
						{
							point.panel.SetActive(false);
							activePoints--;
						}

						//NOTE(Kristof): Video can resume if the user is not using VR
						if (VRDevices.loadedSdk == VRDevices.LoadedSdk.None)
						{
							point.panel.SetActive(false);
							videoController.TogglePlay();
						}
					}
				}
			}
		}

		if (playerState == PlayerState.Opening)
		{
			var panel = indexPanel.GetComponent<IndexPanel>();

			if (panel.answered)
			{
				var metaFilename = Path.Combine(Application.persistentDataPath, Path.Combine(panel.answerVideoId, SaveFile.metaFilename));
				if (OpenFile(metaFilename))
				{
					Destroy(indexPanel);
					playerState = PlayerState.Watching;
					Canvass.modalBackground.SetActive(false);
					if (VRDevices.loadedSdk > VRDevices.LoadedSdk.None)
					{
						EventManager.OnSpace();
						videoPositions.Clear();
					}
				}
				else
				{
					Debug.Log("Couldn't open savefile");
				}
			}
		}

		//NOTE(Kristof): Interaction with UI
		{
			RaycastHit hit;
			Physics.Raycast(ray, out hit, 100, LayerMask.GetMask("UI", "WorldUI"));

			var controllerList = new List<Controller>
			{
				controllerLeft.GetComponent<Controller>(),
				controllerRight.GetComponent<Controller>()
			};

			//NOTE(Kristof): Looping over hittable UI scripts
			foreach (var hittable in hittables)
			{
				if (hittable == null) continue;
				hittable.hitting = false;
				hittable.hovering = false;

				//NOTE(Kristof): Checking for controller hover needs to happen independently of controller interactions
				foreach (var con in controllerList)
				{
					if (con.uiHovering && con.hovered == hittable.gameObject)
					{
						hittable.hovering = true;
					}
				}

				if (hit.transform != null && hit.transform.gameObject == hittable.gameObject)
				{
					//NOTE(Kristof): Interacting with controller
					if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
					{
						hittable.hitting = true;
					}
					//NOTE(Kristof): Interacting without controllers
					else
					{
						interacting = true;
						hittable.hovering = true;
						if (_interactionTimer >= timeToInteract)
						{
							_interactionTimer = -1;
							hittable.hitting = true;
						}
					}
				}
			}
		}

		//NOTE(Kristof): Interaction interactionTimer and Crosshair behaviour
		{
			if (interacting)
			{
				_interactionTimer += Time.deltaTime;
				crosshairTimer.fillAmount = _interactionTimer / timeToInteract;
				crosshair.fillAmount = 1 - (_interactionTimer / timeToInteract);
			}
			else
			{
				_interactionTimer = 0;
				crosshairTimer.fillAmount = 0;
				crosshair.fillAmount = 1;
			}
		}

		//NOTE(Kristof): Turning CameraRig
		{
			var controllers = new[]
			{
				controllerLeft.GetComponent<SteamVR_TrackedObject>(),
				controllerRight.GetComponent<SteamVR_TrackedObject>()
			};

			for (var index = 0; index < controllers.Length; index++)
			{
				var controller = controllers[index];
				if (controller.index > SteamVR_TrackedObject.EIndex.None)
				{
					var device = SteamVR_Controller.Input((int)controller.index);

					switch (VRDevices.loadedControllerSet)
					{
						case VRDevices.LoadedControllerSet.Oculus:
						{
							var touchpad = device.GetAxis();

							if (-0.7f < touchpad.x && touchpad.x < 0.7f)
							{
								cameraRigMovable[index] = true;
							}
							else if (touchpad.x > 0.7f && cameraRigMovable[index])
							{
								cameraRig.transform.localEulerAngles += new Vector3(0, 30, 0);
								cameraRigMovable[index] = false;
							}
							else if (touchpad.x < -0.7f && cameraRigMovable[index])
							{
								cameraRig.transform.localEulerAngles -= new Vector3(0, 30, 0);
								cameraRigMovable[index] = false;
							}

							break;
						}
						case VRDevices.LoadedControllerSet.Vive:
						{
							var touchpad = device.GetAxis();
							if (device.GetPressDown(SteamVR_Controller.ButtonMask.Touchpad))
							{
								if (touchpad.x > 0.7f && cameraRigMovable[index])
								{
									cameraRig.transform.localEulerAngles += new Vector3(0, 30, 0);
									cameraRigMovable[index] = false;
								}
								else if (touchpad.x < -0.7f && cameraRigMovable[index])
								{
									cameraRig.transform.localEulerAngles -= new Vector3(0, 30, 0);
									cameraRigMovable[index] = false;
								}
							}
							else
							{
								cameraRigMovable[index] = true;
							}

							break;
						}
					}
				}

			}
		}
	}

	private bool OpenFile(string path)
	{
		data = SaveFile.OpenFile(path);

		openVideo = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), SaveFile.videoFilename));
		fileLoader.LoadFile(openVideo);

		//NOTE(Kristof): Add the 4 interactionpoints used for the mini-tutorial and starting the video, then load the InteractionPoints
		{
			startPointGroup.SetActive(true);

			foreach (var startPoint in startPoints)
			{
				interactionPoints.Add(new InteractionPointPlayer
				{
					point = startPoint,
					isStartPoint = true,
					startTime = -1,
					endTime = -1,
					isTouched = false
				});

				var content = startPoint.GetComponentInChildren<Text>();
				if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
				{
					content.text = "Aim at the white point below and press the trigger";
				}
				else
				{
					content.text = "Align the central circle with the white point below";
				}
			}
		}

		data.points.Sort((x, y) => x.startTime != y.startTime
										? x.startTime.CompareTo(y.startTime)
										: x.endTime.CompareTo(y.endTime));

		foreach (var point in data.points)
		{
			var newPoint = Instantiate(interactionPointPrefab/*, point.position, Quaternion.identity*/);

			var newInteractionPoint = new InteractionPointPlayer
			{
				startTime = point.startTime,
				endTime = point.endTime,
				title = point.title,
				body = point.body,
				filename = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), point.filename)),
				type = point.type,
				point = newPoint,
				returnRayOrigin = point.returnRayOrigin,
				returnRayDirection = point.returnRayDirection
			};

			// NOTE(Lander): Add file protocol for images, ignore for the rest.
			newInteractionPoint.filename = (newInteractionPoint.type == InteractionType.Image ? "file:///" : "") + newInteractionPoint.filename;

			switch (newInteractionPoint.type)
			{
				case InteractionType.Text:
				{
					var panel = Instantiate(textPanelPrefab);
					panel.GetComponent<TextPanel>().Init(newInteractionPoint.title, newInteractionPoint.body);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Image:
				{
					var panel = Instantiate(imagePanelPrefab);
					panel.GetComponent<ImagePanel>().Init(newInteractionPoint.title, newInteractionPoint.filename, false);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Video:
				{
					var panel = Instantiate(videoPanelPrefab);
					panel.GetComponent<VideoPanel>().Init(newInteractionPoint.title, newInteractionPoint.filename, data.meta.guid.ToString(), false);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.MultipleChoice:
				{
					var panel = Instantiate(multipleChoicePrefab);
					panel.GetComponent<MultipleChoicePanel>().Init(newInteractionPoint.title, newInteractionPoint.body.Split('\f'));
					newInteractionPoint.panel = panel;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			AddInteractionPoint(newInteractionPoint);
		}
		StartCoroutine(UpdatePointPositions());

		return true;
	}

	private void OpenFilePanel()
	{
		indexPanel = Instantiate(indexPanelPrefab);
		indexPanel.GetComponent<IndexPanel>();
		indexPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		playerState = PlayerState.Opening;
	}

	private void AddInteractionPoint(InteractionPointPlayer point)
	{
		point.point.transform.LookAt(Vector3.zero, Vector3.up);
		point.point.transform.RotateAround(point.point.transform.position, point.point.transform.up, 180);

		//NOTE(Simon): Add a number to interaction points
		//TODO(Simon): Make sure these are numbered chronologically
		point.point.transform.GetChild(0).gameObject.SetActive(true);
		point.point.GetComponentInChildren<TextMesh>().text = (++interactionPointCount).ToString();
		point.panel.SetActive(false);
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

	private void Togglecanvasses()
	{
		var seekbarCollider = Canvass.seekbar.gameObject.GetComponent<BoxCollider>();

		Canvass.main.enabled = !Canvass.main.enabled;
		Canvass.seekbar.enabled = !Canvass.seekbar.enabled;
		seekbarCollider.enabled = !seekbarCollider.enabled;
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

	public void BackToBrowser()
	{
		if (videoController.videoState > VideoController.VideoState.Intro)
		{
			Togglecanvasses();
		}
		EventManager.OnSpace();
		projector.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

		if (videoController.playing)
		{
			videoController.TogglePlay();
		}
		videoController.videoState = VideoController.VideoState.Intro;

		for (var j = interactionPoints.Count - 1; j >= 0; j--)
		{
			if (!interactionPoints[j].isStartPoint)
			{
				RemoveInteractionPoint(interactionPoints[j]);
			}
		}
		interactionPoints.Clear();
		interactionPointCount = 0;

		OpenFilePanel();
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
				interactionPoint.panel.transform.position = drawLocation;
			}
		}
	}

	//NOTE(Kristof): We don't want to do this while loading the video, bad performance
	private IEnumerator InstantiateStartPointGroup()
	{
		startPointGroup = Instantiate(startPointGroup);
		//NOTE(Kristof): Wait a frame so the startPointGroup can instantiate properly
		yield return null;
		startPoints.AddRange(GameObject.FindGameObjectsWithTag("StartPoint"));
		startPointGroup.SetActive(false);
	}

	private IEnumerator EnableVr()
	{
		//NOTE(Kristof) If More APIs need to be implemented, add them here
		XRSettings.LoadDeviceByName(new[] { "OpenVR", "None" });

		//NOTE(Kristof): wait one frame to allow the device to be loaded
		yield return null;

		if (XRSettings.loadedDeviceName.Equals("Oculus"))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.Oculus;
			Instantiate(localAvatarPrefab);
			localAvatarPrefab.GetComponent<OvrAvatar>().StartWithControllers = true;
			XRSettings.enabled = true;
		}
		else if (XRSettings.loadedDeviceName.Equals("OpenVR"))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.OpenVr;
			XRSettings.enabled = true;
		}
		else if (XRSettings.loadedDeviceName.Equals(""))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.None;
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
					var nextAngle = 45f + ((i) * 33.75f) + 90f;
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

	public static IEnumerator FadevideoCanvasIn(Transform videoCanvas)
	{
		var group = videoCanvas.GetComponent<CanvasGroup>();

		for (float i = 0; i <= 1; i += Time.deltaTime * 1.5f)
		{
			group.alpha = i;
			yield return null;
		}
		videoCanvas.root.Find("UICanvas").gameObject.SetActive(true);
	}

	public static IEnumerator FadevideoCanvasOut(Transform videoCanvas)
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