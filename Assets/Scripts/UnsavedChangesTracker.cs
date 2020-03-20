using UnityEngine;

public class UnsavedChangesTracker : MonoBehaviour
{
	public static UnsavedChangesTracker Instance;

	public bool unsavedChanges;
	private bool forceQuit;
	public GameObject unsavedChangesPanelPrefab;
	public GameObject unsavedChangesNotification;

	// Start is called before the first frame update
	void Start()
	{
		Instance = this;

		Application.wantsToQuit += OnWantsToQuit;
	}

	// Update is called once per frame
	void Update()
	{
		unsavedChangesNotification.SetActive(unsavedChanges);
	}

	public bool OnWantsToQuit()
	{
		if (forceQuit)
		{
			return true;
		}

		if (unsavedChanges)
		{
			var go = Instantiate(unsavedChangesPanelPrefab);
			go.transform.SetParent(Canvass.main.transform, false);
			var panel = go.GetComponent<UnsavedChangesPanel>();
			Canvass.modalBackground.SetActive(true);

			panel.OnSave += () =>
			{
				if (Editor.Instance.SaveToFile(false))
				{
					forceQuit = true;
					Application.Quit();
				}
				else
				{
					Debug.LogError("Something went wrong while saving the file");
				}
			};
			panel.OnDiscard += () =>
			{
				forceQuit = true;
				Application.Quit();
			};
			panel.OnCancel += () =>
			{
				Destroy(panel.gameObject);
				Canvass.modalBackground.SetActive(false);
			};

			return false;
		}

		return true;
	}

}
