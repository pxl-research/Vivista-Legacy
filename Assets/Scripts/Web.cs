
public static class Web
{
	public static string baseUrl =			"http://localhost:5000/api";
	public static string indexUrl =			baseUrl + "/videos";
	public static string metaUrl =			baseUrl + "/meta";
	public static string chaptersUrl =		baseUrl + "/chapters";
	public static string tagsUrl =			baseUrl + "/tags";
	public static string videoUrl =			baseUrl + "/video";
	public static string downloadVideoUrl =	baseUrl + "/download_video";
	public static string thumbnailUrl =		baseUrl + "/thumbnail";
	public static string fileUrl =			baseUrl + "/file";
	public static string extrasUrl =		baseUrl + "/extras";
	public static string miniaturesUrl =	baseUrl + "/miniatures";
	public static string extraUrl =			baseUrl + "/extra";
	public static string registerUrl =		baseUrl + "/register";
	public static string loginUrl =			baseUrl + "/login";

	public static int minPassLength =		8;

	public static string sessionCookie =	"";
}
