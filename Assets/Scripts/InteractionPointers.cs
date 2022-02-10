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
		var activeInteractions = Player.Instance.GetActiveInteractionPoints();

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

			float animTime = MathHelper.SmoothPingPong(Time.time, .75f);
			var centre = new Vector3(Screen.width, Screen.height) / 2f;
			var bounds = centre * (0.9f - 0.03f * animTime);

			if (isOffscreen)
			{
				var pos = Camera.main.WorldToScreenPoint(point.position);
				pos -= centre;

				if (pos.z < 0)
				{
					pos *= -1;
				}

				// Angle between the x-axis (bottom of screen) and a vector starting at zero(bottom-left corner of screen) and terminating at screenPosition.
				float angle = Mathf.Atan2(pos.y, pos.x);
				// Slope of the line starting from zero and terminating at screenPosition.
				float slope = Mathf.Tan(angle);

				if (pos.x > 0)
				{
					// Keep the x screen position to the maximum x bounds and
					// find the y screen position using y = mx.
					pos = new Vector3(bounds.x, bounds.x * slope, 0);
				}
				else
				{
					pos = new Vector3(-bounds.x, -bounds.x * slope, 0);
				}

				if (pos.y > bounds.y)
				{
					// Keep the y screen position to the maximum y bounds and
					// find the x screen position using x = y/m.
					pos = new Vector3(bounds.y / slope, bounds.y, 0);
				}
				else if (pos.y < -bounds.y)
				{
					pos = new Vector3(-bounds.y / slope, -bounds.y, 0);
				}

				var rect = activeArrows[i].GetComponent<RectTransform>();
				rect.localPosition = pos;
				rect.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg - 90f);

				activeArrows[i].GetComponent<Image>().color = TagManager.Instance.GetTagColorById(point.tagId);
			}
		}
	}

	public void ShouldRender(bool shouldRender)
	{
		this.shouldRender = shouldRender;
	}
}
