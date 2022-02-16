using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionPointers : MonoBehaviour
{
	public static InteractionPointers Instance;

	public GameObject arrowPrefab;

	private List<GameObject> activeArrows = new List<GameObject>();
	private List<GameObject> inactiveArrows = new List<GameObject>();

	private Plane[] cameraPlanes = new Plane[6];

	private bool shouldRender;

	private void Awake()
	{
		Instance = this;
		shouldRender = true;
	}

	private void Update()
	{
		var activeInteractions = Player.Instance.GetShownInteractionPoints();

		if (Player.playerState != PlayerState.Watching)
		{
			return;
		}

		while (activeInteractions.Count > activeArrows.Count)
		{
			if (inactiveArrows.Count > 0)
			{
				var arrow = inactiveArrows[inactiveArrows.Count - 1];
				inactiveArrows.RemoveAt(inactiveArrows.Count - 1);
				arrow.SetActive(true);
				activeArrows.Add(arrow);
			}
			else
			{
				var arrow = Instantiate(arrowPrefab, transform, false);
				activeArrows.Add(arrow);
			}
		}

		while (activeInteractions.Count < activeArrows.Count)
		{
			var arrow = activeArrows[activeArrows.Count - 1];
			activeArrows.RemoveAt(activeArrows.Count - 1);
			arrow.SetActive(false);
			inactiveArrows.Add(arrow);
		}

		for (int i = 0; i < activeInteractions.Count; i++)
		{
			var point = activeInteractions[i];

			GeometryUtility.CalculateFrustumPlanes(Camera.main, cameraPlanes);
			bool isOffscreen = !GeometryUtility.TestPlanesAABB(cameraPlanes, point.point.GetComponent<Renderer>().bounds);
			activeArrows[i].SetActive(isOffscreen && shouldRender);

			float animTime = MathHelper.SmoothPingPong(Time.time, .6f);
			var centre = new Vector3(Screen.width, Screen.height) / 2f;
			var bounds = centre * (0.9f - 0.03f * animTime);

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

				var rect = activeArrows[i].GetComponent<RectTransform>();
				rect.localPosition = pos;
				rect.localRotation = Quaternion.Euler(0, 0, angle);
				rect.localScale = Vector3.one * (0.85f + animTime * .3f);

				activeArrows[i].GetComponent<Image>().color = TagManager.Instance.GetTagColorById(point.tagId);
			}
		}
	}

	public void ShouldRender(bool shouldRender)
	{
		this.shouldRender = shouldRender;
	}
}
