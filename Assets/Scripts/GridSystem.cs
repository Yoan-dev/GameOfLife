using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

public partial struct GridInitSystem : ISystem
{
	private NativeArray<int> _cells;
	private NativeArray<int> _copy;
	private NativeArray<float4> _colors;

	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<GridComponent>();
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
		if (_cells.IsCreated)
		{
			_cells.Dispose();
			_copy.Dispose();
			_colors.Dispose();
		}
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		Entity gridEntity = SystemAPI.GetSingletonEntity<GridComponent>();
		GridComponent grid = SystemAPI.GetComponent<GridComponent>(gridEntity);

		int length = grid.Width * grid.Height;
		_cells = new NativeArray<int>(length, Allocator.Persistent);
		_copy = new NativeArray<int>(length, Allocator.Persistent);
		_colors = new NativeArray<float4>(length, Allocator.Persistent);

		state.EntityManager.AddComponentData(gridEntity, new CellArrayComponent
		{
			Cells = _cells,
			Copy = _copy,
		});
		state.EntityManager.AddComponentData(gridEntity, new ColorArrayComponent
		{
			Colors = _colors,
		});
		
		state.Enabled = false;
	}
}

[UpdateAfter(typeof(GridInitSystem))]
public partial struct GridSystem : ISystem
{
	private const int ForBatchCount = 32;
	private const float UpdateTick = 0.1f;

	private float _time;

	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<GridComponent>();
		state.RequireForUpdate<CellArrayComponent>();
		state.RequireForUpdate<BlueprintCollectionRef>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		_time += SystemAPI.Time.DeltaTime;

		if (_time >= UpdateTick)
		{
			_time -= UpdateTick;

			Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
			GridComponent grid = SystemAPI.GetComponent<GridComponent>(entity);
			int length = grid.Width * grid.Height;

			// get as RW to force job dependency
			CellArrayComponent cellArray = SystemAPI.GetComponentRW<CellArrayComponent>(entity).ValueRW;
			ColorArrayComponent colorArray = SystemAPI.GetComponentRW<ColorArrayComponent>(entity).ValueRW;

			state.Dependency = new GridUpdateJob
			{
				Read = cellArray.Copy,
				Write = cellArray.Cells,
				Colors = colorArray.Colors,
				Grid = grid,
			}.ScheduleParallel(length, ForBatchCount, state.Dependency);

			state.Dependency = new BlueprintJob
			{
				Write = cellArray.Cells,
				BlueprintCollection = SystemAPI.GetSingleton<BlueprintCollectionRef>(),
				Grid = grid,
			}.Schedule(state.Dependency);

			state.Dependency = new CopyJob
			{
				Read = cellArray.Cells,
				Write = cellArray.Copy,
			}.ScheduleParallel(length, ForBatchCount, state.Dependency);
		}
	}

	[BurstCompile]
	public partial struct GridUpdateJob : IJobFor
	{
		[ReadOnly]
		public NativeArray<int> Read;
		[WriteOnly]
		public NativeArray<int> Write;
		[WriteOnly]
		public NativeArray<float4> Colors;
		public GridComponent Grid;

		public void Execute(int index)
		{
			int x = index % Grid.Width;
			int y = index / Grid.Width;

			int xPlus = (x + 1) % Grid.Width;
			int xMinus = (Grid.Width + x - 1) % Grid.Width;
			int yPlus = (y + 1) % Grid.Height;
			int yMinus = (Grid.Height + y - 1) % Grid.Height;

			int aliveNeighborCount =
				Read[xPlus + y * Grid.Width] +
				Read[xMinus + y * Grid.Width] +
				Read[x + yPlus * Grid.Width] +
				Read[x + yMinus * Grid.Width] +
				Read[xPlus + yPlus * Grid.Width] +
				Read[xMinus + yPlus * Grid.Width] +
				Read[xPlus + yMinus * Grid.Width] +
				Read[xMinus + yMinus * Grid.Width];

			int state = aliveNeighborCount == 3 || Read[index] + aliveNeighborCount == 3 ? 1 : 0;

			Write[index] = state;
			Colors[index] = new float4(state, state, state, 1f);
		}
	}

	[BurstCompile]
	public partial struct BlueprintJob : IJobEntity
	{
		[WriteOnly]
		public NativeArray<int> Write;
		public BlueprintCollectionRef BlueprintCollection;
		public GridComponent Grid;

		public void Execute(ref DynamicBuffer<BlueprintEventBufferElement> blueprintEvents)
		{
			foreach (var blueprintEvent in blueprintEvents)
			{
				ref BlueprintData blueprint = ref BlueprintCollection.Collection.Value.Blueprints[blueprintEvent.BlueprintIndex];
				for (int i = 0; i < blueprint.Cells.Length; i++)
				{
					int2 coordinates = Grid.AdjustCoordinates(blueprint.GetCell(i, blueprintEvent.Orientation) + blueprintEvent.Coordinates);
					int index = Grid.Index(coordinates);
					Write[coordinates.x + coordinates.y * Grid.Width] = 1;
				}
			}
			blueprintEvents.Clear();
		}
	}

	[BurstCompile]
	public partial struct CopyJob : IJobFor
	{
		[ReadOnly]
		public NativeArray<int> Read;
		[WriteOnly]
		public NativeArray<int> Write;

		public void Execute(int index)
		{
			Write[index] = Read[index];
		}
	}
}