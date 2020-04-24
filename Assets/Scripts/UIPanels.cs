using UnityEngine;

public class UIPanels : MonoBehaviour
{
	public static UIPanels Instance { get; private set; }

	//NOTE(Simon): Only allowed to add to this list if it is a UI panel with its own script
	public ExplorerPanel explorerPanel;
	public ImportPanel importPanel;

	private void Awake()
	{
		Instance = this;
	}
}
