using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FindAreaPanelSphere : MonoBehaviour
{
	public Text title;
	public Button startButton;
	public Text result;
	private List<Area> areas;
	private List<AreaRenderer> areaRenderers = new List<AreaRenderer>();

	public GameObject areaRendererPrefab;

	private Controller[] controllers;
	private List<Ray> rays;

	private bool isFindingArea;
	private bool completed;

	private int id;
	private int wrongAnswersTried;

	void Awake()
	{
		rays = new List<Ray>();
	}

	void Update()
	{
		if (isFindingArea)
		{
			rays.Clear();

			if (Input.GetMouseButtonDown(0))
			{
				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				//NOTE(Simon): Add both directions, because winding order determines from which direction a mesh can collide
				rays.Add(ray);
				rays.Add(ray.ReverseRay());
			}

			for (int i = 0; i < controllers.Length; i++)
			{
				if (controllers[i].triggerPressed)
				{
					var ray = controllers[i].CastRay();
					//NOTE(Simon): Add both directions, because winding order determines from which direction a mesh can collide
					rays.Add(ray);
					rays.Add(ray.ReverseRay());
				}
			}

			GameObject target = null;
			for (int i = 0; i < rays.Count; i++)
			{
				if (Physics.Raycast(rays[i], out var hit, Mathf.Infinity, LayerMask.GetMask("Area")))
				{
					target = hit.transform.gameObject;
					break;
				}
			}

			if (target != null)
			{
				int selectedAnswer = -1;
				for (int i = 0; i < areaRenderers.Count; i++)
				{
					var areaRenderer = areaRenderers[i].GetComponent<AreaRenderer>();
					areaRenderer.EnableRenderer();
					areaRenderer.DisableCollider();

					if (areaRenderer.gameObject == target.gameObject)
					{
						selectedAnswer = i;
					}
				}

				Player.Instance.UnsuspendInteractionPoint();
				VideoResultTracker.RegisterQuestionResult(new QuestionResult
				{
					interactionId = id,
					type = InteractionType.FindArea,
					answerChosen = selectedAnswer,
					correctAnswer = selectedAnswer,
					wrongAnswersTried = wrongAnswersTried
				});

				startButton.gameObject.SetActive(false);
				result.gameObject.SetActive(true);

				isFindingArea = false;
				completed = true;
			}
			else
			{
				wrongAnswersTried++;
			}
		}
	}

	private void OnEnable()
	{
		if (completed)
		{
			for (int i = 0; i < areaRenderers.Count; i++)
			{
				areaRenderers[i].EnableRenderer();
			}
		}
	}

	private void OnDisable()
	{
		for (int i = 0; i < areaRenderers.Count; i++)
		{
			areaRenderers[i].DisableRenderer();
		}
	}

	public void Init(string newTitle, List<Area> newAreas, int id)
	{
		title.text = newTitle;
		areas = newAreas;
		this.id = id;

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
