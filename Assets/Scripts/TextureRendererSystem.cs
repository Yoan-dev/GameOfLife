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
[UpdateAfter(typeof(BlueprintSystem))]
public partial class TextureRendererSystem : SystemBase
{
	private Texture2D _gridTexture;

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

			Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
			GridComponent grid = SystemAPI.GetComponent<GridComponent>(entity);

			_gridTexture = new Texture2D(grid.Width, grid.Height);
			_gridTexture.filterMode = FilterMode.Point;

			Material gridMaterial = new Material(MonoInstance.Instance.GridShader);
			gridMaterial.SetTexture("_Grid", _gridTexture);

			EntitiesGraphicsSystem entitiesGraphics = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
			EntityManager.SetComponentData(entity, new MaterialMeshInfo
			{
				MeshID = entitiesGraphics.RegisterMesh(MonoInstance.Instance.QuadMesh),
				MaterialID = entitiesGraphics.RegisterMaterial(gridMaterial),
			});
			EntityManager.SetComponentData(entity, new LocalToWorld
			{
				Value = float4x4.TRS(float3.zero, quaternion.identity, new float3(grid.Width, grid.Height, 1f)),
			});
		}
	}

	[BurstCompile]
	protected override void OnUpdate()
	{
		Dependency.Complete();

		_gridTexture.SetPixels(SystemAPI.GetSingleton<ColorArrayComponent>().Colors.Reinterpret<Color>().ToArray());
		_gridTexture.Apply();
	}
}