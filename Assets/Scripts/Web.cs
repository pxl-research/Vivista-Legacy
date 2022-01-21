using System;
using System.IO;
using UnityEngine;

public class Web : MonoBehaviour
{
	//Note(Simon): This is used for requests that always need to go to the main public server, such as requests to check for updates
	//public static string baseUrlPublic = "vivista.net/api";
	public static string baseUrlPublic = "localhost:5000/api";
	//public static string baseUrlPublicFiles = "vivista.net";
	public static string baseUrlPublicFiles = "localhost:5000";

	public static string baseUrl 
	{
		get 
		{
			if (String.IsNullOrEmpty(_baseUrl))
			{
				_baseUrl = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "server.txt")).Trim() + "/api";
				return _baseUrl;
			}
			return _baseUrl;
		}
	}
	private static string _baseUrl = "";

	public static string indexUrl =			baseUrl + "/videos";
	public static string videoUrl =			baseUrl + "/video";
	public static string thumbnailUrl =		baseUrl + "/thumbnail";
	public static string finishUploadUrl =	baseUrl + "/finish_upload";
	public static string filesUrl =			baseUrl + "/files";
	public static string fileUrl =			baseUrl + "/file";
	public static string registerUrl =		baseUrl + "/register";
	public static string loginUrl =			baseUrl + "/login";
	public static string bugReportUrl =		baseUrl + "/report_bug";

	//Note(Simon): We ignore the baseUrl in the following endpoints on purpose. Downloading updates should always happen from official servers. So we look up the versionNumber there.
	public static string versionNumberUrl = baseUrlPublic + "/latest_version_number";
	public static string latestPlayerUrl = baseUrlPublic + "/latest_version_player_url";
	public static string latestEditorUrl = baseUrlPublic + "/latest_version_editor_url";

	public static int minPassLength =		8;

	public static string sessionCookie =	"";
	public static string formattedCookieHeader => $"session={sessionCookie}";
}
