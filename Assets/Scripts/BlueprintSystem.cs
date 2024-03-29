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

public partial struct BlueprintController : IComponentData
{
	public int BlueprintIndex;
	public int Orientation;
	public int2 Coordinates;
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
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
		if (_blueprintCollectionRef.IsCreated)
		{
			_blueprintCollectionRef.Dispose();
		}
	}

	//[BurstCompile]
	public void OnStartRunning(ref SystemState state)
	{
		var builder = new BlobBuilder(Allocator.Temp);

		// create blueprints data from managed data instance

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

		// add blueprint-related components to simulation singleton

		Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
		state.EntityManager.AddComponentData(entity, new BlueprintCollectionRef
		{
			Collection = blueprintCollectionReference,
		});
		state.EntityManager.AddComponentData(entity, new BlueprintController
		{
			BlueprintIndex = 0,
		});
		state.EntityManager.AddBuffer<BlueprintEventBufferElement>(entity);
	}

	[BurstCompile]
	public void OnStopRunning(ref SystemState state)
	{
	}

	//[BurstCompile]
    public void OnUpdate(ref SystemState state)
	{
		GridComponent grid = SystemAPI.GetSingleton<GridComponent>();

		// mouse position to position on grid
		Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		int2 mouseCoordinates = new int2(
			(int)((worldMousePos.x - grid.MinBounds.x) / (grid.MaxBounds.x - grid.MinBounds.x) * grid.Width),
			(int)((worldMousePos.y - grid.MinBounds.y) / (grid.MaxBounds.y - grid.MinBounds.y) * grid.Height));

		bool isMouseOnGrid =
			mouseCoordinates.x >= 0 &&
			mouseCoordinates.y >= 0 &&
			mouseCoordinates.x < grid.Width &&
			mouseCoordinates.y < grid.Height;

		state.Dependency = new UpdateBlueprintJob
		{
			Index = isMouseOnGrid ? UIManager.Instance.GetBlueprintIndex() : -1,
			PressedRotateInput = Input.GetMouseButtonDown(1),
			Coordinates = mouseCoordinates,
		}.Schedule(state.Dependency);

		if (isMouseOnGrid && Input.GetMouseButtonDown(0))
		{
			// create a blueprint event
			// will be printed during simulation (GridSystem)
			state.Dependency = new AddBlueprintEventJob().Schedule(state.Dependency);
		}
	}

	[BurstCompile]
	public partial struct UpdateBlueprintJob : IJobEntity
	{
		public int Index;
		public bool PressedRotateInput;
		public int2 Coordinates;

		public void Execute(ref BlueprintController blueprintController)
		{
			blueprintController.BlueprintIndex = Index;
			blueprintController.Coordinates = Coordinates;

			if (PressedRotateInput)
			{
				blueprintController.Orientation = (blueprintController.Orientation + 90) % 360;
			}
		}
	}

	[BurstCompile]
	public partial struct AddBlueprintEventJob : IJobEntity
	{
		public void Execute(in BlueprintController blueprintController, ref DynamicBuffer<BlueprintEventBufferElement> blueprintEvents)
		{
			if (blueprintController.BlueprintIndex != -1)
			{
				blueprintEvents.Add(new BlueprintEventBufferElement
				{
					BlueprintIndex = blueprintController.BlueprintIndex,
					Orientation = blueprintController.Orientation,
					Coordinates = blueprintController.Coordinates,
				});
			}
		}
	}
}
