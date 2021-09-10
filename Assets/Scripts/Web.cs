
public static class Web
{
	//public static string baseUrl =			"http://localhost:5000/api";
	public static string baseUrl =			"http://10.50.70.240/api";
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
