using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuBar : MonoBehaviour
{
	public List<Button> menuBarItems;
	public List<GameObject> menuBarPanels;

	public Button debugMenu;

	public void Start()
	{
		Debug.Assert(menuBarItems.Count == menuBarPanels.Count, "Make sure you have a menu bar panel for each menu bar button. Also make sure their ordering is the same");
#if UNITY_EDITOR
		ActivateDebugMenu();
#endif

		for (int i = 0; i < menuBarItems.Count; i++)
		{
			int index = i;
			menuBarItems[i].onClick.AddListener(() => { OnMenuBarItemClick(index); });

			menuBarPanels[i].SetActive(false);

			var buttons = menuBarPanels[i].GetComponentsInChildren<Button>();

			foreach (var button in buttons)
			{
				button.onClick.AddListener(ClosePanels);
			}
		}
	}

	public void OnMenuBarItemClick(int index)
	{
		for (int i = 0; i < menuBarPanels.Count; i++)
		{
			if (i == index)
			{
				menuBarPanels[i].SetActive(!menuBarPanels[index].activeSelf);
			}
			else
			{
				menuBarPanels[i].SetActive(false);
			}
		}
	}

	public void ClosePanels()
	{
		foreach (var p in menuBarPanels)
		{
			p.SetActive(false);
		}
	}

	private void ActivateDebugMenu()
	{
		debugMenu.gameObject.SetActive(true);
	}
}
