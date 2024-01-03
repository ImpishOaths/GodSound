using Godot;
using System;

public partial class BackgroundOptionWindow : Window
{
	private OptionButton backgroundStyle;
	private CheckBox hideUIToggle;
	private ColorPickerButton flatColor;

	private bool initialized = false;

    public override void _Ready()
    {
		backgroundStyle = GetNode<OptionButton>("%BackgroundStyle");
		hideUIToggle = GetNode<CheckBox>("%HideUI");
		flatColor = GetNode<ColorPickerButton>("%FlatColor");
    }

    public void CloseButton()
	{
		Hide();
	}

	public override void _Process(double delta)
	{
		if(initialized == false)
		{
			ReflectSettings(Main.Instance.ProgramSettings);
			initialized = true;
		}
	}

	public void ReflectSettings(Settings settings)
	{
		backgroundStyle.Selected = (int)settings.backgroundStyle;
		hideUIToggle.ButtonPressed = settings.hideUI;
		flatColor.Color = settings.flatColor;
	}

	public void ChangeBackground(int style)
	{
		Main.Instance.ProgramSettings.backgroundStyle = (Settings.BackgroundStyle)style;
		Main.Instance.ReflectSettings();
	}

	public void ChangeHideUI(bool toggle)
	{
		Main.Instance.ProgramSettings.hideUI = toggle;
		Main.Instance.ReflectSettings();
	}

	public void ChangeFlatColor(Color color)
	{
		Main.Instance.ProgramSettings.flatColor = color;
		Main.Instance.ReflectSettings();
	}
}
