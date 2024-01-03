using Godot;
using System;

public partial class AlbumButton : Control
{
	public TextureRect albumCover;
	public Button selectAlbumButton;

	public override void _Ready()
	{
		albumCover = GetNode<TextureRect>("%AlbumCover");
		selectAlbumButton = GetNode<Button>("%SelectAlbumButton");
	}

	public void Initialize(Album album)
	{
		selectAlbumButton.Pressed += ()=>{Main.Instance.SetPlaylist(album);};
		
		Image img = new Image();
		ImageTexture tex = new ImageTexture();
		switch(album.albumCoverType)
		{
			case "image/png":
				img.LoadPngFromBuffer(album.albumCover);
			break;
			case "image/jpeg":
				img.LoadJpgFromBuffer(album.albumCover);
			break;
			default:
				return;
		}
		tex.SetImage(img);
		albumCover.Texture = tex;
	}
}
