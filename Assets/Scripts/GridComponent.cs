using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public partial struct GridComponent : IComponentData
{
	public int Width;
	public int Height;
	public float2 MinBounds;
	public float2 MaxBounds;

	public int Index(int2 coordinates)
	{
		return coordinates.x + coordinates.y * Width;
	}

	public int2 AdjustCoordinates(int2 coordinates)
	{
		return new int2(
			(coordinates.x + Width) % Width,
			(coordinates.y + Height) % Height);
	}
}

public partial struct CellArrayComponent : IComponentData
{
	public NativeArray<int> Cells;
	public NativeArray<int> Copy;
}

public partial struct ColorArrayComponent : IComponentData
{
	public NativeArray<float4> Colors;
}