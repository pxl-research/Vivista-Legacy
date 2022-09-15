using System;
using System.Collections.Generic;
using UnityEngine;

public class AreaRenderer : MonoBehaviour
{
	public MeshFilter meshFilter;
	public MeshFilter outlineFilter;
	public MeshCollider meshCollider;
	public MeshRenderer meshRenderer;
	public MeshRenderer outlineRenderer;

	public void Awake()
	{
		transform.position = Vector3.zero;
	}

	public void SetVertices(List<Vector3> vertices)
	{
		SetVertices(vertices.ToArray());
	}

	public void SetVertices(Vector3[] vertices)
	{
		var mesh = new Mesh();

		mesh.vertices = vertices;
		var triangulator = new Triangulator(mesh.vertices);
		mesh.triangles = triangulator.Triangulate();

		//NOTE(Simon): Throws error is < 3 vertices are used for a meshCollider
		if (vertices.Length > 2)
		{
			meshCollider.sharedMesh = mesh;
		}
		//NOTE(Simon): Should this be mesh instead of sharedMesh???
		meshFilter.sharedMesh = mesh;

		var outlineVertices = new Vector3[mesh.vertices.Length + 1];
		Array.Copy(mesh.vertices, outlineVertices, mesh.vertices.Length);
		outlineVertices[outlineVertices.Length - 1] = outlineVertices[0];
		outlineFilter.mesh.vertices = outlineVertices;

		//NOTE(Simon): Generate indices for a line strip.
		var indices = new int[outlineVertices.Length];
		for (int i = 0; i < indices.Length; i++)
		{
			indices[i] = i;
		}

		outlineFilter.mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
	}

	public void EnableCollider()
	{
		meshCollider.enabled = true;
	}

	public void DisableCollider()
	{
		meshCollider.enabled = false;
	}

	public void EnableRenderer()
	{
		meshRenderer.enabled = true;
		outlineRenderer.enabled = true;
	}

	public void DisableRenderer()
	{
		meshRenderer.enabled = false;
		outlineRenderer.enabled = false;
	}

	public Bounds GetBounds()
	{
		return meshFilter.mesh.bounds;
	}

	public void SetColor(Color color)
	{
		outlineRenderer.material.color = color;
		color.a = .5f;
		meshRenderer.material.color = color;
	}
}
