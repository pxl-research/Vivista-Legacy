using System;
using System.IO;
using UnityEngine;

public static class Config
{
	[Serializable]
	private class ConfigData
	{
		public float mainVideoVolume;
		public float videoInteractionVolume;
		public float audioInteractionVolume;
	}

	public static float MainVideoVolume
	{
		get => data.mainVideoVolume;
		set
		{
			data.mainVideoVolume = Mathf.Clamp01(value);
			SaveConfig();
		}
	}

	public static float VideoInteractionVolume
	{
		get => data.videoInteractionVolume;
		set
		{
			data.videoInteractionVolume = Mathf.Clamp01(value);
			SaveConfig();
		}
	}

	public static float AudioInteractionVolume
	{
		get => data.audioInteractionVolume;
		set
		{
			data.audioInteractionVolume = Mathf.Clamp01(value);
			SaveConfig();
		}
	}

	public static string mainVideoMixerChannelName = "VideoVolume";
	public static string audioInteractionMixerChannelName = "AudioInteractionVolume";
	public static string videoInteractionMixerChannelName = "VideoInteractionVolume";

	private static ConfigData data;

	static Config()
	{
		LoadConfig();
	}

	public static void LoadConfig()
	{
		var path = Path.Combine(Application.persistentDataPath, ".config");
		if (File.Exists(path))
		{
			string raw = File.ReadAllText(path);
			data = JsonUtility.FromJson<ConfigData>(raw);
		}
		else
		{
			File.WriteAllText(path, "");
			data = new ConfigData();
		}
	}

	public static void SaveConfig()
	{
		Debug.Log("Saving Config");
		string json = JsonUtility.ToJson(data);
		File.WriteAllText(Path.Combine(Application.persistentDataPath, ".config"), json);
	}
}
