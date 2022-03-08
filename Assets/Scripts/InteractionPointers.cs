using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class InteractionPointers : MonoBehaviour
{
	public static InteractionPointers Instance;

	public GameObject arrowPrefab;
	[Range(0, 1)]
	public float boundsSize2D;
	[Range(0, 1)]
	public float boundsSizeVR;

	private GameObjectPool arrowPool;

	private Plane[] cameraPlanes = new Plane[6];

	private bool shouldRender;

	private void Awake()
	{
		Instance = this;
		shouldRender = true;

		arrowPool = new GameObjectPool(arrowPrefab, transform);
	}

	private void Update()
	{
		var activeInteractions = Player.Instance.GetShownInteractionPoints();

		if (Player.playerState != PlayerState.Watching)
		{
			return;
		}

		arrowPool.EnsureActiveCount(activeInteractions.Count);

		for (int i = 0; i < activeInteractions.Count; i++)
		{
			var point = activeInteractions[i];

			GeometryUtility.CalculateFrustumPlanes(Camera.main, cameraPlanes);
			bool isOffscreen = !GeometryUtility.TestPlanesAABB(cameraPlanes, point.point.GetComponent<Renderer>().bounds);
			arrowPool[i].SetActive(isOffscreen && shouldRender);

			float animTime = MathHelper.SmoothPingPong(Time.time, .6f);
			var centre = new Vector3(Screen.width, Screen.height) / 2f;
 			var boundsSize = XRSettings.isDeviceActive ? boundsSizeVR : boundsSize2D;
			var bounds = centre * (boundsSize - 0.03f * animTime);

			if (isOffscreen)
			{
				var pos = Camera.main.WorldToScreenPoint(point.position);
				pos -= centre;

				//NOTE(Simon): If behind us, invert pos
				float zDir = Mathf.Sign(pos.z);
				pos *= zDir;

				float angle = Mathf.Atan2(pos.y, pos.x);
				float slope = Mathf.Tan(angle);
				angle *= Mathf.Rad2Deg;
				angle -= 90;

				//NOTE(Simon): clamp to left/right of screen
				float xDir = Mathf.Sign(pos.x);
				pos = new Vector3(xDir * bounds.x, xDir * bounds.x * slope, 0);

				if (pos.y > bounds.y)
				{
					//NOTE(Simon): Keep the y screen position to the maximum y bounds and find the x screen position using x = y/m.
					pos = new Vector3(bounds.y / slope, bounds.y, 0);
				}
				else if (pos.y < -bounds.y)
				{
					pos = new Vector3(-bounds.y / slope, -bounds.y, 0);
				}

				var rect = arrowPool[i].GetComponent<RectTransform>();
				rect.localPosition = pos;
				rect.localRotation = Quaternion.Euler(0, 0, angle);
				rect.localScale = Vector3.one * (0.85f + animTime * .3f);

				float alpha = point.isSeen ? 0.4f : .95f;
				var color = TagManager.Instance.GetTagColorById(point.tagId);
				color.a = alpha;
				arrowPool[i].GetComponent<Image>().color = color;
			}
		}
	}

	public void ShouldRender(bool shouldRender)
	{
		this.shouldRender = shouldRender;
	}
}
