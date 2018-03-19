using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct Line
{
	public Vector2 start;
	public Vector2 end;
	public float thickness;
}

//NOTE(Simon): Immediate-ish mode line drawer. Lines to be drawn need to be re-added every frame.
public class UILineRenderer : Graphic
{
	public static List<Line> lines;

	public UILineRenderer()
	{
		lines = new List<Line>();
	}

	void Update()
	{
		lines.Clear();
		SetAllDirty();
	}

	public static void DrawLine(Vector2 start, Vector2 end, float thickness)
	{
		lines.Add(new Line
		{
			start = start,
			end = end,
			thickness = thickness
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
					perpendicular = new Vector2(-dy, dx);
					perpendicular.Normalize();
				}

				var x1 = line.start - (line.thickness / 2) * perpendicular;
				var y1 = line.start + (line.thickness / 2) * perpendicular;
				var x2 = line.end - (line.thickness / 2) * perpendicular;
				var y2 = line.end + (line.thickness / 2) * perpendicular;

				var verts = new UIVertex[4];
				verts[0].position = x1;
				verts[1].position = y1;
				verts[2].position = y2;
				verts[3].position = x2;

				verts[0].color = color;
				verts[1].color = color;
				verts[2].color = color;
				verts[3].color = color;

				vh.AddUIVertexQuad(verts);
			}
		}
	}
}
