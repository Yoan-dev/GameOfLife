using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public partial struct TextureRendererComponent : IComponentData
{
}

[BurstCompile]
[UpdateAfter(typeof(ColorSystem))]
public partial class TextureRendererSystem : SystemBase
{
	private Texture2D _gridTexture;
	private Material _gridMaterial;
	private int _cachedWidth;
	private int _cachedHeight;

	[BurstCompile]
	protected override void OnCreate()
	{
		RequireForUpdate<GridComponent>();
		RequireForUpdate<ColorArrayComponent>();
		RequireForUpdate<TextureRendererComponent>();
	}

	[BurstCompile]
	protected override void OnStartRunning()
	{
		if (_gridTexture == null)
		{
			Dependency.Complete();
			Initialize(SystemAPI.GetSingleton<GridComponent>());
		}
	}

	[BurstCompile]
	protected override void OnUpdate()
	{
		Dependency.Complete();

		GridComponent grid = SystemAPI.GetSingleton<GridComponent>();
		if (grid.Width != _cachedWidth || grid.Height != _cachedHeight)
		{
			// dimensions changed, re-initialize
			Initialize(grid);
		}

		// update referenced texture
		_gridTexture.SetPixels(SystemAPI.GetSingleton<ColorArrayComponent>().Colors.Reinterpret<Color>().ToArray());
		_gridTexture.Apply();
	}

	private void Initialize(GridComponent grid)
	{
		// create a texture, assign it to a material and set it to simulation singleton

		_gridTexture = new Texture2D(grid.Width, grid.Height);
		_gridTexture.filterMode = FilterMode.Point;

		if (_gridMaterial == null)
		{
			_gridMaterial = new Material(ManagedData.Instance.GridShader);
		}
		_gridMaterial.SetTexture("_Grid", _gridTexture);

		Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
		EntitiesGraphicsSystem entitiesGraphics = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
		EntityManager.SetComponentData(entity, new MaterialMeshInfo
		{
			MeshID = entitiesGraphics.RegisterMesh(ManagedData.Instance.QuadMesh),
			MaterialID = entitiesGraphics.RegisterMaterial(_gridMaterial),
		});
		EntityManager.SetComponentData(entity, new LocalToWorld
		{
			Value = float4x4.TRS(float3.zero, quaternion.identity, new float3(grid.Width, grid.Height, 1f)),
		});

		// to detect changes
		_cachedWidth = grid.Width;
		_cachedHeight = grid.Height;
	}
}