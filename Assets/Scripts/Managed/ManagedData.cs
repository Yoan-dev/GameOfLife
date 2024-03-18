using System;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct ManagedBlueprintData
{
	public string Name;
	public int2[] Cells;
}

public class ManagedData : MonoBehaviour
{
	public static ManagedData Instance;

	[Header("Rendering")]
	public Mesh QuadMesh;
	public Material InstanceMaterial;
	public Shader GridShader;

	[Header("Blueprints")]
	public ManagedBlueprintData[] Blueprints = new ManagedBlueprintData[0];

	public void Awake()
	{
		// please don't judge me
		Instance = this;
	}
}