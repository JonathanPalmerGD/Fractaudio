using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshGenerator : MonoBehaviour
{
	public SquareGrid sGrid;
	public List<Vector3> vertices;
	public List<int> tris;
	public float wallHeight = 2;
	Dictionary<int, List<Triangle>> triDict = new Dictionary<int, List<Triangle>>();

	public MeshFilter walls;

	public List<List<int>> outlines = new List<List<int>>();
	HashSet<int> checkedVerts = new HashSet<int>();

	public void GenerateMesh(int[,] map, float squareSize)
	{
		outlines.Clear();
		checkedVerts.Clear();

		triDict.Clear();

		vertices = new List<Vector3>();
		tris = new List<int>();

		sGrid = new SquareGrid(map, squareSize);

		for (int x = 0; x < sGrid.squares.GetLength(0); x++)
		{
			for (int z = 0; z < sGrid.squares.GetLength(1); z++)
			{
				TriangulateSquare(sGrid.squares[x, z]);
			}
		}

		if (GetComponent<MapGenerator>().currentGenMode == MapGenerator.GenMode.Walls)
		{
			Mesh mesh = new Mesh();
			GetComponent<MeshFilter>().mesh = mesh;

			mesh.vertices = vertices.ToArray();
			mesh.triangles = tris.ToArray();

			mesh.RecalculateNormals();

			CreateWallMesh();
		}
	}

	void CreateWallMesh()
	{
		CalculateMeshOutlines();

		List<Vector3> wallVerts = new List<Vector3>();
		List<int> wallTris = new List<int>();

		Mesh wallMesh = new Mesh();

		foreach (List<int> outline in outlines)
		{
			for (int i = 0; i < outline.Count - 1; i++)
			{
				int startInd = wallVerts.Count;

				wallVerts.Add(vertices[outline[i]]); // Left
				wallVerts.Add(vertices[outline[i + 1]]); // Right
				wallVerts.Add(vertices[outline[i]] - Vector3.up * wallHeight); // Bottom Left
				wallVerts.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // Bottom Right

				//Tri 1
				wallTris.Add(startInd + 0);
				wallTris.Add(startInd + 2);
				wallTris.Add(startInd + 3);

				//Tri 2
				wallTris.Add(startInd + 3);
				wallTris.Add(startInd + 1);
				wallTris.Add(startInd + 0);
			}
		}
		wallMesh.vertices = wallVerts.ToArray();
		wallMesh.triangles = wallTris.ToArray();
		walls.mesh = wallMesh;
	}

	void TriangulateSquare(Square sqr)
	{
		switch (sqr.config)
		{
			// 0 points:
			case 0:
				break;
			// 1 points:
			case 1:
				MeshFromPoints(sqr.centerLeft, sqr.centerBottom, sqr.bottomLeft);
				break;
			case 2:
				MeshFromPoints(sqr.bottomRight, sqr.centerBottom, sqr.centerRight);
				break;
			case 4:
				MeshFromPoints(sqr.topRight, sqr.centerRight, sqr.centerTop);
				break;
			case 8:
				MeshFromPoints(sqr.topLeft, sqr.centerTop, sqr.centerLeft);
				break;

			// 2 points:
			case 3:
				MeshFromPoints(sqr.centerRight, sqr.bottomRight, sqr.bottomLeft, sqr.centerLeft);
				break;
			case 6:
				MeshFromPoints(sqr.centerTop, sqr.topRight, sqr.bottomRight, sqr.centerBottom);
				break;
			case 9:
				MeshFromPoints(sqr.topLeft, sqr.centerTop, sqr.centerBottom, sqr.bottomLeft);
				break;
			case 12:
				MeshFromPoints(sqr.topLeft, sqr.topRight, sqr.centerRight, sqr.centerLeft);
				break;
			case 5:
				MeshFromPoints(sqr.centerTop, sqr.topRight, sqr.centerRight, sqr.centerBottom, sqr.bottomLeft, sqr.centerLeft);
				break;
			case 10:
				MeshFromPoints(sqr.topLeft, sqr.centerTop, sqr.centerRight, sqr.bottomRight, sqr.centerBottom, sqr.centerLeft);
				break;

			// 3 point:
			case 7:
				MeshFromPoints(sqr.centerTop, sqr.topRight, sqr.bottomRight, sqr.bottomLeft, sqr.centerLeft);
				break;
			case 11:
				MeshFromPoints(sqr.topLeft, sqr.centerTop, sqr.centerRight, sqr.bottomRight, sqr.bottomLeft);
				break;
			case 13:
				MeshFromPoints(sqr.topLeft, sqr.topRight, sqr.centerRight, sqr.centerBottom, sqr.bottomLeft);
				break;
			case 14:
				MeshFromPoints(sqr.topLeft, sqr.topRight, sqr.bottomRight, sqr.centerBottom, sqr.centerLeft);
				break;

			// 4 point:
			case 15:
				MeshFromPoints(sqr.topLeft, sqr.topRight, sqr.bottomRight, sqr.bottomLeft);

				//We add these because they can never be on the outside of an outline.
				checkedVerts.Add(sqr.topLeft.vertInd);
				checkedVerts.Add(sqr.topRight.vertInd);
				checkedVerts.Add(sqr.bottomRight.vertInd);
				checkedVerts.Add(sqr.bottomLeft.vertInd);
				break;
		}
	}

	void MeshFromPoints(params Node[] points)
	{
		AssignVertices(points);

		if (points.Length >= 3)
		{
			CreateTriangle(points[0], points[1], points[2]);
		}
		if (points.Length >= 4)
		{
			CreateTriangle(points[0], points[2], points[3]);
		}
		if (points.Length >= 5)
		{
			CreateTriangle(points[0], points[3], points[4]);
		}
		if (points.Length >= 6)
		{
			CreateTriangle(points[0], points[4], points[5]);
		}
	}

	void AssignVertices(Node[] points)
	{
		for (int i = 0; i < points.Length; i++)
		{
			if (points[i].vertInd == -1)
			{
				//Will count up as we add
				points[i].vertInd = vertices.Count;

				vertices.Add(points[i].pos);
			}
		}
	}

	void CreateTriangle(Node a, Node b, Node c)
	{
		tris.Add(a.vertInd);
		tris.Add(b.vertInd);
		tris.Add(c.vertInd);

		Triangle tri = new Triangle(a.vertInd, b.vertInd, c.vertInd);
		AddTriangleToDict(tri.vertIndA, tri);
		AddTriangleToDict(tri.vertIndB, tri);
		AddTriangleToDict(tri.vertIndC, tri);
	}

	void AddTriangleToDict(int vertIndKey, Triangle newTri)
	{
		if (triDict.ContainsKey(vertIndKey))
		{
			triDict[vertIndKey].Add(newTri);
		}
		else
		{
			List<Triangle> triList = new List<Triangle>();
			triList.Add(newTri);
			triDict.Add(vertIndKey, triList);
		}
	}

	void CalculateMeshOutlines()
	{
		for (int vertInd = 0; vertInd < vertices.Count; vertInd++)
		{
			if (!checkedVerts.Contains(vertInd))
			{
				int newOutlineVertex = GetConnectedOutline(vertInd);
				if (newOutlineVertex != -1)
				{
					checkedVerts.Add(vertInd);

					List<int> newOutline = new List<int>();
					newOutline.Add(vertInd);

					outlines.Add(newOutline);

					FollowOutline(newOutlineVertex, outlines.Count - 1);

					//Close the outline.
					outlines[outlines.Count - 1].Add(vertInd);
				}
			}
		}
	}

	//Recursive following.
	void FollowOutline(int vertInd, int outlineIndex)
	{
		outlines[outlineIndex].Add(vertInd);
		checkedVerts.Add(vertInd);

		int nextVertInd = GetConnectedOutline(vertInd);

		if (nextVertInd != -1)
		{
			FollowOutline(nextVertInd, outlineIndex);
		}
	}

	int GetConnectedOutline(int vertInd)
	{
		List<Triangle> TrisContainVertex = triDict[vertInd];

		for (int i = 0; i < TrisContainVertex.Count; i++)
		{
			Triangle tri = TrisContainVertex[i];

			for (int vCount = 0; vCount < 3; vCount++)
			{
				//This uses the indexer we set up for Triangle (to get the vertex listed)
				int vertB = tri[vCount];

				//Don't continue if it's the same as B, or if it is already checked.
				if (vertB != vertInd && !checkedVerts.Contains(vertB))
				{
					if (IsOutlineEdge(vertInd, vertB))
					{
						return vertB;
					}
				}
			}
		}

		return -1;
	}

	bool IsOutlineEdge(int vertA, int vertB)
	{
		List<Triangle> triContainA = triDict[vertA];
		int sharedTriCount = 0;

		for (int i = 0; i < triContainA.Count; i++)
		{
			if (triContainA[i].Contains(vertB))
			{
				sharedTriCount++;
				if (sharedTriCount > 1)
				{
					break;
				}
			}

		}
		return sharedTriCount == 1;
	}

	struct Triangle
	{
		public int vertIndA;
		public int vertIndB;
		public int vertIndC;

		int[] vert;

		//This is an indexer for accessing the vert points like an array.
		public int this[int i]
		{
			get { return vert[i]; }
		}

		public Triangle(int a, int b, int c)
		{
			vertIndA = a;
			vertIndB = b;
			vertIndC = c;

			vert = new int[3];
			vert[0] = a;
			vert[1] = b;
			vert[2] = c;
		}

		public bool Contains(int vertexIndex)
		{
			return vertexIndex == vertIndA || vertexIndex == vertIndB || vertexIndex == vertIndC;
		}
	}

	public class SquareGrid
	{
		public Square[,] squares;

		public SquareGrid(int[,] map, float squareSize)
		{
			//Debug.Log("Hit2\n");
			int nodeCountX = map.GetLength(0);
			int nodeCountZ = map.GetLength(1);

			float mapWidth = nodeCountX * squareSize;
			float mapDepth = nodeCountZ * squareSize;

			ControlNode[,] cNodes = new ControlNode[nodeCountX, nodeCountZ];

			for (int x = 0; x < nodeCountX; x++)
			{
				for (int z = 0; z < nodeCountZ; z++)
				{
					Vector3 pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0, -mapWidth / 2 + z * squareSize + squareSize / 2);

					cNodes[x, z] = new ControlNode(pos, map[x, z], squareSize);
				}
			}

			squares = new Square[nodeCountX - 1, nodeCountZ - 1];

			int nullCount = 0;

			for (int x = 0; x < nodeCountX - 1; x++)
			{
				for (int z = 0; z < nodeCountZ - 1; z++)
				{
					squares[x, z] = new Square(cNodes[x, z + 1], cNodes[x + 1, z + 1], cNodes[x + 1, z], cNodes[x, z]);

					nullCount += squares[x, z].topLeft.right == null ? 1 : 0;

				}
			}

			//Debug.Log("Null Count: " + nullCount + "\n");
			//Debug.Log("X " + nodeCountX + "  Z " + nodeCountZ + "\n");
			//Debug.Log("Square Count " + squares.Length + "  \n");
		}
	}

	public class Square
	{
		public ControlNode topLeft, topRight, bottomRight, bottomLeft;
		public Node centerTop, centerRight, centerBottom, centerLeft;
		public int config;

		public Square(ControlNode tl, ControlNode tr, ControlNode br, ControlNode bl)
		{
			topLeft = tl;
			topRight = tr;
			bottomRight = br;
			bottomLeft = bl;

			//Debug.Log(topLeft == null);
			centerTop = topLeft.right;
			centerRight = bottomRight.above;
			centerBottom = bottomLeft.right;
			centerLeft = bottomLeft.above;
			//Debug.Log(centerTop == null);

			#region Configuration Setup
			if (topLeft.Active)
				config += 8;
			if (topRight.Active)
				config += 4;
			if (bottomRight.Active)
				config += 2;
			if (bottomLeft.Active)
				config += 1;
			#endregion
		}
	}

	public class Node
	{
		public Vector3 pos;
		public int vertInd = -1;

		public Node(Vector3 newPos)
		{
			pos = newPos;
		}

	}

	public class ControlNode : Node
	{
		public int state;
		public bool Active
		{
			get { return (state == 3); }
		}
		public Node above, right;

		public ControlNode(Vector3 newPos, int newState, float squareSize)
			: base(newPos)
		{
			state = newState;
			above = new Node(pos + Vector3.forward * squareSize / 2f);
			right = new Node(pos + Vector3.right * squareSize / 2f);

		}
	}

	void OnDrawGizmos()
	{
		if (GetComponent<MapGenerator>().currentGenMode == MapGenerator.GenMode.Layout)
		{
			int nullCount = 0;
			int totalCount = 0;
			if (sGrid != null && sGrid.squares != null)
			{
				for (int x = 0; x < sGrid.squares.GetLength(0); x++)
				{
					for (int z = 0; z < sGrid.squares.GetLength(1); z++)
					{
						totalCount++;
						Gizmos.color = sGrid.squares[x, z].topLeft.Active ? Color.black : Color.white;
						Gizmos.DrawCube(sGrid.squares[x, z].topLeft.pos, Vector3.one * .4f);

						Gizmos.color = sGrid.squares[x, z].topRight.Active ? Color.black : Color.white;
						Gizmos.DrawCube(sGrid.squares[x, z].topRight.pos, Vector3.one * .4f);

						Gizmos.color = sGrid.squares[x, z].bottomRight.Active ? Color.black : Color.white;
						Gizmos.DrawCube(sGrid.squares[x, z].bottomRight.pos, Vector3.one * .4f);

						Gizmos.color = sGrid.squares[x, z].bottomLeft.Active ? Color.black : Color.white;
						Gizmos.DrawCube(sGrid.squares[x, z].bottomLeft.pos, Vector3.one * .4f);

						if (sGrid.squares[x, z].centerTop == null)
							nullCount++;
						Gizmos.color = Color.grey;
						Gizmos.DrawCube(sGrid.squares[x, z].centerTop.pos, Vector3.one * .15f);
						Gizmos.DrawCube(sGrid.squares[x, z].centerRight.pos, Vector3.one * .15f);
						Gizmos.DrawCube(sGrid.squares[x, z].centerBottom.pos, Vector3.one * .15f);
						Gizmos.DrawCube(sGrid.squares[x, z].centerLeft.pos, Vector3.one * .15f);

					}
				}
			}
		}

		//Debug.Log("Null Count: " + nullCount + "\nTotal Count: " + totalCount); ;
	}
}
