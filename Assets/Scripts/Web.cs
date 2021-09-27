using System;
using System.IO;
using UnityEngine;

public class Web : MonoBehaviour
{
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

	public static int minPassLength =		8;

	public static string sessionCookie =	"";
	public static string formattedCookieHeader => $"session={sessionCookie}";
}
