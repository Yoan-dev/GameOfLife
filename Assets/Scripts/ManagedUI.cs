using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ManagedUI : MonoBehaviour
{
	public static ManagedUI Instance;

	public Dropdown BlueprintDropdown;

    public void Awake()
    {
		Instance = this;
	}

	public void Start()
	{
		List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
		for (int i = 0; i < ManagedData.Instance.Blueprints.Length; i++)
		{
			options.Add(new Dropdown.OptionData(ManagedData.Instance.Blueprints[i].Name));
		}
		BlueprintDropdown.AddOptions(options);
	}

	public int GetBlueprintIndex()
	{
		return BlueprintDropdown.value;
	}
}
