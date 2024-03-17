using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum RenderType
{
	None = 0,
	Texture,
	Instances,
}

[Serializable]
public struct BlueprintManagedData
{
	public int2[] Cells;
}

[DisallowMultipleComponent]
public class GridAuthoring : MonoBehaviour
{
	public RenderType RenderType;
	public int Width;
	public int Height;
	public float CellSpacing;
	public BlueprintManagedData[] Blueprints = new BlueprintManagedData[0];

	public class Baker : Baker<GridAuthoring>
	{
		public override void Bake(GridAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);

			float spacing = 1f + (authoring.RenderType == RenderType.Texture ? 0f : authoring.CellSpacing);

			AddComponent(entity, new GridComponent
			{
				Width = authoring.Width,
				Height = authoring.Height,
				MaxBounds = new float2(authoring.Width * spacing / 2f, authoring.Height * spacing / 2f),
				MinBounds = new float2(-authoring.Width * spacing / 2f, -authoring.Height * spacing / 2f),
			});

			if (authoring.RenderType == RenderType.Texture)
			{
				AddComponent(entity, new TextureRendererComponent());
			}
			else if (authoring.RenderType == RenderType.Instances)
			{
				AddComponent(entity, new InstanceRendererComponent { CellSpacing = authoring.CellSpacing });
			}

			var builder = new BlobBuilder(Allocator.Temp);

			ref BlueprintCollection blueprintCollection = ref builder.ConstructRoot<BlueprintCollection>();
			
			int blueprintCount = authoring.Blueprints.Length;
			BlobBuilderArray<BlueprintData> blueprintArrayBuilder = builder.Allocate(ref blueprintCollection.Blueprints, blueprintCount);
			for (int i = 0; i < blueprintCount; i++)
			{
				BlueprintManagedData blueprintManagedData = authoring.Blueprints[i];
				blueprintArrayBuilder[i] = new BlueprintData();

				int cellsCount = blueprintManagedData.Cells.Length;
				BlobBuilderArray<int2> cellArrayBuilder = builder.Allocate(ref blueprintArrayBuilder[i].Cells, cellsCount);
				for (int j = 0; j < cellsCount; j++)
				{
					cellArrayBuilder[j] = blueprintManagedData.Cells[j];
				}
			}

			var blueprintCollectionReference = builder.CreateBlobAssetReference<BlueprintCollection>(Allocator.Persistent);
			AddBlobAsset(ref blueprintCollectionReference, out var hash);
			builder.Dispose();

			AddComponent(entity, new BlueprintCollectionRef
			{
				Collection = blueprintCollectionReference,
			});
			AddComponent(entity, new BlueprintComponent
			{
				BlueprintId = 0,
			});
			AddBuffer<BlueprintEventBufferElement>(entity);
		}
	}
}