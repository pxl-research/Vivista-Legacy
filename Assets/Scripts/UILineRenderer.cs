using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct Line
{
	public Vector2 start;
	public Vector2 end;
	public float thickness;
	public Color color;
}

//NOTE(Simon): Immediate-ish mode line drawer. Lines to be drawn need to be re-added every frame.
//TODO(Simon): Performance test
public class UILineRenderer : Graphic
{
	public static List<Line> lines;
	private static UIVertex[] verts = new UIVertex[4];

	void Update()
	{
		if (lines != null && lines.Count > 0)
		{
			lines.Clear();
			SetAllDirty();
		}
	}

	public static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
	{
		if (lines == null)
		{
			lines = new List<Line>();
		}
		lines.Add(new Line
		{
			start = start,
			end = end,
			thickness = thickness,
			color = color
		});
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		vh.Clear();
		if (lines != null)
		{
			foreach (var line in lines)
			{
				Vector2 perpendicular;
				{
					var dx = line.end.x - line.start.x;
					var dy = line.end.y - line.start.y;
					perpendicular.x = -dy;
					perpendicular.y = -dx;
					perpendicular.Normalize();
				}

				var x1 = line.start - (line.thickness / 2) * perpendicular;
				var y1 = line.start + (line.thickness / 2) * perpendicular;
				var x2 = line.end - (line.thickness / 2) * perpendicular;
				var y2 = line.end + (line.thickness / 2) * perpendicular;

				verts[0].position = x1;
				verts[1].position = y1;
				verts[2].position = y2;
				verts[3].position = x2;

				verts[0].color = line.color;
				verts[1].color = line.color;
				verts[2].color = line.color;
				verts[3].color = line.color;

				vh.AddUIVertexQuad(verts);
			}
		}
	}
}
