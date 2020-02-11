using UnityEngine;

public class Cursors : MonoBehaviour 
{
	public static Cursors Instance { get; private set; }

	public Texture2D CursorDrag;
	public Texture2D CursorResizeHorizontal;
	public Texture2D CursorResizeVertical;

	private void Awake()
	{
		Instance = this;
	}

	public static bool isOverridingCursor;
}
