using Godot;

public partial class PlaylistButton : Button
{
	public Playlist playlist;
	public Label songCount;
	public ScrollingText playlistName;

	public override void _Ready()
	{
		songCount = GetNode<Label>("%SongCount");
		playlistName = GetNode<ScrollingText>("%PlaylistName");
	}

	public void Initialize(Playlist playlist)
	{
		this.playlist = playlist;
		Pressed += ()=>{Main.Instance.SetPlaylist(playlist);};
		songCount.Text = playlist.songs.Count.ToString();
		playlistName.UpdateText(playlist.playlistName);
	}

	private void RefreshPlaylistVisuals()
	{
		songCount.Text = playlist.songs.Count.ToString();
	}

	public void AddSongPressed()
	{
		if(playlist.songs.Contains(Main.Instance.PlayingSong) == false)
			playlist.songs.Add(Main.Instance.PlayingSong);
		RefreshPlaylistVisuals();
		Main.Instance.BufferSavePlaylist.Add(playlist.playlistName);
	}

	public void RemoveSongPressed()
	{
		playlist.songs.Remove(Main.Instance.PlayingSong);
		if(playlist == Main.Instance.SelectedPlaylist)
			Main.Instance.SetPlaylist(playlist);
		RefreshPlaylistVisuals();
		Main.Instance.BufferSavePlaylist.Add(playlist.playlistName);
	}

	public void AddAlbumPressed()
	{
		foreach(var song in Main.Instance.SelectedPlaylist.songs)
			if(playlist.songs.Contains(song) == false)
				playlist.songs.Add(song);
		RefreshPlaylistVisuals();
		Main.Instance.BufferSavePlaylist.Add(playlist.playlistName);
	}

	public void RemoveAlbumPressed()
	{
		if(playlist == Main.Instance.SelectedPlaylist)
			return;
		foreach(var song in Main.Instance.SelectedPlaylist.songs)
			playlist.songs.Remove(song);
		RefreshPlaylistVisuals();
		Main.Instance.BufferSavePlaylist.Add(playlist.playlistName);
	}
}
