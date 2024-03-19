using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public static UIManager Instance;

	public Dropdown BlueprintDropdown;
	public Button ResetButton;
	public InputField WidthInputField;
	public InputField HeightInputField;

	private bool _resetPressed;

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

		ResetButton.onClick.AddListener(() => { _resetPressed = true; });
	}

	public int GetBlueprintIndex()
	{
		return BlueprintDropdown.value;
	}

	public bool HasResetBeenPressed()
	{
		if (_resetPressed)
		{
			// consume
			_resetPressed = false;

			return true;
		}

		return false;
	}

	public int GetWidthInput()
	{
		return GetIntInput(WidthInputField);
	}

	public int GetHeightInput()
	{
		return GetIntInput(HeightInputField);
	}

	private int GetIntInput(InputField field)
	{
		int res;
		int.TryParse(field.text, out res);
		return res;
	}
}
