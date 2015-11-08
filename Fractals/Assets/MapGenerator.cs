using UnityEngine;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour
{
	[Range(10, 150)]
	public int width = 50;
	[Range(10, 100)]
	public int depth = 50;

	public string seed;
	public bool useRandomSeed = false;

	public Color[] tileColors;

	[Range(0, 100)]
	public int EmptyThreshold = 30;
	[Range(0, 100)]
	public int DustThreshold = 40;
	[Range(0, 100)]
	public int MuddyThreshold = 50;
	//[Range(0, 100)]
	//public int WallThreshold = 60;

	[Range(0, 20)]
	public int MudAttempts = 5;

	[Range(0, 20)]
	public int DustCount = 2;
	[Range(0, 20)]
	public int SmoothCount = 5;
	//0 for empty
	//1 for dusty
	//2 for muddy
	//3 for wall
	int[,] map;

	//Cellular automata works by randomly filling
	//Then going through smoothing approaches to make tiles like their neighbors.

	void Start()
	{
		GenerateMap();
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			GenerateMap();
		}
		if (Input.GetKeyDown(KeyCode.Space))
		{
			SmoothMap();
		}
		if (Input.GetKeyDown(KeyCode.LeftControl))
		{
			AddDust();
		}

	}

	void GenerateMap()
	{
		Debug.Log("Creating new map (" + width + "," + depth + ")\n");
		map = new int[width, depth];
		RandomFillMap();
		for (int i = 0; i < SmoothCount; i++)
		{
			SmoothMap();
		}

		for (int i = 0; i < DustCount; i++)
		{
			AddDust();
		}
		//AddMud();
		//SmoothMud();

		MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(map, 1);
	}

	void RandomFillMap()
	{
		if (useRandomSeed)
		{
			seed = System.DateTime.Now.ToString();
		}

		System.Random rand = new System.Random(seed.GetHashCode());

		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < depth; z++)
			{
				int rVal = rand.Next(0, 100);
				int assign = -1;
				if (rVal < EmptyThreshold)
				{
					assign = 0;
				}
				else if (rVal < DustThreshold)
				{
					assign = 1;
				}
				//else if (rVal < MuddyThreshold)
				//{
				//	assign = 2;
				//}
				else
				{
					assign = 3;
				}

				if (x == 0 || x == width - 1 || z == 0 || z == depth - 1)
				{
					assign = 3;
				}

				map[x, z] = assign;
			}

		}
	}

	void SmoothMap()
	{
		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < depth; z++)
			{
				int nearbyDensity = CountNearbyDensity(x, z);
				if (nearbyDensity < 7)
				{
					map[x, z] = 0;
				}
				else if (nearbyDensity < 9)
				{
					map[x, z] = 1;
				}
				//else if (nearbyDensity < 9)
				//{
				//	map[x, z] = 2;
				//}
				else
				{
					map[x, z] = 3;
				}

			}
		}
	}

	#region Mud
	void SmoothMud()
	{
		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < depth; z++)
			{
				int avgDensity = AverageNeighborDensity(x, z);

				if (avgDensity > 1.2f && (avgDensity < 1f))
				{
					map[x, z] = 2;
				}

			}
		}
	}

	void AddMud()
	{
		for (int i = 0; i < MudAttempts; i++)
		{
			int randXSample = UnityEngine.Random.Range(1, width - 1);
			int randZSample = UnityEngine.Random.Range(1, depth - 1);

			int nearbyDensity = CountNearbyDensity(randXSample, randZSample);
			if (nearbyDensity < 5)
			{
				map[randXSample, randZSample] = 2;

				SpreadMud(randXSample, randZSample);
			}

			//Debug.Log("Mud attempt #" + i + " at (" + randXSample + "," + randYSample + "," + randZSample + ") with nearby density of " + nearbyDensity + "\n");
		}
	}

	void SpreadMud(int gridX, int gridZ)
	{
		for (int xMod = -5; xMod <= 5; xMod++)
		{
			for (int zMod = -5; zMod <= 5; zMod++)
			{
				int currentX = gridX + xMod;
				int currentZ = gridZ + zMod;

				bool skip = false;

				if (currentX < 0 || currentX >= width)
				{
					skip = true;
				}
				if (currentZ < 0 || currentZ >= depth)
				{
					skip = true;
				}

				if (!skip)
				{
					Debug.Log("Spreading mud\n");
					if (map[currentX, currentZ] <= 0 && UnityEngine.Random.Range(0, 10) > 4)
					{
						map[currentX, currentZ] = 2;
					}
				}
			}
		}
	}
	#endregion

	void AddDust()
	{
		for (int x = 0; x < width; x++)
		{
			for (int z = 0; z < depth; z++)
			{
				int nearbyDensity = CountNearbyDensity(x, z);
				if (nearbyDensity > -2 && nearbyDensity < 6)
				{
					map[x, z] = 1;
				}
				else if (UnityEngine.Random.Range(0, 10) > 2 && nearbyDensity > -8 && nearbyDensity < 6)
				{
					map[x, z] = 1;
				}
			}

		}
	}

	int CountNearbyDensity(int gridX, int gridZ)
	{
		int densityCount = 0;
		for (int xMod = -1; xMod <= 1; xMod++)
		{
			for (int zMod = -1; zMod <= 1; zMod++)
			{
				int currentX = gridX + xMod;
				int currentZ = gridZ + zMod;

				bool skip = false;

				if (currentX < 0 || currentX >= width)
				{
					skip = true;
				}
				if (currentZ < 0 || currentZ >= depth)
				{
					skip = true;
				}

				if (!skip)
				{
					try
					{
						if (map[currentX, currentZ] > 0)
						{
							if (currentX == gridX && currentZ == gridZ)
							{
								//I am a wall
							}
							else
							{
								densityCount += map[currentX, currentZ];
							}
						}
						else
						{
							densityCount -= 2;
						}
					}
					catch (Exception e)
					{
						Debug.Log("Unable to smooth " +
							"\tcX: " + currentX +
							"\tgX: " + gridX +
							"\tcZ: " + currentZ +
							"\tgZ: " + gridZ + "\n" + e.Message);
					}
				}
				else
				{
					densityCount += 4;
				}
			}
		}

		return densityCount;
	}

	int CountNeighborWalls(int gridX, int gridY, int gridZ)
	{
		int wallCount = 0;
		for (int xMod = -1; xMod <= 1; xMod++)
		{
			for (int zMod = -1; zMod <= 1; zMod++)
			{
				int currentX = gridX + xMod;
				int currentZ = gridZ + zMod;

				bool skip = false;

				if (currentX < 0 || currentX >= width || currentZ < 0 || currentZ >= depth)
				{
					skip = true;
				}

				if (!skip)
				{
					try
					{
						if (currentX == gridX && currentZ == gridZ)
						{

						}
						else
						{
							wallCount += map[currentX, currentZ] == 3 ? 1 : 0;
						}
					}
					catch (Exception e)
					{
						Debug.Log("Unable to smooth " +
							"\tcX: " + currentX +
							"\tgX: " + gridX +
							"\tcZ: " + currentZ +
							"\tgZ: " + gridZ +
							"\tgY: " + gridY + "\n" + e.Message);
					}
				}
				else
				{
					wallCount += 1;
				}
			}
		}
		return wallCount;
	}

	int AverageNeighborDensity(int gridX, int gridZ)
	{
		int densityCount = 0;
		int neighborCount = 0;
		for (int xMod = -1; xMod <= 1; xMod++)
		{
			for (int zMod = -1; zMod <= 1; zMod++)
			{
				neighborCount++;
				int currentX = gridX + xMod;
				int currentZ = gridZ + zMod;

				bool skip = false;

				if (currentX < 0)
				{
					neighborCount--;
					skip = true;
				}
				if (currentX >= width)
				{
					neighborCount--;
					skip = true;
				}
				if (currentZ < 0)
				{
					neighborCount--;
					skip = true;
				}
				if (currentZ >= depth)
				{
					neighborCount--;
					skip = true;
				}

				if (!skip)
				{
					try
					{
						if (currentX == gridX && currentZ == gridZ)
						{
							neighborCount--;
						}
						else
						{
							densityCount += map[currentX, currentZ];
						}
					}
					catch (Exception e)
					{
						Debug.Log("Unable to smooth " +
							"\tcX: " + currentX +
							"\tgX: " + gridX +
							"\tcZ: " + currentZ +
							"\tgZ: " + gridZ + "\n" + e.Message);
					}
				}
				else
				{
					densityCount += 1;
				}
			}
		}

		return densityCount / neighborCount;
	}

	void OnDrawGizmos()
	{
		/*if (map != null)
		{
			for (int x = 0; x < width; x++)
			{
				for (int z = 0; z < depth; z++)
				{
					try
					{
						Gizmos.color = tileColors[map[x, z]];
						Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -depth / 2 + z + .5f);
						Gizmos.DrawCube(pos, Vector3.one);
					}
					catch (Exception e)
					{
						Debug.LogError("Unable to create Gizmo at (" + x + ",0," + z + ")\n" + map.Length + "\n" + e.Message);
					}

				}
			}
		}*/
	}
}
