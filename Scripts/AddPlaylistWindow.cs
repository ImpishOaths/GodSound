using Godot;
using System;

public partial class AddPlaylistWindow : Window
{
	private LineEdit playlistName;

	public override void _Ready()
	{
		playlistName = GetNode<LineEdit>("%PlaylistName");
	}

	public void OkayButton()
	{
		Main.Instance.NewPlaylist(playlistName.Text);
		Reset();
	}

	public void CancelButton()
	{
		Reset();
	}

	private void Reset()
	{
		playlistName.Text = "";
		Hide();
	}
}
