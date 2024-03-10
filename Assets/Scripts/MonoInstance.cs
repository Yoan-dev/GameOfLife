using UnityEngine;

public class MonoInstance : MonoBehaviour
{
	public static MonoInstance Instance;

	public Mesh QuadMesh;
	public Material InstanceMaterial;
	public Shader GridShader;

	public void Awake()
	{
		// please don't judge me
		Instance = this;
	}
}