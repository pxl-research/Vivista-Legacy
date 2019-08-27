using System.IO;
using UnityEngine;

public static class AudioHelper
{
	public static AudioType AudioTypeFromFilename(string urlToLoad)
	{
		var extension = Path.GetExtension(urlToLoad);
		var audioType = AudioType.UNKNOWN;
		if (extension == ".mp3")
		{
			audioType = AudioType.MPEG;
		}
		if (extension == ".ogg")
		{
			audioType = AudioType.OGGVORBIS;
		}
		if (extension == ".aif" || extension == ".aiff")
		{
			audioType = AudioType.AIFF;
		}
		if (extension == ".wav")
		{
			audioType = AudioType.WAV;
		}
		return audioType;
	}
}
