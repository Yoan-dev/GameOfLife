using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct BlueprintData
{
    public BlobArray<int2> Cells;

	public int2 GetCell(int index, int orientation)
	{
		int2 cellCoordinates = Cells[index];

		if (orientation == 90)
		{
			return new int2(cellCoordinates.y, -cellCoordinates.x);
		}
		else if (orientation == 180)
		{
			return new int2(-cellCoordinates.x, -cellCoordinates.y);
		}
		else if (orientation == 270)
		{
			return new int2(-cellCoordinates.y, cellCoordinates.x);
		}

		return cellCoordinates;
	}
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
	public int BlueprintIndex;
	public int Orientation;
}

[InternalBufferCapacity(0)]
public partial struct BlueprintEventBufferElement : IBufferElementData
{
	public int BlueprintIndex;
	public int Orientation;
	public int2 Coordinates;
}

[UpdateAfter(typeof(GridSystem))]
public partial struct BlueprintSystem : ISystem, ISystemStartStop
{
	private BlobAssetReference<BlueprintCollectionRef> _blueprintCollectionRef;

	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<GridComponent>();
		state.RequireForUpdate<ColorArrayComponent>();
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
		if (_blueprintCollectionRef.IsCreated)
		{
			_blueprintCollectionRef.Dispose();
		}
	}

	[BurstCompile]
	public void OnStartRunning(ref SystemState state)
	{
		var builder = new BlobBuilder(Allocator.Temp);

		ref BlueprintCollection blueprintCollection = ref builder.ConstructRoot<BlueprintCollection>();

		int blueprintCount = ManagedData.Instance.Blueprints.Length;
		BlobBuilderArray<BlueprintData> blueprintArrayBuilder = builder.Allocate(ref blueprintCollection.Blueprints, blueprintCount);
		for (int i = 0; i < blueprintCount; i++)
		{
			ManagedBlueprintData blueprintManagedData = ManagedData.Instance.Blueprints[i];
			blueprintArrayBuilder[i] = new BlueprintData();

			int cellsCount = blueprintManagedData.Cells.Length;
			BlobBuilderArray<int2> cellArrayBuilder = builder.Allocate(ref blueprintArrayBuilder[i].Cells, cellsCount);
			for (int j = 0; j < cellsCount; j++)
			{
				cellArrayBuilder[j] = blueprintManagedData.Cells[j];
			}
		}

		var blueprintCollectionReference = builder.CreateBlobAssetReference<BlueprintCollection>(Allocator.Persistent);
		builder.Dispose();

		Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
		state.EntityManager.AddComponentData(entity, new BlueprintCollectionRef
		{
			Collection = blueprintCollectionReference,
		});
		state.EntityManager.AddComponentData(entity, new BlueprintComponent
		{
			BlueprintIndex = 0,
		});
		state.EntityManager.AddBuffer<BlueprintEventBufferElement>(entity);
	}

	[BurstCompile]
	public void OnStopRunning(ref SystemState state)
	{
	}

	[BurstCompile]
    public void OnUpdate(ref SystemState state)
	{
		GridComponent grid = SystemAPI.GetSingleton<GridComponent>();

		Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		int2 mouseCoordinates = new int2(
			(int)((worldMousePos.x - grid.MinBounds.x) / (grid.MaxBounds.x - grid.MinBounds.x) * grid.Width),
			(int)((worldMousePos.y - grid.MinBounds.y) / (grid.MaxBounds.y - grid.MinBounds.y) * grid.Height));

		state.Dependency = new UpdateBlueprintJob
		{
			Index = ManagedUI.Instance.GetBlueprintIndex(),
		}.Schedule(state.Dependency);

		if (mouseCoordinates.x >= 0 &&
			mouseCoordinates.y >= 0 &&
			mouseCoordinates.x < grid.Width &&
			mouseCoordinates.y < grid.Height)
		{
			if (Input.GetMouseButtonDown(1))
			{
				state.Dependency = new IncrementOrientationJob().Schedule(state.Dependency);
			}
			
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
	public partial struct UpdateBlueprintJob : IJobEntity
	{
		public int Index;

		public void Execute(ref BlueprintComponent blueprintComponent)
		{
			blueprintComponent.BlueprintIndex = Index;
		}
	}

	[BurstCompile]
	public partial struct IncrementOrientationJob : IJobEntity
	{
		public void Execute(ref BlueprintComponent blueprintComponent)
		{
			blueprintComponent.Orientation = (blueprintComponent.Orientation + 90) % 360;
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
			if (blueprintComponent.BlueprintIndex != -1)
			{
				ref BlueprintData blueprint = ref BlueprintCollection.Collection.Value.Blueprints[blueprintComponent.BlueprintIndex];
				for (int i = 0; i < blueprint.Cells.Length; i++)
				{
					int2 coordinates = Grid.AdjustCoordinates(blueprint.GetCell(i, blueprintComponent.Orientation) + Coordinates);
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
			if (blueprintComponent.BlueprintIndex != -1)
			{
				blueprintEvents.Add(new BlueprintEventBufferElement
				{
					BlueprintIndex = blueprintComponent.BlueprintIndex,
					Orientation = blueprintComponent.Orientation,
					Coordinates = Coordinates,
				});
			}
		}
	}
}
