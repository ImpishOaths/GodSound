using Godot;
using Godot.Collections;

[Tool]
public partial class CycleButton : Button
{
	[Export]
	private string[] labels;
	private int _currentIndex = 0;
	[Export]
	public int CurrentIndex {get {return _currentIndex;} private set {_currentIndex = value; Update();}}

	[Signal]
	public delegate void SelectLabelEventHandler(int index);

	public override void _Ready()
	{
		Pressed += OnPressed;
	}

	public void OnPressed()
	{
		_currentIndex += 1;
		if(_currentIndex >= labels.Length)
			_currentIndex = 0;
		EmitSignal("SelectLabel", CurrentIndex);
		Update();
	}

	public void Update()
	{
		Text = labels[_currentIndex];
	}

	public void SetIndex(int index, bool signal = false)
	{
		_currentIndex = index;
		if(signal)
			EmitSignal("SelectLabel",CurrentIndex);
	}
}
