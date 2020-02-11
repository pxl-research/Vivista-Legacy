using UnityEngine;
using System.Collections.Generic;

public class Triangulator
{
	private List<Vector3> m_points;

	public Triangulator(Vector3[] points)
	{
		m_points = new List<Vector3>(points);
	}

	public int[] Triangulate()
	{
		var indices = new List<int>();

		int numPoints = m_points.Count;
		if (numPoints < 3)
		{
			return indices.ToArray();
		}

		int[] V = new int[numPoints];
		if (Area() > 0)
		{
			for (int v = 0; v < numPoints; v++)
			{
				V[v] = v;
			}
		}
		else
		{
			for (int v = 0; v < numPoints; v++)
			{
				V[v] = (numPoints - 1) - v;
			}
		}

		int nv = numPoints;
		int count = 2 * nv;
		for (int v = nv - 1; nv > 2;)
		{
			if (count-- <= 0)
			{
				return indices.ToArray();
			}

			int u = v;
			if (nv <= u)
			{
				u = 0;
			}
			v = u + 1;
			if (nv <= v)
			{
				v = 0;
			}
			int w = v + 1;
			if (nv <= w)
			{
				w = 0;
			}

			if (Snip(u, v, w, nv, V))
			{
				int s, t;
				int a = V[u];
				int b = V[v];
				int c = V[w];
				indices.Add(a);
				indices.Add(b);
				indices.Add(c);

				for (s = v, t = v + 1; t < nv; s++, t++)
				{
					V[s] = V[t];
				}

				nv--;
				count = 2 * nv;
			}
		}

		indices.Reverse();
		return indices.ToArray();
	}

	private float Area()
	{
		int n = m_points.Count;
		float A = 0.0f;
		for (int p = n - 1, q = 0; q < n; p = q++)
		{
			Vector2 pval = m_points[p];
			Vector2 qval = m_points[q];
			A += pval.x * qval.y - qval.x * pval.y;
		}
		return A * 0.5f;
	}

	private bool Snip(int u, int v, int w, int n, int[] V)
	{
		var A = m_points[V[u]];
		var B = m_points[V[v]];
		var C = m_points[V[w]];

		if (Mathf.Epsilon > ((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x)))
		{
			return false;
		}
		for (int p = 0; p < n; p++)
		{
			if (p == u || p == v || p == w)
			{
				continue;
			}
			Vector2 P = m_points[V[p]];
			if (InsideTriangle(A, B, C, P))
			{
				return false;
			}
		}
		return true;
	}

	private static bool InsideTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 point)
	{
		float ax = p3.x - p2.x; float ay = p3.y - p2.y;
		float bx = p1.x - p3.x; float by = p1.y - p3.y;
		float cx = p2.x - p1.x; float cy = p2.y - p1.y;
		float apx = point.x - p1.x; float apy = point.y - p1.y;
		float bpx = point.x - p2.x; float bpy = point.y - p2.y;
		float cpx = point.x - p3.x; float cpy = point.y - p3.y;

		float aCROSSbp = ax * bpy - ay * bpx;
		float cCROSSap = cx * apy - cy * apx;
		float bCROSScp = bx * cpy - by * cpx;

		return aCROSSbp >= 0.0f && bCROSScp >= 0.0f && cCROSSap >= 0.0f;
	}
}