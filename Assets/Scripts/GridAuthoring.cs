using Unity.Entities;
using UnityEngine;

public enum GridType
{
	Array = 0,
	Entities,
}

public enum RenderType
{
	None = 0,
	Texture,
	Instances,
}

[DisallowMultipleComponent]
public class GridAuthoring : MonoBehaviour
{
	public GridType GridType;
	public RenderType RenderType;
	public GridComponent Grid;
	
	public class Baker : Baker<GridAuthoring>
	{
		public override void Bake(GridAuthoring authoring)
		{
			Entity entity = GetEntity(TransformUsageFlags.Dynamic);

			AddComponent(entity, in authoring.Grid);

			if (authoring.GridType == GridType.Array)
			{
				AddComponent(entity, new ArrayGridInitComponent());
			}
			else if (authoring.GridType == GridType.Entities)
			{
				AddComponent(entity, new EntityGridComponent());
			}

			if (authoring.RenderType == RenderType.Texture)
			{
				AddComponent(entity, new TextureRendererComponent());
			}
			else if (authoring.RenderType == RenderType.Instances)
			{
				AddComponent(entity, new InstanceRendererComponent());
			}
		}
	}
}