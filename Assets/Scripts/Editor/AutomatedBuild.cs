using System;
using System.Collections;
using UnityEditor;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Unity.EditorCoroutines.Editor;


public class AutomatedBuild : EditorWindow
{
	private static string branch;
	private static string lastTag;
	private static bool hasChanges;
	private static bool isInnoSetupAvailable;
	private static bool isCodeSigningScriptAvailable;
	private static bool isCodeSigningToolAvailable;
	private static EditorCoroutine periodicUpdateRoutine;

	private static string innoSetupLocation = Environment.ExpandEnvironmentVariables("%LocalAppData%\\Programs\\Inno Setup 6\\ISCC.exe");
	private static string codeSigningScriptLocation = Environment.ExpandEnvironmentVariables("%userprofile%\\CodeSigning\\vivista.bat");
	private static string codeSigningToolLocation = "C:\\Program Files (x86)\\Microsoft SDKs\\ClickOnce\\SignTool\\signtool.exe";

	private static AutomatedBuild window;
	private static AutomatedBuild instance;

	string newTagNumber;

	private static ProcessStartInfo defaultProcess = new ProcessStartInfo
	{
		RedirectStandardOutput = true,
		UseShellExecute = false,
		CreateNoWindow = true,
	};

	public AutomatedBuild()
	{
		if (instance != null && window != null)
		{
			window.Close();
		}

		instance = this;
	}

	[MenuItem("Build/Win64 %&b", false, 1)]
	public static void Init()
	{
		if (window != null)
		{
			window.Close();
		}

		UpdateGitInfo();

		window = CreateInstance<AutomatedBuild>();
		window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 155);
		window.Repaint();
		window.ShowPopup();
		if (periodicUpdateRoutine == null)
		{
			periodicUpdateRoutine = EditorCoroutineUtility.StartCoroutine(instance.PeriodicUpdateGitInfo(), instance);
		}
	}

	public void OnGUI()
	{
		var margin = new RectOffset(10, 10, 10, 10);

		GUILayout.BeginVertical(new GUIStyle {margin = margin});
		{
			if (branch == "master")
			{
				GUI.contentColor = Color.green;
				EditorGUILayout.LabelField("Branch is master.", EditorStyles.wordWrappedLabel);
			}
			else
			{
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField($"Branch is {branch}. Official build only allowed on master.", EditorStyles.wordWrappedLabel);
			}

			GUILayout.Space(10);

			if (hasChanges)
			{
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField("There are uncommitted changes. Official build not allowed.", EditorStyles.wordWrappedLabel);
			}
			else
			{
				GUI.contentColor = Color.green;
				EditorGUILayout.LabelField("There are no uncommitted changes", EditorStyles.wordWrappedLabel);
			}

			if (!isInnoSetupAvailable)
			{
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField($"Inno Setup 6 not installed at {innoSetupLocation}. Official build not allowed.", EditorStyles.wordWrappedLabel);
			}

			if (!isCodeSigningScriptAvailable)
			{
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField($"Code signing script was not found at {codeSigningScriptLocation}. Official build not allowed.", EditorStyles.wordWrappedLabel);
			}

			if (!isCodeSigningToolAvailable)
			{
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField($"Code signing tool was not found at {codeSigningToolLocation}. Official build not allowed.", EditorStyles.wordWrappedLabel);
			}

			GUI.contentColor = Color.white;
			GUILayout.Space(10);
			EditorGUILayout.LabelField($"Last tag number: {lastTag}", EditorStyles.wordWrappedLabel);
			GUILayout.Space(20);

			bool hasNewTag = !string.IsNullOrEmpty(newTagNumber);
			if (!hasNewTag)
			{
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField("No new tag number filled in. Official build not allowed", EditorStyles.wordWrappedLabel);
				GUI.contentColor = Color.white;
			}

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("New tag number", GUILayout.Width(100));
				newTagNumber = GUILayout.TextField(newTagNumber);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(20);

			GUILayout.BeginHorizontal();
			{
				//NOTE(Simon): Perform local build
				if (GUILayout.Button("Local build", GUILayout.Height(30)))
				{
					BuildWin64();

					ShowInWindowsExplorer("builds/" + branch);
					Close();
				}

				GUI.enabled = branch == "master" && !hasChanges && hasNewTag;
				//NOTE(Simon): Perform official build
				if (GUILayout.Button("Official build", GUILayout.Height(30)))
				{
					WriteVersionNumber(newTagNumber);
					CreateNewGitTag(newTagNumber);
					BuildWin64();
					BuildInstallers();
					SignInstallers();
					RenameInstallers(newTagNumber);

					ShowInWindowsExplorer("builds/installers" );
					Close();
				}

				GUI.enabled = true;

				GUILayout.Space(20);

				if (GUILayout.Button("Cancel", GUILayout.Height(30)))
				{
					if (periodicUpdateRoutine != null)
					{
						EditorCoroutineUtility.StopCoroutine(periodicUpdateRoutine);
					}

					Close();
					window = null; 
				}
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();

		if (Event.current.type == EventType.Repaint && window != null)
		{
			var totalSize = GUILayoutUtility.GetLastRect();
			var pos = window.position;
			pos.height = totalSize.height + margin.top + margin.bottom;
			window.position = pos;
		}
	}

	public static void BuildWin64()
	{
		BuildPlayerOptions options;

		string path = $"builds/{branch}/EditorWin/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Editor.unity" }, 
			locationPathName = path + "VivistaEditor.exe", 
			target = BuildTarget.StandaloneWindows64, 
			options = 0,
		};
		PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
		PlayerSettings.defaultIsNativeResolution = true;
		PlayerSettings.usePlayerLog = true;
		PlayerSettings.resizableWindow = true;
		BuildPipeline.BuildPlayer(options);

		path = $"builds/{branch}/PlayerWin/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Player.unity" }, 
			locationPathName = path + "VivistaPlayer.exe", 
			target = BuildTarget.StandaloneWindows64, 
			options = 0 
		};
		BuildPipeline.BuildPlayer(options);
	}

	private static void BuildInstallers()
	{
		var proc1 = new Process { StartInfo = defaultProcess };
		proc1.StartInfo.FileName = innoSetupLocation;
		proc1.StartInfo.Arguments = "/Q " + Path.Combine(Application.dataPath, "..", "InstallerScriptEditor.iss");
		proc1.Start();

		var proc2 = new Process { StartInfo = defaultProcess };
		proc2.StartInfo.FileName = innoSetupLocation;
		proc2.StartInfo.Arguments = "/Q " + Path.Combine(Application.dataPath, "..", "InstallerScriptPlayer.iss");
		proc2.Start();

		proc1.WaitForExit();
		proc2.WaitForExit();
	}

	private static void SignInstallers()
	{
		var proc = Process.Start(codeSigningScriptLocation);
		proc.WaitForExit();
	}

	private static void RenameInstallers(string versionNumber)
	{
		string cleanNumber = versionNumber;
		if (versionNumber.StartsWith("v"))
		{
			cleanNumber = cleanNumber.Substring(1);
		}

		var path = Path.GetFullPath("builds/installers");
		File.Move(Path.Combine(path, "VivistaEditor.exe"), Path.Combine(path, $"VivistaEditor-{cleanNumber}.exe"));
		File.Move(Path.Combine(path, "VivistaPlayer.exe"), Path.Combine(path, $"VivistaPlayer-{cleanNumber}.exe"));
	}

	[MenuItem("Build/OSX")]
	static void BuildOSX()
	{
		BuildPlayerOptions options;

		string path = "builds/" + branch + "/EditorOSX/";
		options = new BuildPlayerOptions 
		{ 
			scenes = new[] { "Assets/Editor.unity" }, 
			locationPathName = path + "VivistaEditor.exe", 
			target = BuildTarget.StandaloneOSX, 
			options = 0 
		};
		PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
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
		BuildPipeline.BuildPlayer(options);
	}

	public static string GetBranch()
	{
		var proc = new Process { StartInfo = defaultProcess };
		proc.StartInfo.FileName = "git";
		proc.StartInfo.Arguments = "rev-parse --abbrev-ref HEAD";
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

	public static string GetLastTag()
	{
		var proc = new Process { StartInfo = defaultProcess };
		proc.StartInfo.FileName = "git";
		proc.StartInfo.Arguments = "rev-list --tags --max-count=1";
		proc.Start();
		string result = proc.StandardOutput.ReadToEnd().Trim();

		proc.StartInfo.Arguments = $"describe --tags {result}";
		proc.Start();
		result = proc.StandardOutput.ReadToEnd().Trim();
		return result;
	}

	public static bool HasChanges()
	{
		var proc = new Process { StartInfo = defaultProcess };
		proc.StartInfo.FileName = "git";
		proc.StartInfo.Arguments = "update-index --refresh";
		proc.Start();
		proc.WaitForExit();

		proc.StartInfo.Arguments = "diff-index --quiet HEAD --";
		proc.Start();
		proc.WaitForExit();
		return proc.ExitCode > 0;
	}

	public static void CreateNewGitTag(string tag)
	{
		var proc = new Process { StartInfo = defaultProcess };
		proc.StartInfo.FileName = "git";
		proc.StartInfo.Arguments = $"commit -a -m {tag}";
		proc.Start();
		proc.WaitForExit();

		proc.StartInfo.Arguments = $"tag {tag}";
		proc.Start();
		proc.WaitForExit();
	}

	public static void WriteVersionNumber(string number)
	{
		string cleanNumber = number;
		if (number.StartsWith("v"))
		{
			cleanNumber = cleanNumber.Substring(1);
		}

		File.WriteAllText(Path.Combine(Application.dataPath, "..", "version.iss"), $"#define MyAppVersion \"{cleanNumber}\"");

		File.WriteAllText(Path.Combine(Application.dataPath, "version.txt"), cleanNumber);
	}

	private static void UpdateGitInfo()
	{
		branch = GetBranch();
		lastTag = GetLastTag();
		hasChanges = HasChanges();
		isInnoSetupAvailable = IsInnoSetupAvailable();
		isCodeSigningScriptAvailable = IsCodeSigningScriptAvailable();
		isCodeSigningToolAvailable = IsCodeSigningToolAvailable();
	}

	private IEnumerator PeriodicUpdateGitInfo()
	{
		while (true)
		{
			yield return new EditorWaitForSeconds(2);
			UpdateGitInfo();
			Repaint();
		}
	}

	private static bool IsInnoSetupAvailable()
	{
		return File.Exists(innoSetupLocation);
	}

	private static bool IsCodeSigningToolAvailable()
	{
		return File.Exists(codeSigningToolLocation);
	}

	private static bool IsCodeSigningScriptAvailable()
	{
		return File.Exists(codeSigningScriptLocation);
	}
}