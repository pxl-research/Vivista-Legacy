using UnityEngine;

public class UnsavedChangesTracker : MonoBehaviour
{
	public static UnsavedChangesTracker Instance;

	public bool unsavedChanges;
	private bool forceQuit;
	public GameObject unsavedChangesPanelPrefab;
	public GameObject unsavedChangesNotification;

	void Start()
	{
		Instance = this;

		Application.wantsToQuit += OnWantsToQuit;
	}

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
				if (Editor.Instance.SaveProject(false))
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
