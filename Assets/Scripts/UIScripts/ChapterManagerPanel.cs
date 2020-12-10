using UnityEngine;
using UnityEngine.UI;

public class ChapterManagerPanel : MonoBehaviour
{
	public GameObject chapterItemPrefab;

	public GameObject chapterItemHolder;

	public InputField newName;
	public InputField newDescription;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	void Start()
	{
		newName.onValueChanged.AddListener(OnEditName);
		newDescription.onValueChanged.AddListener(OnEditDescription);

		var tags = ChapterManager.Instance.chapters;

		for (int i = 0; i < tags.Count; i++)
		{
			AddChapterItem(tags[i].name, tags[i].description, tags[i].time);
		}
	}

	public void OnAdd()
	{
		var name = newName.text;
		var description = newDescription.text;

		var success = ChapterManager.Instance.AddChapter(name, description);

		if (success)
		{
			newName.text = "";
			newDescription.text = "";

			AddChapterItem(name, description, 0);
		}
		else
		{
			newName.image.color = errorColor;
		}
	}

	public void AddChapterItem(string name, string description, float time)
	{
		var chapterGo = Instantiate(chapterItemPrefab);
		var chapterItem = chapterGo.GetComponent<ChapterItem>();
		chapterGo.transform.SetParent(chapterItemHolder.transform);
		chapterItem.Init(name, description, time);
		chapterItem.deleteButton.onClick.AddListener(() => OnRemove(chapterGo, name));
	}

	public void OnRemove(GameObject chapterGo, string name)
	{
		ChapterManager.Instance.RemoveChapter(name);
		Destroy(chapterGo);
	}

	public void OnEditName(string _)
	{
		newName.image.color = Color.white;
	}

	public void OnEditDescription(string _)
	{
		newDescription.image.color = Color.white;
	}

	public void Close()
	{
		Destroy(gameObject);
	}
}
