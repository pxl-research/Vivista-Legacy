using UnityEditor;
using System.Diagnostics;
using System.IO;


public class AutomatedBuild : EditorWindow
{
	[MenuItem("Build/Win64 %&b", false, 1)]
	public static void BuildWin64()
	{
		string branch = GetBranch();
		BuildPlayerOptions options;

		string path = "builds/" + branch + "/EditorWin/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Editor.unity" }, 
			locationPathName = path + "VivistaEditor.exe", 
			target = BuildTarget.StandaloneWindows64, 
			options = 0 
		};
		PlayerSettings.virtualRealitySupported = false;
		PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
		PlayerSettings.defaultIsNativeResolution = true;
		PlayerSettings.usePlayerLog = true;
		PlayerSettings.resizableWindow = true;
		BuildPipeline.BuildPlayer(options);

		path = "builds/" + branch + "/PlayerWin/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Player.unity" }, 
			locationPathName = path + "VivistaPlayer.exe", 
			target = BuildTarget.StandaloneWindows64, 
			options = 0 
		};
		PlayerSettings.virtualRealitySupported = true;
		BuildPipeline.BuildPlayer(options);

		ShowInWindowsExplorer("builds/" + branch);
	}

	[MenuItem("Build/OSX")]
	static void BuildOSX()
	{
		string branch = GetBranch();
		BuildPlayerOptions options;

		string path = "builds/" + branch + "/EditorOSX/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Editor.unity" }, 
			locationPathName = path + "VivistaEditor.exe", 
			target = BuildTarget.StandaloneOSX, 
			options = 0 
		};
		PlayerSettings.virtualRealitySupported = false;
		PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
		PlayerSettings.defaultIsNativeResolution = true;
		PlayerSettings.usePlayerLog = true;
		PlayerSettings.resizableWindow = true;
		BuildPipeline.BuildPlayer(options);

		path = "builds/" + branch + "/PlayerOSX/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Player.unity" }, 
			locationPathName = path + "VivistaPlayer.exe", 
			target = BuildTarget.StandaloneOSX, 
			options = 0 
		};
		PlayerSettings.virtualRealitySupported = false;
		BuildPipeline.BuildPlayer(options);

		ShowInWindowsExplorer("builds/" + branch);
	}

	public static string GetBranch()
	{
		var proc = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "git",
				Arguments = "rev-parse --abbrev-ref HEAD",
				RedirectStandardOutput = true,
				UseShellExecute = false
			}
		};
		proc.Start();
		return proc.StandardOutput.ReadToEnd().Trim();
	}

	public static void ShowInWindowsExplorer(string folder)
	{
		var cleaned = Path.GetFullPath(folder);
		UnityEngine.Debug.Log(cleaned);
		var proc = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = $"/select,\"{cleaned}\"",
			}
		};
		proc.Start();
	}
}