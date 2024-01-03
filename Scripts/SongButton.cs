using Godot;
using System;

public partial class SongButton : Button
{
	public Label songNumber;
	public ScrollingText songName;
	public ScrollingText songArtist;
	public Label songLength;
	
	public override void _Ready()
	{
		songNumber = GetNode<Label>("%SongNumber");
		songName = GetNode<ScrollingText>("%SongName");
		songArtist = GetNode<ScrollingText>("%SongArtist");
		songLength = GetNode<Label>("%SongLength");
	}

	public void Initialize(Playlist playlist, int index)
	{
		Pressed += ()=>{Main.Instance.PlaySong(playlist, index);};
		songNumber.Text = (index + 1).ToString();
		Song song = playlist.songs[index];
		songName.UpdateText(song.songName);
		songArtist.UpdateText(song.artistName);
		songLength.Text = song.GetLengthMinutesSeconds();
	}
}
