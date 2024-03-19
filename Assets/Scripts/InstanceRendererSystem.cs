using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct InstanceRendererComponent : IComponentData
{
	public float CellSpacing;
}

[BurstCompile]
[UpdateAfter(typeof(ColorSystem))]
public partial class InstanceRendererSystem : SystemBase
{
	private const int RenderBatchSize = 454;

	private NativeArray<Matrix4x4> _matrices;
	private Mesh _mesh;
	private Material _material;
	private GraphicsBuffer _buffer;
	private MaterialPropertyBlock _materialPropertyBlock;
	private RenderParams _renderParams;
	private int _cachedWidth;
	private int _cachedHeight;

	[BurstCompile]
	protected override void OnCreate()
	{
		RequireForUpdate<GridComponent>();
		RequireForUpdate<ColorArrayComponent>();
		RequireForUpdate<InstanceRendererComponent>();
	}

	[BurstCompile]
	protected override void OnDestroy()
	{
		if (_matrices.IsCreated)
		{
			_matrices.Dispose();
		}
	}

	[BurstCompile]
	protected override void OnStartRunning()
	{
		if (!_matrices.IsCreated)
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
			if (_matrices.IsCreated)
			{
				_matrices.Dispose();
			}
			Initialize(grid);
		}

		// update graphics buffer with computed colors
		// coloration rest will be done shader-side

		_buffer.SetData(SystemAPI.GetSingleton<ColorArrayComponent>().Colors);
		_material.SetBuffer("_ColorBuffer", _buffer);

		for (int i = 0; i < _matrices.Length; i += RenderBatchSize)
		{
			// specify batch offset so it can be retrieved shader-side
			_materialPropertyBlock.SetInteger("_InstanceIDOffset", i);
			Graphics.RenderMeshInstanced(_renderParams, _mesh, 0, _matrices, math.min(_matrices.Length - i, RenderBatchSize), i);
		}
	}

	private void Initialize(GridComponent grid)
	{
		int length = grid.Width * grid.Height;
		_matrices = new NativeArray<Matrix4x4>(length, Allocator.Persistent);

		float spacing = 1f + SystemAPI.GetSingleton<InstanceRendererComponent>().CellSpacing;
		float xOffset = grid.Width * spacing / 2f;
		float yOffset = grid.Height * spacing / 2f;

		for (int i = 0; i < length; i++)
		{
			int x = i % grid.Width;
			int y = i / grid.Width;
			_matrices[i] = Matrix4x4.TRS(new Vector3(x * spacing - xOffset, y * spacing - yOffset, 0f), quaternion.identity, Vector3.one);
		}

		_mesh = ManagedData.Instance.QuadMesh;
		_material = ManagedData.Instance.InstanceMaterial;
		_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, sizeof(float) * 4);
		_materialPropertyBlock = new MaterialPropertyBlock();
		_renderParams = new RenderParams(_material) { matProps = _materialPropertyBlock };

		// to detect changes
		_cachedWidth = grid.Width;
		_cachedHeight = grid.Height;
	}
}