using System.IO;
using UnityEngine;

public class Web : MonoBehaviour
{
	
	public static string apiRootUrl;
	public static string wwwrootUrl;
	private static string debugBaseUrl = "https://localhost:5001";

	public static string indexUrl; //videos
	public static string videoApiUrl; //video
	public static string videoWebUrl; //video
	public static string editVideoUrl; //edit_video
	public static string thumbnailUrl; //thumbnail
	public static string finishUploadUrl; //finish_upload
	public static string filesUrl; //files
	public static string fileUrl; //file
	public static string registerUrl; //register
	public static string loginUrl; //login
	public static string bugReportUrl; //report_bug
	public static string videoResultApiUrl; //video_result

	public static string versionNumberUrl; //latest_version_number
	public static string latestPlayerUrl; //latest_version_player_url
	public static string latestEditorUrl; //latest_version_editor_url

	public static int minPassLength = 8;

	public static string sessionCookie = "";
	public static string formattedCookieHeader => $"session={sessionCookie}";

	static Web()
	{
		SetDefaultUrl();
	}

	public static void SetDefaultUrl()
	{
		var baseUrl = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "server.txt")).Trim();
		SetUrls(baseUrl);
	}

	public static void SetDebugUrl()
	{
		SetUrls(debugBaseUrl);
	}

	//NOTE(Simon): By making newUrl a parameter, the server location is not hardcoded
	private static void SetUrls(string newUrl)
	{
		wwwrootUrl = newUrl;
		apiRootUrl = newUrl + "/api";

		indexUrl = apiRootUrl + "/videos";
		videoApiUrl = apiRootUrl + "/video";
		videoWebUrl = wwwrootUrl + "/video";
		editVideoUrl = wwwrootUrl + "/edit_video";
		thumbnailUrl = apiRootUrl + "/thumbnail";
		finishUploadUrl = apiRootUrl + "/finish_upload";
		filesUrl = apiRootUrl + "/files";
		fileUrl = apiRootUrl + "/file";
		registerUrl = apiRootUrl + "/register";
		loginUrl = apiRootUrl + "/login";
		bugReportUrl = apiRootUrl + "/report_bug";
		videoResultApiUrl = apiRootUrl + "/video_result";

		//Note(Simon): We ignore the baseUrl in the following endpoints on purpose. Downloading updates should always happen from official servers. So we look up the versionNumber there.
		versionNumberUrl = apiRootUrl + "/latest_version_number";
		latestPlayerUrl = apiRootUrl + "/latest_version_player_url";
		latestEditorUrl = apiRootUrl + "/latest_version_editor_url";

#if DEBUG
		//NOTE(Simon): Check whether all URLs have been filled.
		var fields = Assembly.GetExecutingAssembly()
						.GetType(nameof(Web)).GetFields(BindingFlags.Static | BindingFlags.Public)
						.Where(x => x.FieldType == typeof(string)).ToArray();

		foreach (var field in fields)
		{
			Assert.IsFalse(field.GetValue(null) == null, $"{field.Name} was not intialised in SetUrls()");
		}

		//NOTE(Simon): Check if all URLs are unique. To catch copy paste errors.
		var hashSet = new HashSet<string>();
		foreach (var field in fields)
		{
			if (!hashSet.Add((string)field.GetValue(null)))
			{
				Debug.LogError("One of the URLs in Web is dueplicated. Is this correct?");
			}
		}
#endif
}
}
