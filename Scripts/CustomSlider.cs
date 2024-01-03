using Godot;
using System;

public partial class CustomSlider : Control
{
	[Export]
	private float value = 0.5f;

	[Signal]
	public delegate void ValueChangedEventHandler(float value);

	private Control leftFill;
	private Control rightFill;
	private bool grabbed;

	public override void _Ready()
	{
		leftFill = GetNode<Control>("WhiteFill/LeftFill");
		rightFill = GetNode<Control>("WhiteFill/RightFill");
		SetScrollValue(value);
	}

	public override void _Process(double delta)
	{
		if(grabbed)
		{
			Vector2 mouse = GetViewport().GetMousePosition();
			float mouseX = mouse.X;
			float globalScale = GetGlobalTransform().Scale.X;
			float globalPos = GlobalPosition.X;
			float value = (mouseX-globalPos-10*globalScale)/((Size.X-20)*globalScale);
			InputScrollValue(value);
		}
	}

	public void SetScrollValue(float value, bool OverrideGrab = false)
	{
		if(grabbed && !OverrideGrab)
			return;
		this.value = Mathf.Clamp(value, 0f, 1f);
		leftFill.SizeFlagsStretchRatio = this.value;
		rightFill.SizeFlagsStretchRatio = 1f - this.value;
	}

	public void InputScrollValue(float value)
	{
		SetScrollValue(value, true);
		EmitSignal("ValueChanged", this.value);
	}

	public float GetValue()
	{
		return value;
	}

	private void GrabberDown()
	{
		grabbed = true;
	}

	private void GrabberUp()
	{
		grabbed = false;
	}
}
