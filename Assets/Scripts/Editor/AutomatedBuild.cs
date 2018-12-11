using UnityEditor;
using System.Diagnostics;


public class AutomatedBuild : EditorWindow
{
	[MenuItem("Build/Win64 %&b", false, 1)]
	public static void BuildWin64()
	{
		string branch = GetBranch();
		BuildPlayerOptions options;
		

		string path = "builds/" + branch + "/Editor/";
		options = new BuildPlayerOptions { scenes = new[] { "Assets/Editor.unity" }, locationPathName = path + "VivistaEditor.exe", target = BuildTarget.StandaloneWindows64, options = 0};
		PlayerSettings.virtualRealitySupported = false;
		PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
		PlayerSettings.defaultIsNativeResolution = true;
		PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Enabled;
		PlayerSettings.usePlayerLog = true;
		PlayerSettings.resizableWindow = true;
		BuildPipeline.BuildPlayer(options);

		path = "builds/" + branch + "/Player/";
		options = new BuildPlayerOptions { scenes = new[] { "Assets/Player.unity" }, locationPathName = path + "VivistaPlayer.exe", target = BuildTarget.StandaloneWindows64, options = 0 };
		PlayerSettings.virtualRealitySupported = true;
		BuildPipeline.BuildPlayer(options);

		//TODO(Kristof): zip all in folder and copy zips one higher
	}

	// TODO(Lander): Linux builds, needs testing
	/*
	[MenuItem("Build/Linux")]
	static void BuildLinux()
	{
		var args = System.Environment.GetCommandLineArgs();
		var options = new BuildPlayerOptions { scenes = new string[] { "Assets/Player.unity" }, locationPathName="builds/app", target = BuildTarget.StandaloneLinuxUniversal};
		BuildPipeline.BuildPlayer(options);
	}
	*/

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
}