using Godot;
using System;

public partial class ScrollbarSpacer : Control
{
	[Export]
	private ScrollContainer scrollContainer;

	private VScrollBar scrollbar;

	public override void _Ready()
	{
		scrollbar = scrollContainer.GetVScrollBar();
		scrollbar.VisibilityChanged += Resize;
		Resize();
	}

	private void Resize()
	{
		if(scrollbar.Visible)
		{
			CustomMinimumSize = Vector2.Zero;
			Size = Vector2.Zero;
		}
		else
		{
			CustomMinimumSize = new Vector2(scrollbar.Size.X, 0);
			Size = new Vector2(scrollbar.Size.X, 0);
		}
	}
}
