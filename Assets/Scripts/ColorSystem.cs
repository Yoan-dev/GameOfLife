using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[UpdateAfter(typeof(BlueprintSystem))]
public partial struct ColorSystem : ISystem
{
	private const int ForBatchCount = 32;

	[BurstCompile]
	public void OnCreate(ref SystemState state)
	{
		state.RequireForUpdate<GridComponent>();
		state.RequireForUpdate<CellArrayComponent>();
		state.RequireForUpdate<ColorArrayComponent>();
		state.RequireForUpdate<BlueprintCollectionRef>();
	}

	[BurstCompile]
	public void OnUpdate(ref SystemState state)
	{
		Entity entity = SystemAPI.GetSingletonEntity<GridComponent>();
		GridComponent grid = SystemAPI.GetComponent<GridComponent>(entity);

		// get as RW to force dependency (native collection)
		ColorArrayComponent colorArray = SystemAPI.GetComponentRW<ColorArrayComponent>(entity).ValueRW;

		// prepare color array for rendering
		
		state.Dependency = new GetColorsJob
		{
			Colors = colorArray.Colors,
			Cells = SystemAPI.GetComponent<CellArrayComponent>(entity).Cells,
			Grid = SystemAPI.GetSingleton<GridComponent>(),
		}.ScheduleParallel(grid.Width * grid.Height, ForBatchCount, state.Dependency);

		state.Dependency = new BlueprintPreviewJob
		{
			Colors = colorArray.Colors,
			BlueprintCollection = SystemAPI.GetSingleton<BlueprintCollectionRef>(),
			Grid = SystemAPI.GetSingleton<GridComponent>(),
		}.Schedule(state.Dependency);
	}

	[BurstCompile]
	public partial struct GetColorsJob : IJobFor
	{
		[ReadOnly]
		public NativeArray<int> Cells;
		[WriteOnly]
		public NativeArray<float4> Colors;
		public GridComponent Grid;

		public void Execute(int index)
		{
			int state = Cells[index];
			Colors[index] = new float4(state, state, state, 1f);
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

		public void Execute(in BlueprintController blueprintController, in DynamicBuffer<BlueprintEventBufferElement> blueprintEvents)
		{
			foreach (var blueprintEvent in blueprintEvents)
			{
				Print(blueprintEvent.BlueprintIndex, blueprintEvent.Orientation, blueprintEvent.Coordinates, new float4(1f, 1f, 0f, 1f));
			}

			if (blueprintController.BlueprintIndex != -1)
			{
				Print(blueprintController.BlueprintIndex, blueprintController.Orientation, blueprintController.Coordinates, new float4(1f, 0f, 0f, 1f));
			}
		}

		private void Print(int blueprintIndex, int orientation, int2 coordinates, float4 color)
		{
			ref BlueprintData blueprint = ref BlueprintCollection.Collection.Value.Blueprints[blueprintIndex];

			for (int i = 0; i < blueprint.Cells.Length; i++)
			{
				int2 cellCoordinates = Grid.AdjustCoordinates(blueprint.GetCell(i, orientation) + coordinates);
				Colors[Grid.Index(cellCoordinates)] = color;
			}
		}
	}
}