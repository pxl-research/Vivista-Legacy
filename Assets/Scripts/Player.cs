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
	public Quaternion rotation;
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public float interactionTimer;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

public class Player : MonoBehaviour
{
	private PlayerState playerState;

	private List<InteractionPointPlayer> interactionPoints;
	private FileLoader fileLoader;
	private VideoController videoController;
	private Image crosshair;
	private Image crosshairTimer;

	public GameObject interactionPointPrefab;
	public GameObject indexPanelPrefab;
	public GameObject imagePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject localAvatarPrefab;

	public GameObject controllerLeft;
	public GameObject controllerRight;

	public bool isOutofView;

	private GameObject indexPanel;

	private VRControllerState_t controllerLeftOldState;
	private VRControllerState_t controllerRightOldState;
	private SteamVR_TrackedController trackedControllerLeft;
	private SteamVR_TrackedController trackedControllerRight;

	private float currentSeekbarAngle;
	private string openVideo;
	private int interactionPointCount;

	void Start()
	{
		StartCoroutine(EnableVr());

		trackedControllerLeft = controllerLeft.GetComponent<SteamVR_TrackedController>();
		trackedControllerRight = controllerRight.GetComponent<SteamVR_TrackedController>();

		interactionPoints = new List<InteractionPointPlayer>();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.videoController.GetComponent<VideoController>();
		OpenFilePanel();
		playerState = PlayerState.Opening;
		crosshair = Canvass.main.transform.Find("Crosshair").GetComponent<Image>();
		crosshairTimer = crosshair.transform.Find("CrosshairTimer").GetComponent<Image>();
	}

	void Update()
	{

		VRDevices.DetectDevices();

		if (playerState == PlayerState.Watching)
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}

			if (XRSettings.enabled)
			{
				videoController.transform.position = Camera.main.transform.position;
				Canvass.main.renderMode = RenderMode.ScreenSpaceCamera;

				//NOTE(Kristof): Seekbar rotation is the same as the seekbar's angle on the circle
				//var seekbarAngle = Canvass.seekbar.transform.eulerAngles.y;
				var seekbarAngle = Vector2.SignedAngle(new Vector2(Canvass.seekbar.transform.position.x, Canvass.seekbar.transform.position.z), Vector2.up);

				var fov = Camera.main.fieldOfView;
				//NOTE(Kristof): Camera rotation tells you to which angle on the circle the camera is looking
				var cameraAngle = Camera.main.transform.eulerAngles.y;

				//NOTE(Kristof): Calculate the absolute degree angle from the camera to the seekbar
				var distanceLeft = Mathf.Abs((cameraAngle - seekbarAngle + 360) % 360);
				var distanceRight = Mathf.Abs((cameraAngle - seekbarAngle - 360) % 360);

				var angle = Mathf.Min(distanceLeft, distanceRight);

				//NOTE(Kristof): Rotating the seekbar
				{
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
						var z = 1.8f * Mathf.Sin(radianAngle);

						Canvass.seekbar.transform.position = new Vector3(x, 0, z);
						Canvass.seekbar.transform.eulerAngles = new Vector3(30, newAngle, 0);
					}
				}
			}
			else
			{
				Canvass.main.renderMode = RenderMode.ScreenSpaceOverlay;
			}

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

			Ray ray;
			//NOTE(Kristof): Deciding on which object the Ray will be based on
			{
				Ray cameraRay = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
				Ray controllerRay = new Ray();

				const ulong ulTriggerValue = (ulong)1 << 33;

				if (trackedControllerLeft.controllerState.ulButtonPressed == controllerLeftOldState.ulButtonPressed + ulTriggerValue)
				{
					controllerRay = new Ray(controllerLeft.transform.position, controllerLeft.transform.forward);
				}

				if (trackedControllerRight.controllerState.ulButtonPressed == controllerRightOldState.ulButtonPressed + ulTriggerValue)
				{
					controllerRay = new Ray(controllerRight.transform.position, controllerRight.transform.forward);
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

			//Note(Simon): Create a reversed raycast to find positions on the sphere with
			ray.origin = ray.GetPoint(100);
			ray.direction = -ray.direction;

			//Note(Simon): Interaction with points
			{
				RaycastHit hit;
				Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("interactionPoints"));

				bool interacting = false;
				foreach (var point in interactionPoints)
				{
					const float timeToInteract = 0.75f;

					var pointActive = point.startTime <= videoController.currentTime && point.endTime >= videoController.currentTime;
					point.point.SetActive(pointActive);

					if (hit.transform != null && hit.transform.gameObject == point.point)
					{
						if (VRDevices.loadedControllerSet > VRDevices.LoadedControllerSet.NoControllers)
						{
							point.panel.SetActive(!point.panel.activeSelf);
						}
						else
						{
							interacting = true;
							point.interactionTimer += Time.deltaTime;
							crosshairTimer.fillAmount = point.interactionTimer / timeToInteract;
							crosshair.fillAmount = 1 - (point.interactionTimer / timeToInteract);

							if (point.interactionTimer > timeToInteract)
							{
								point.panel.SetActive(true);
							}
						}
					}
					else if (point.panel.activeSelf)
					{
						if (VRDevices.loadedControllerSet == VRDevices.LoadedControllerSet.NoControllers)
						{
							point.panel.SetActive(false);
						}
					}
					else
					{
						point.interactionTimer = 0;
					}
				}

				if (!interacting)
				{
					crosshairTimer.fillAmount = 0;
					crosshair.fillAmount = 1;
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
				}
				else
				{
					Debug.Log("Couldn't open savefile");
				}
			}
		}
	}

	private bool OpenFile(string path)
	{
		var data = SaveFile.OpenFile(path);

		openVideo = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), SaveFile.videoFilename));
		fileLoader.LoadFile(openVideo);

		for (var j = interactionPoints.Count - 1; j >= 0; j--)
		{
			RemoveInteractionPoint(interactionPoints[j]);
		}

		interactionPoints.Clear();

		data.points.Sort((x, y) => x.startTime != y.startTime
										? x.startTime.CompareTo(y.startTime) 
										: x.endTime.CompareTo(y.endTime));

		foreach (var point in data.points)
		{
			var newPoint = Instantiate(interactionPointPrefab, point.position, Quaternion.identity);
			
			var newInteractionPoint = new InteractionPointPlayer
			{
				startTime = point.startTime,
				endTime = point.endTime,
				title = point.title,
				body = point.body,
				filename = "file:///" + Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), point.filename)),
				type = point.type,
				point = newPoint,
				returnRayOrigin = point.returnRayOrigin,
				returnRayDirection = point.returnRayDirection
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
					panel.GetComponent<ImagePanel>().Init(point.position, newInteractionPoint.title, newInteractionPoint.filename, false);
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

	//NOTE(Simon): This needs to be a coroutine so that we can wait a frame before recalculating point positions. If this were run in the first frame, collider positions would not be up to date yet.
	private IEnumerator UpdatePointPositions()
	{
		//NOTE(Simon): wait one frame
		yield return null;

		foreach (var interactionPoint in interactionPoints)
		{
			var ray = new Ray(interactionPoint.returnRayOrigin, interactionPoint.returnRayDirection);

			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, 100))
			{
				var drawLocation = hit.point;
				interactionPoint.point.transform.position = drawLocation;
			}
		}
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
}
