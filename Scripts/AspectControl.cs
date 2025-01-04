using Godot;
using System;

public partial class AspectControl : Control
{
	public Control main;
	public Vector2 BaseSize;
	public PopupMenu MusicDropdown;
	public Control SpeedDropdown;
	public ShaderMaterial material;

	public override void _Ready()
	{
		MusicDropdown = GetNode<OptionButton>("%PlaybackTypeOptions").GetPopup();
		MusicDropdown.Unresizable = false;
		MusicDropdown.Borderless = false;
		GD.Print(MusicDropdown.ContentScaleAspect);
		MusicDropdown.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
		MusicDropdown.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
		GD.Print(MusicDropdown.ContentScaleAspect);
		/*
		GetTree().Root.MinSize = new Vector2I(100,100);
		main = GetTree().Root.GetChild<Control>(0);
		material = (ShaderMaterial)GetNode<Control>("%RainbowBackground").Material;
		main.Resized += OnResize;
		*/
	}

    public override void _Process(double delta)
    {
		GD.Print(MusicDropdown.ContentScaleAspect);
    }

    public void OnResize()
	{
	}
}
