using System.Diagnostics;
using System.IO;
using UnityEngine;

public class HelpMenu : MonoBehaviour
{
	//NOTE(Simon): For now this shows the log file in the appropriate explorer-like application on the various OSes
	public void ExportLog()
	{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		var path = Path.Combine(Application.persistentDataPath, "Player.log");
		path = path.Replace('/', '\\');
		Process.Start("explorer.exe", $"/select,\"{path}\"");
#endif

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		var path = "~/Library/Logs/Unity/Player.log"
		Process.Start("open", "-R " + path);
#endif

#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
		var path = Path.Combine(Application.persistentDataPath, "Player.log");
		Process.Start("xdg-open", path);
#endif
	}
}
