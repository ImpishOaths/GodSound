using Godot;
using System;

public partial class AlbumButton : Control
{
	public TextureRect albumCover;
	public Button selectAlbumButton;
	public bool MouseOver;

	public override void _Ready()
	{
		albumCover = GetNode<TextureRect>("%AlbumCover");
		selectAlbumButton = GetNode<Button>("%SelectAlbumButton");
	}

	public void Initialize(Album album)
	{
		selectAlbumButton.Pressed += ()=>{Main.Instance.SetPlaylist(album);};
		selectAlbumButton.MouseEntered += ()=>{MouseOver = true;};
		selectAlbumButton.MouseExited += ()=>{MouseOver = false;};
		
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

    public override void _Process(double delta)
    {
		if(MouseOver == false)
			return;
		
		if(Input.IsMouseButtonPressed(MouseButton.Left))
		{
			//GD.Print("hi");
		}
    }
}
