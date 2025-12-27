using Godot;
using System;

public partial class ScrollingText : Control
{
	[Export]
	public string text {get; private set;}
	[Export]
	private double scrollSpeed;
	[Export]
	private bool leftAlign = true;
	[Export]
	private double scrollThreshhold = 30;
	[Export]
	private double holdTime = 1;

	private enum State {START, SCROLL, HOLD, FADE_OUT, FADE_IN}
	private State currentState;
	private double timer;
	public Label label;
	public double scrollDistance;
	public float yVal;

	public override void _Ready()
	{
		label = GetNode<Label>("Label");
		UpdateText(text);
		label.Position = new Vector2(0, yVal);
	}

	public override void _Process(double delta)
	{
		if(scrollDistance <= scrollThreshhold)
		{
			return;
		}
		label.HorizontalAlignment = HorizontalAlignment.Left;
		timer += delta;
		switch(currentState)
		{
			case State.START:
				if(timer >= holdTime)
				{
					currentState = State.SCROLL;
					timer = 0;
				}
				break;
			case State.SCROLL:
				label.Position = new Vector2((float)(-timer*scrollSpeed), yVal);
				if(timer >= scrollDistance/scrollSpeed)
				{
					currentState = State.HOLD;
					timer = 0;
				}
				break;
			case State.HOLD:
				if(timer >= holdTime)
				{
					currentState = State.FADE_OUT;
					timer = 0;
				}
				break;
			case State.FADE_OUT:
				label.Modulate = new Color(1, 1, 1, (float)(1 - timer/0.5));
				if(timer >= 0.5)
				{
					label.Modulate = new Color(1, 1, 1, 0);
					currentState = State.FADE_IN;
					label.Position = new Vector2(0, yVal);
					timer = 0;
				}
				break;
			case State.FADE_IN:
				label.Modulate = new Color(1, 1, 1, (float)(timer/0.5));
				if(timer >= 0.5)
				{
					label.Modulate = Colors.White;
					currentState = State.START;
					timer = 0;
				}
				break;
		}
	}

	public void UpdateText(string newText)
	{
		text = newText;
		label.Text = newText;
		label.SetSize(label.GetMinimumSize());
		float labelSize = label.GetMinimumSize().X;
		float boxSize = Size.X;
		scrollDistance = labelSize - boxSize;
		yVal = (Size.Y - label.Size.Y)/2;

		label.Position = new Vector2(0, yVal);

		if(scrollDistance <= scrollThreshhold)
		{
			timer = 0;
			currentState = State.HOLD;
			label.Modulate = Colors.White;
			if(leftAlign)
			{
				label.HorizontalAlignment = HorizontalAlignment.Left;
				label.Position = new Vector2(0, yVal);
			}
			else
			{
				label.HorizontalAlignment = HorizontalAlignment.Right;
				label.Position = new Vector2(-(float)scrollDistance, yVal);
			}
		}
	}

	public void OnResized()
	{
		UpdateText(text);
	}
}
