using Godot;
using System;

public partial class AwakeControl : Node
{
	private AnimationPlayer FadeAnimator;
	private bool IsAwake = true;
	public bool AlwaysAwake;
	
	public override void _Ready()
	{
		FadeAnimator = GetNode<AnimationPlayer>("%FadeAnimator");
	}

    public override void _Notification(int what)
    {
		if(AlwaysAwake && IsAwake)
			return;
		if(what == MainLoop.NotificationApplicationFocusIn)
		{
			IsAwake = true;
			FadeAnimator.Play("FadeIn");
		}
		else if(what == MainLoop.NotificationApplicationFocusOut)
		{
			IsAwake = false;
			FadeAnimator.Play("FadeOut");
		}
    }
}
