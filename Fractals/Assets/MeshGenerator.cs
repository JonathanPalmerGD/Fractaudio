using UnityEngine;
using System.Collections;

public class MeshGenerator : MonoBehaviour 
{
	public SquareGrid sGrid;

	public void GenerateMesh(int[,] map, float squareSize)
	{
		sGrid = new SquareGrid(map, squareSize);
	}

	void OnDrawGizmos()
	{
		for (int x = 0; x < sGrid.squares.GetLength(0); x++)
		{
			for (int z = 0; z < sGrid.squares.GetLength(1); z++)
			{
				Gizmos.color = sGrid.squares[x, z].topLeft.Active ? Color.black : Color.white;
				Gizmos.DrawCube(sGrid.squares[x, z].topLeft.pos, Vector3.one * .4f);

				Gizmos.color = sGrid.squares[x, z].topRight.Active ? Color.black:Color.white;
				Gizmos.DrawCube(sGrid.squares[x, z].topRight.pos, Vector3.one * .4f);

				Gizmos.color = sGrid.squares[x, z].bottomRight.Active ? Color.black:Color.white;
				Gizmos.DrawCube(sGrid.squares[x, z].bottomRight.pos, Vector3.one * .4f);

				Gizmos.color = sGrid.squares[x, z].bottomLeft.Active ? Color.black:Color.white;
				Gizmos.DrawCube(sGrid.squares[x, z].bottomLeft.pos, Vector3.one * .4f);

				Gizmos.color = Color.grey;
				Gizmos.DrawCube(sGrid.squares[x, z].centerTop.pos, Vector3.one * .15f);
				Gizmos.DrawCube(sGrid.squares[x, z].centerRight.pos, Vector3.one * .15f);
				Gizmos.DrawCube(sGrid.squares[x, z].centerBottom.pos, Vector3.one * .15f);
				Gizmos.DrawCube(sGrid.squares[x, z].centerLeft.pos, Vector3.one * .15f);

			}
		}
	}

	public class SquareGrid
	{
		public Square[,] squares;

		public SquareGrid(int [,] map, float squareSize)
		{
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


			for (int x = 0; x < nodeCountX - 1; x++)
			{
				for (int z = 0; z < nodeCountZ - 1; z++)
				{
					squares[x, z] = new Square(cNodes[x, z + 1], cNodes[x + 1, z + 1], cNodes[x + 1, z], cNodes[x, z]);
				}
			}
		}
	}

	public class Square
	{
		public ControlNode topLeft, topRight, bottomLeft, bottomRight;

		public Node centerTop, centerRight, centerBottom, centerLeft;

		public Square(ControlNode tl, ControlNode tr, ControlNode bl, ControlNode br)
		{
			topLeft = tl;
			topRight = tr;
			bottomLeft = bl;
			bottomRight = br;

			centerTop = topLeft.right;
			centerRight = bottomRight.above;
			centerBottom = bottomLeft.right;
			centerLeft = bottomLeft.above;
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

		public ControlNode(Vector3 newPos, int newState, float squareSize) : base(newPos)
		{
			state = newState;
			above = new Node(pos + Vector3.forward * squareSize/2f);
			above = new Node(pos + Vector3.right * squareSize / 2f);
			
		}
	}

}
