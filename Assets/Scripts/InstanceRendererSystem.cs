using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct InstanceRendererComponent : IComponentData
{
}

[BurstCompile]
[UpdateAfter(typeof(BlueprintSystem))]
public partial class InstanceRendererSystem : SystemBase
{
	private const int RenderBatchSize = 454;
	private const float CellSpacing = 1.1f;

	private NativeArray<Matrix4x4> _matrices;
	private Mesh _mesh;
	private Material _material;
	private GraphicsBuffer _buffer;
	private MaterialPropertyBlock _materialPropertyBlock;
	private RenderParams _renderParams;

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

			GridComponent grid = SystemAPI.GetSingleton<GridComponent>();

			int length = grid.Width * grid.Height;
			_matrices = new NativeArray<Matrix4x4>(length, Allocator.Persistent);

			float xOffset = grid.Width * CellSpacing / 2f;
			float yOffset = grid.Height * CellSpacing / 2f;

			for (int i = 0; i < length; i++)
			{
				int x = i % grid.Width;
				int y = i / grid.Width;
				_matrices[i] = Matrix4x4.TRS(new Vector3(x * CellSpacing - xOffset, y * CellSpacing - yOffset, 0f), quaternion.identity, Vector3.one);
			}

			_mesh = MonoInstance.Instance.QuadMesh;
			_material = MonoInstance.Instance.InstanceMaterial;
			_buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, sizeof(float) * 4);
			_materialPropertyBlock = new MaterialPropertyBlock();
			_renderParams = new RenderParams(_material) { matProps = _materialPropertyBlock };
		}
	}

	[BurstCompile]
	protected override void OnUpdate()
	{
		Dependency.Complete();

		_buffer.SetData(SystemAPI.GetSingleton<ColorArrayComponent>().Colors);
		_material.SetBuffer("_ColorBuffer", _buffer);

		for (int i = 0; i < _matrices.Length; i += RenderBatchSize)
		{
			_materialPropertyBlock.SetInteger("_InstanceIDOffset", i);
			Graphics.RenderMeshInstanced(_renderParams, _mesh, 0, _matrices, math.min(_matrices.Length - i, RenderBatchSize), i);
		}
	}
}