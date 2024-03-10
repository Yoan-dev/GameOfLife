using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial struct EntityGridComponent : IComponentData
{
}

public partial struct CellComponent : IComponentData
{
	public int Index;
	public int State;
}

public partial struct EntityGridInitSystem : ISystem
{
	private NativeArray<float4> _colors;

	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<EntityGridComponent>();
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
		if (_colors.IsCreated)
		{
			_colors.Dispose();
		}
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
		GridComponent grid = SystemAPI.GetComponent<GridComponent>(entity);

		int length = grid.Width * grid.Height;

		_colors = new NativeArray<float4>(length, Allocator.Persistent);

		Entity prototype = state.EntityManager.CreateEntity();
		state.EntityManager.AddComponent<CellComponent>(prototype);

		NativeArray<Entity> entities = new NativeArray<Entity>(length, Allocator.Temp);
		state.EntityManager.Instantiate(prototype, entities);

		for (int i = 0; i < length; i++)
		{
			state.EntityManager.SetComponentData(entities[i], new CellComponent
			{
				Index = i,
				State = i == 1 || i == 2 || i == grid.Width || i == grid.Width + 2 || i == grid.Width * 2 + 2 ? 1 : 0,
			});
		}

		entities.Dispose();

		state.EntityManager.AddComponentData(entity, new ColorArrayComponent
		{
			Colors = _colors,
		});
		state.EntityManager.DestroyEntity(prototype);
		state.Enabled = false;
	}
}

[UpdateAfter(typeof(EntityGridInitSystem))]
public partial struct EntityGridSystem : ISystem, ISystemStartStop
{
	private const float UpdateTick = 0.1f;

	private NativeArray<int> _cachedStates;
	private float _time;

	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<EntityGridComponent>();
		state.RequireForUpdate<ColorArrayComponent>();
	}

	[BurstCompile]
	public void OnDestroy(ref SystemState state)
	{
		if (_cachedStates.IsCreated)
		{
			_cachedStates.Dispose();
		}
	}

	[BurstCompile]
	public void OnStartRunning(ref SystemState state)
	{
		if (!_cachedStates.IsCreated)
		{
			GridComponent grid = SystemAPI.GetSingleton<GridComponent>();
			_cachedStates = new NativeArray<int>(grid.Width * grid.Height, Allocator.Persistent);
		}
	}

	public void OnStopRunning(ref SystemState state)
	{
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		_time += SystemAPI.Time.DeltaTime;

		if (_time >= UpdateTick)
		{
			_time -= UpdateTick;

			// get as RW to force job dependency
			ColorArrayComponent colorArray = SystemAPI.GetSingletonRW<ColorArrayComponent>().ValueRW;

			state.Dependency = new CacheStatesJob
			{
				States = _cachedStates,
			}.ScheduleParallel(state.Dependency);

			state.Dependency = new CellUpdateJob
			{
				States = _cachedStates,
				Colors = colorArray.Colors,
				Grid = SystemAPI.GetSingleton<GridComponent>(),
			}.ScheduleParallel(state.Dependency);
		}
	}

	[BurstCompile]
	public partial struct CacheStatesJob : IJobEntity
	{
		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public NativeArray<int> States;

		public void Execute(in CellComponent cell)
		{
			States[cell.Index] = cell.State;
		}
	}

	[BurstCompile]
	public partial struct CellUpdateJob : IJobEntity
	{
		[ReadOnly]
		public NativeArray<int> States;
		[NativeDisableParallelForRestriction]
		[WriteOnly]
		public NativeArray<float4> Colors;
		public GridComponent Grid;

		public void Execute(ref CellComponent cell)
		{
			int x = cell.Index % Grid.Width;
			int y = cell.Index / Grid.Width;

			int xPlus = (x + 1) % Grid.Width;
			int xMinus = (Grid.Width + x - 1) % Grid.Width;
			int yPlus = (y + 1) % Grid.Height;
			int yMinus = (Grid.Height + y - 1) % Grid.Height;

			int aliveNeighborCount =
				States[xPlus + y * Grid.Width] +
				States[xMinus + y * Grid.Width] +
				States[x + yPlus * Grid.Width] +
				States[x + yMinus * Grid.Width] +
				States[xPlus + yPlus * Grid.Width] +
				States[xMinus + yPlus * Grid.Width] +
				States[xPlus + yMinus * Grid.Width] +
				States[xMinus + yMinus * Grid.Width];

			cell.State = aliveNeighborCount == 3 || States[cell.Index] + aliveNeighborCount == 3 ? 1 : 0;

			Colors[cell.Index] = new float4(cell.State, cell.State, cell.State, 1f);
		}
	}
}