using Godot;
using System;

public partial class AspectControl : Control
{
	[Export]
	public Vector2 resolution;

	public Control main;
	public ShaderMaterial material;

	public override void _Ready()
	{
		GetTree().Root.MinSize = new Vector2I(100,100);
		main = GetTree().Root.GetChild<Control>(0);
		material = (ShaderMaterial)GetNode<Control>("%RainbowBackground").Material;
	}


	public override void _Process(double delta)
	{
		float scaleFactor = Mathf.Min(main.Size.X/resolution.X, main.Size.Y/resolution.Y);
		Scale = new Vector2(scaleFactor, scaleFactor);
		Size = main.Size/scaleFactor;
		Position = new Vector2(0,0);
		material.SetShaderParameter("resolution", main.Size);
	}
}
