using Godot;
using System;

public partial class AddFolderWindow : Window
{
	private LineEdit folderPath;
	private CheckBox openRecursive;
	private CheckBox audiobook;

	public override void _Ready()
	{
		folderPath = GetNode<LineEdit>("%Path");
		openRecursive = GetNode<CheckBox>("%OpenRecursive");
		audiobook = GetNode<CheckBox>("%Audiobook");

	}

	public void OkayButton()
	{
		Main.Instance.AddFolder(folderPath.Text, openRecursive.ButtonPressed, audiobook.ButtonPressed);
		Reset();
	}

	public void CancelButton()
	{
		Reset();
	}

	public void PathSet(string path)
	{
		folderPath.Text = path;
	}

	private void Reset()
	{
		folderPath.Text = "";
		openRecursive.ButtonPressed = false;
		audiobook.ButtonPressed = false;
		Hide();
	}
}
