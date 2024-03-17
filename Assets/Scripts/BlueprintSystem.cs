using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BlueprintData
{
    public BlobArray<int2> Cells;
}

public struct BlueprintCollection : IComponentData
{
	public BlobArray<BlueprintData> Blueprints;
}

public struct BlueprintCollectionRef : IComponentData
{
	public BlobAssetReference<BlueprintCollection> Collection;
}

public partial struct BlueprintComponent : IComponentData
{
	public int BlueprintId;
	public int Orientation;
}

[InternalBufferCapacity(0)]
public partial struct BlueprintEventBufferElement : IBufferElementData
{
	public int BlueprintId;
	public int Orientation;
	public int2 Coordinates;
}

[UpdateAfter(typeof(GridSystem))]
public partial struct BlueprintSystem : ISystem
{
    [BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<BlueprintComponent>();
		state.RequireForUpdate<BlueprintCollectionRef>();
		state.RequireForUpdate<ColorArrayComponent>();
	}
	
	[BurstCompile]
    public void OnUpdate(ref SystemState state)
	{
		GridComponent grid = SystemAPI.GetSingleton<GridComponent>();

		Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		int2 mouseCoordinates = new int2(
			(int)((worldMousePos.x - grid.MinBounds.x) / (grid.MaxBounds.x - grid.MinBounds.x) * grid.Width),
			(int)((worldMousePos.y - grid.MinBounds.y) / (grid.MaxBounds.y - grid.MinBounds.y) * grid.Height));

		if (mouseCoordinates.x >= 0 &&
			mouseCoordinates.y >= 0 &&
			mouseCoordinates.x < grid.Width &&
			mouseCoordinates.y < grid.Height)
		{
			// get as RW to force job dependency
			ColorArrayComponent colorArray = SystemAPI.GetSingletonRW<ColorArrayComponent>().ValueRW;

			state.Dependency = new BlueprintPreviewJob
			{
				Colors = colorArray.Colors,
				BlueprintCollection = SystemAPI.GetSingleton<BlueprintCollectionRef>(),
				Coordinates = mouseCoordinates,
				Grid = grid,
			}.Schedule(state.Dependency);

			if (Input.GetMouseButtonDown(0))
			{
				state.Dependency = new AddBlueprintEventJob
				{
					Coordinates = mouseCoordinates,
				}.Schedule(state.Dependency);
			}
		}
	}

	[BurstCompile]
	public partial struct BlueprintPreviewJob : IJobEntity
	{
		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public NativeArray<float4> Colors;
		public BlueprintCollectionRef BlueprintCollection;
		public GridComponent Grid;
		public int2 Coordinates;

		public void Execute(in BlueprintComponent blueprintComponent)
		{
			if (blueprintComponent.BlueprintId != -1)
			{
				// TODO: rotation

				ref BlueprintData blueprint = ref BlueprintCollection.Collection.Value.Blueprints[blueprintComponent.BlueprintId];
				for (int i = 0; i < blueprint.Cells.Length; i++)
				{
					int2 coordinates = Grid.AdjustCoordinates(blueprint.Cells[i] + Coordinates);
					int index = Grid.Index(coordinates);
					Colors[index] = new float4(1f, 0f, 0f, 1f);
				}
			}
		}
	}

	[BurstCompile]
	public partial struct AddBlueprintEventJob : IJobEntity
	{
		public int2 Coordinates;

		public void Execute(in BlueprintComponent blueprintComponent, ref DynamicBuffer<BlueprintEventBufferElement> blueprintEvents)
		{
			if (blueprintComponent.BlueprintId != -1)
			{
				blueprintEvents.Add(new BlueprintEventBufferElement
				{
					BlueprintId = blueprintComponent.BlueprintId,
					Orientation = blueprintComponent.Orientation,
					Coordinates = Coordinates,
				});
			}
		}
	}
}
