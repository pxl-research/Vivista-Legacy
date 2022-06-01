using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class Hittable : MonoBehaviour
{
	public UnityEvent onHit;
	public UnityEvent onHitDown;
	public UnityEvent onHitUp;
	public UnityEvent onHoverStart;
	public UnityEvent onHoverStay;
	public UnityEvent onHoverEnd;

	public RawImage image;
	public Color hoverColor;
	private Color originalColor;

	public bool hitting;
	public bool hovering;

	private bool oldHitting;
	private bool oldHovering;

	private static List<Hittable> hittables = new List<Hittable>();

	void Start()
	{
		if (!SceneManager.GetActiveScene().name.Equals("Editor"))
		{
			hittables.Add(this);
		}
	}

	void Update()
	{
		if (oldHitting && hitting)
		{
			onHit.Invoke();
		}

		if (!oldHitting && hitting)
		{
			onHitDown.Invoke();
		}

		if (oldHitting & !hitting)
		{
			onHitUp.Invoke();
		}

		if (!oldHovering && hovering)
		{
			if (image)
			{
				originalColor = image.color;
				image.color = hoverColor;
			}

			onHoverStart.Invoke();
		}

		if (oldHovering && hovering)
		{
			onHoverStay.Invoke();
		}

		if (oldHovering && !hovering)
		{
			if (image)
			{
				image.color = originalColor;
			}
			onHoverEnd.Invoke();
		}

		oldHitting = hitting;
		oldHovering = hovering;
	}

	void OnDestroy()
	{
		// NOTE(Lander): Remove the items only when used in Player 
		if (hittables != null)
		{
			hittables.Remove(this);
		}
	}

	public static void UpdateAllHittables(Controller[] controllers, List<KeyValuePair<Ray, bool>> interactionpointRays)
	{
		//NOTE(Simon): Reset all hittables
		foreach (var hittable in hittables)
		{
			if (hittable == null)
			{
				continue;
			}

			//NOTE(Jitse): Check if a hittable is being held down
			if (!(controllers[0].triggerDown || controllers[1].triggerDown))
			{
				hittable.hitting = false;
			}

			hittable.hovering = false;
		}

		//NOTE(Simon): Set hitting and hovering in hittables
		foreach (var ray in interactionpointRays)
		{
			Physics.Raycast(ray.Key, out var hit, 100, LayerMask.GetMask("UI", "WorldUI"));

			if (hit.transform != null)
			{
				var hittable = hit.transform.GetComponent<Hittable>();
				if (hittable != null)
				{
					if (ray.Value)
					{
						hittable.hitting = true;
					}
					hittable.hovering = true;
				}
			}
		}
	}
}
