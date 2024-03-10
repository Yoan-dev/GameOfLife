using Unity.Entities;
using UnityEngine;

public enum GridType
{
	Array = 0,
	Entities,
}

public enum RenderType
{
	Texture = 0,
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
			else
			{
				AddComponent(entity, new EntityGridComponent());
			}

			if (authoring.RenderType == RenderType.Texture)
			{
				AddComponent(entity, new TextureRendererComponent());
			}
			else
			{
				AddComponent(entity, new InstanceRendererComponent());
			}
		}
	}
}