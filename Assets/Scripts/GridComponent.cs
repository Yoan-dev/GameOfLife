using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
public partial struct GridComponent : IComponentData
{
	public int Width;
	public int Height;
}

public partial struct ArrayGridInitComponent : IComponentData
{
}

public partial struct ArrayGridComponent : IComponentData
{
	public NativeArray<int> Cells;
	public NativeArray<int> Copy;
}

public partial struct ColorArrayComponent : IComponentData
{
	public NativeArray<float4> Colors;
}

public partial struct InstanceRendererComponent : IComponentData
{
}