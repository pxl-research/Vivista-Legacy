using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class MultipleChoiceAreaPanelSphere : MonoBehaviour
{
	public Text title;
	public Button startButton;
	public Text result;

	private List<Area> areas;
	private int correctIndex;

	public GameObject areaRendererPrefab;
	private List<AreaRenderer> areaRenderers = new List<AreaRenderer>();

	public Color incorrectBackground;
	public Color incorrectOutline;

	private bool isFindingArea;
	private bool completed;

	private Controller[] controllers;
	private List<Ray> clickRays = new List<Ray>();

	void Update()
	{
		if (isFindingArea)
		{
			clickRays.Clear();

			if (!XRSettings.isDeviceActive)
			{

				if (Input.GetMouseButtonDown(0))
				{
					//NOTE(Simon): Add both directions, because winding order determines from which direction a mesh can collide
					var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					clickRays.Add(ray);
					clickRays.Add(ray.ReverseRay());
				}
			}
			else
			{
				for (int i = 0; i < controllers.Length; i++)
				{
					if (controllers[i].triggerPressed)
					{
						var ray = controllers[i].CastRay();
						//NOTE(Simon): Add both directions, because winding order determines from which direction a mesh can collide
						clickRays.Add(ray);
						clickRays.Add(ray.ReverseRay());
					}
				}
			}

			AreaRenderer clickTarget = null;
			for (int i = 0; i < clickRays.Count; i++)
			{
				if (Physics.Raycast(clickRays[i], out var hit, Mathf.Infinity, LayerMask.GetMask("Area")))
				{
					clickTarget = hit.transform.GetComponent<AreaRenderer>();
					break;
				}
			}

			if (clickTarget != null)
			{
				for (int i = 0; i < areaRenderers.Count; i++)
				{
					var areaRenderer = areaRenderers[i].GetComponent<AreaRenderer>();
					areaRenderer.EnableRenderer();
					areaRenderer.DisableCollider();

					if (i != correct)
					{
						areaRenderer.SetColor(incorrectBackground, incorrectOutline);
					}
				}

				Player.Instance.UnsuspendInteractionPoint();

				startButton.gameObject.SetActive(false);
				result.gameObject.SetActive(true);

				isFindingArea = false;
				completed = true;
			}
		}
	}

	private void OnEnable()
	{
		for (int i = 0; i < areaRenderers.Count; i++)
		{
			areaRenderers[i].EnableRenderer();
		}
	}

	private void OnDisable()
	{
		for (int i = 0; i < areaRenderers.Count; i++)
		{
			areaRenderers[i].DisableRenderer();
		}
	}

	public void Init(string newTitle, List<Area> newAreas, int newCorrect)
	{
		correctIndex = newCorrect;
		title.text = newTitle;
		areas = newAreas;

		foreach (var area in areas)
		{
			var areaRenderer = Instantiate(areaRendererPrefab).GetComponent<AreaRenderer>();
			areaRenderers.Add(areaRenderer);

			areaRenderer.SetVertices(area.vertices);
			areaRenderer.DisableRenderer();
			areaRenderer.EnableCollider();

			areaRenderer.gameObject.SetActive(false);
		}
	}

	public void OnStartFindArea()
	{
		for (int i = 0; i < areaRenderers.Count; i++)
		{
			areaRenderers[i].gameObject.SetActive(true);
		}

		controllers = Player.Instance.GetControllers();
		Player.Instance.SuspendInteractionPoint();

		isFindingArea = true;
	}
}
