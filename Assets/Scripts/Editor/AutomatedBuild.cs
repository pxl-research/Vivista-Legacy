using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public class AutomatedBuild : MonoBehaviour
{

	[MenuItem("Build/Win64")]
	static void BuildWin64()
	{
		//var args = System.Environment.GetCommandLineArgs();
		var branch = getBranch();
		string path;

		path = "builds/player-" + branch;
		var options = new BuildPlayerOptions { scenes = new string[] { "Assets/Player.unity" }, locationPathName = path + "/360_Player.exe", target = BuildTarget.StandaloneWindows64 };
		BuildPipeline.BuildPlayer(options);

		path = "builds/editor-" + branch;
		options = new BuildPlayerOptions { scenes = new string[] { "Assets/Editor.unity" }, locationPathName = path + "/360_Editor.exe", target = BuildTarget.StandaloneWindows64 };
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

	static string getBranch()
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