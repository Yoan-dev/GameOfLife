using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public enum RenderType
{
	None = 0,
	Texture,
	Instances,
}

[DisallowMultipleComponent]
public class GridAuthoring : MonoBehaviour
{
	public RenderType RenderType;
	public int Width;
	public int Height;
	public float CellSpacing;

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
			AddComponent(entity, new GridInitComponent());

			if (authoring.RenderType == RenderType.Texture)
			{
				AddComponent(entity, new TextureRendererComponent());
			}
			else if (authoring.RenderType == RenderType.Instances)
			{
				AddComponent(entity, new InstanceRendererComponent { CellSpacing = authoring.CellSpacing });
			}
		}
	}
}