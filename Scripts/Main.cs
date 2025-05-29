using Godot;
using System.IO;

using NAudio.Wave;

using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System;
using SharpHook;
using SharpHook.Native;

public partial class Main : Control
{
	private static Main instance;
	public static Main Instance {get{return instance;}}

	[Export]
	public PackedScene AlbumButtonScene;
	[Export]
	public PackedScene PlaylistButtonScene;
	[Export]
	public PackedScene SongButtonScene;
	[Export]
	public Texture2D PauseButtonIcon;
	[Export]
	public Texture2D PlayButtonIcon;
	[Export]
	public string LibraryPath = "user://library/";
	[Export]
	public string SettingsPath = "user://settings.json";

	private Control PlaylistContainer;
	private Control AlbumGrid;
	private Control AudiobookGrid;
	private Control SongList;
	private ScrollingText AlbumName;
	private ScrollingText ArtistName;
	private Button PlayButton;
	private bool Playing = false;
	private Label CurrentTime;
	private Label TotalTime;
	private CustomSlider TimeSlider;
	private ScrollingText SongNameLabel;
	private ColorRect RainbowBackground;
	private ColorRect FlatBackground;
	private CycleButton PlaybackType;
	private CycleButton PlaybackSpeed;
	private Control SetBookmark;
	private ScrollingText BookmarkLabel;

	private AwakeControl AwakeControl;
	public Settings ProgramSettings = new Settings();

	private Queue<AddFolderArguments> AddFolderQueue = new Queue<AddFolderArguments>();
	public readonly Dictionary<string, LibraryObject> Library = [];
	public SortedSet<string> LoadAlbumQueue = [];
	public List<string> BufferSavePlaylist = [];
	public bool seeking = false;

	private AudioFileReader Reader;
	private WaveOutEvent WavePlayer;
	private SonicSampleProvider SpeedControl;
	public QueueSampleProvider SampleQueue;
	public bool PlayingAudiobook = false;
	public Bookmark ActiveBookmark = null;
	public Bookmark VisualBookmark = null;

	public Song PlayingSong {get; private set;}
	public Playlist PlayingPlaylist {get; private set;}
	public int PlayingIndex {get; private set;}
	public Playlist SelectedPlaylist {get; private set;}

	public enum PlaybackMode {STOP, LOOP, LOOP1, SHUFFLE}
	public float[] SpeedOptions = {0.5f, 1.0f, 2.0f, 3.0f};
	public PlaybackMode CurrentMode = PlaybackMode.STOP;
	public Queue<int> ShuffleList = new Queue<int>();
	public Random random;

	public (Playlist, int, bool)? DefferedSongUpdate = null;

	private JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects };

	private SimpleGlobalHook hook;
	private bool scheduleTogglePlay = false;
	private bool scheduleNext = false;

	public override async void _Ready()
	{
		PlaylistContainer = GetNode<Control>("%PlaylistContainer");
		AlbumGrid = GetNode<Control>("%AlbumGrid");
		AudiobookGrid = GetNode<Control>("%AudiobookGrid");
		SongList = GetNode<Control>("%SongList");
		AlbumName = GetNode<ScrollingText>("%AlbumName");
		ArtistName = GetNode<ScrollingText>("%ArtistName");
		PlayButton = GetNode<Button>("%PlayButton");
		CurrentTime = GetNode<Label>("%CurrentTime");
		TotalTime = GetNode<Label>("%TotalTime");
		TimeSlider = GetNode<CustomSlider>("%TimeSlider");
		SongNameLabel = GetNode<ScrollingText>("%NameLabel");
		AwakeControl = GetNode<AwakeControl>("%AwakeControl");
		RainbowBackground = GetNode<ColorRect>("%RainbowBackground");
		FlatBackground = GetNode<ColorRect>("%FlatBackground");
		PlaybackType = GetNode<CycleButton>("%PlaybackTypeOptions");
		PlaybackSpeed = GetNode<CycleButton>("%PlaybackSpeedOptions");
		SetBookmark = GetNode<Control>("%SetBookmark");
		BookmarkLabel = GetNode<ScrollingText>("%BookmarkLabel");
		WavePlayer = new WaveOutEvent();
		random = new Random();
		SetVolume(1);

		SampleQueue = new QueueSampleProvider();
		SampleQueue.OnEmpty += ()=>{NextSong();};

		SpeedControl = new SonicSampleProvider();
		SpeedControl.OnStopped += ()=>{NextChapter();};

		instance = this;
		PreloadLibrary();
		if(LoadSettings() == false)
			SaveSettings();
		ReflectSettings();

		GetTree().AutoAcceptQuit = false;

		hook = new SimpleGlobalHook();
		hook.KeyPressed += (sender, e) => {
			if(e.Data.KeyCode ==  KeyCode.VcPause)
				scheduleTogglePlay = true;
			if(e.Data.KeyCode ==  KeyCode.VcEnd)
				scheduleNext = true;
		};
		await hook.RunAsync();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			hook.Dispose();
			SaveSettings();
			if(PlayingPlaylist != null && PlayingPlaylist.GetPlaylistType() == Playlist.Type.AUDIOBOOK && ActiveBookmark != null)
				WriteBookmark(((Audiobook)PlayingPlaylist).bookmarkPath, ActiveBookmark);
			GetTree().Quit();
		}
	}

	public void SelectSong(Playlist playlist, int index)
	{
		PlayingPlaylist = playlist;
		PlayingIndex = index;
		PlayingSong = playlist.songs[index];
		SongNameLabel.UpdateText(PlayingSong.songName);
		TotalTime.Text = PlayingSong.GetLengthMinutesSeconds();
		UpdateTimeTracker();
	}

	public double GetCurrentSongTimeRatio()
	{
		double time;
		if(seeking)
			time = TimeSlider.GetValue();
		else
		{
			if(PlayingAudiobook)
				time = (double)Reader.Position / Reader.Length;
			else
				time = (double)SampleQueue.position / SampleQueue.length;
		}
		return time;
	}

	public void UpdateTimeTracker()
	{
		double time = GetCurrentSongTimeRatio();
		CurrentTime.Text = Song.GetLengthMinutesSeconds(PlayingSong.songLength * time);
		TimeSlider.SetScrollValue((float)time);
	}

	public void SetPlaybackMode(int mode)
	{
		CurrentMode = (PlaybackMode)mode;
		ProgramSettings.playbackMode = CurrentMode;
		PlaybackType.SetIndex(mode);
		if(CurrentMode != PlaybackMode.SHUFFLE)
			ShuffleList = new Queue<int>();
	}

	public int GetNextShuffledIndex(Playlist playlist)
	{
		if(ShuffleList.Count == 0)
		{
			ResetShuffle(playlist);
		}
		return ShuffleList.Dequeue();
	}

	public void PlayAlbum()
	{
		if(SelectedPlaylist.GetPlaylistType() == Playlist.Type.AUDIOBOOK)
		{
			PlaySong(SelectedPlaylist, VisualBookmark.chapterIndex, (float)VisualBookmark.chapterPositionRatio);
		}
		else if(CurrentMode == PlaybackMode.SHUFFLE)
		{
			ResetShuffle(SelectedPlaylist);
			int index = GetNextShuffledIndex(SelectedPlaylist);
			PlaySong(SelectedPlaylist, index, 0, false);
		}
		else
			PlaySong(SelectedPlaylist, 0);
	}

	public void NextChapter()
	{
		if(PlayingAudiobook == false)
			return;
		(Playlist, int) songChoice = (null, -1);
		if(PlayingIndex + 1 < PlayingPlaylist.songs.Count)
			songChoice = (PlayingPlaylist, PlayingIndex + 1);
		else
			return;
		DefferedSongUpdate = (songChoice.Item1, songChoice.Item2, true);
		return;
	}

	public void UpdateBookmarkVisual()
	{
		BookmarkLabel.UpdateText(VisualBookmark.ToString());
	}

	public void NextSong(bool forced = false)
	{
		if(PlayingAudiobook)
			return;
		(Playlist, int) songChoice = (null, -1);
		switch(CurrentMode)
		{
			case PlaybackMode.STOP:
				if(PlayingIndex + 1 < PlayingPlaylist.songs.Count)
					songChoice = (PlayingPlaylist, PlayingIndex + 1);
				else
					return;
				break;
			case PlaybackMode.LOOP:
				if(PlayingIndex + 1 < PlayingPlaylist.songs.Count)
					songChoice = (PlayingPlaylist, PlayingIndex + 1);
				else
					songChoice = (PlayingPlaylist, 0);
				break;
			case PlaybackMode.LOOP1:
				songChoice = (PlayingPlaylist, PlayingIndex);
				break;
			case PlaybackMode.SHUFFLE:
				int index = GetNextShuffledIndex(PlayingPlaylist);
				songChoice = (PlayingPlaylist, index);
				break;
		}
		if(forced)
		{
			bool reshuffle = CurrentMode != PlaybackMode.SHUFFLE;
			PlaySong(songChoice.Item1, songChoice.Item2, 0, reshuffle);
		}
		else
		{
			QueueNextSong(songChoice.Item1, songChoice.Item2);
		}
	}

	public void StartSeeking()
	{
		seeking = true;
	}

	public void SeekTime()
	{
		bool wasPlaying = Playing;
		seeking = false;
		SampleQueue.position = (long)(SampleQueue.length * TimeSlider.GetValue());
		PlaySong(PlayingPlaylist, PlayingIndex, TimeSlider.GetValue(), false);
		if(wasPlaying == false)
			TogglePlay();
	}

	public void ResetShuffle(Playlist playlist, int toRemove = -1)
	{
		ShuffleList = new Queue<int>();
		List<int> tempList = new List<int>(Enumerable.Range(0, playlist.songs.Count));
		for(int i = 0; i < tempList.Count; ++i)
		{
			int swapIndex1 = i;
			int swapIndex2 = random.Next(tempList.Count);
			int swapVal1 = tempList[swapIndex1];
			tempList[swapIndex1] = tempList[swapIndex2];
			tempList[swapIndex2] = swapVal1;
		}
		for(int i = 0; i < tempList.Count; ++i)
		{
			if(tempList[i] == toRemove)
				continue;
			ShuffleList.Enqueue(tempList[i]);
		}
	}

	public void QueueNextSong(Playlist playlist, int index)
	{
		Reader = new AudioFileReader(playlist.songs[index].filePath);
		if(SampleQueue.WaveFormat.Equals(Reader.WaveFormat))
		{
			SampleQueue.Enqueue(Reader);
			DefferedSongUpdate = (playlist, index, false);
		}
		else
		{
			DefferedSongUpdate = (playlist, index, true);
		}
	}

	public void PlaySong(Playlist playlist, int index, float seekRatio = 0, bool resetShuffle = true)
	{
		if(PlayingPlaylist != null && PlayingPlaylist.GetPlaylistType() == Playlist.Type.AUDIOBOOK && ActiveBookmark != null)
		{
			WriteBookmark(((Audiobook)PlayingPlaylist).bookmarkPath, ActiveBookmark);
		}
		if(playlist.GetPlaylistType() == Playlist.Type.AUDIOBOOK)
		{
			PlayAudiobook(playlist, index, seekRatio);
			return;
		}
		Playing = false;
		PlayingAudiobook = false;
		WavePlayer.Stop();

		if(CurrentMode == PlaybackMode.SHUFFLE && resetShuffle)
			ResetShuffle(playlist, index);
		
		var Reader = new AudioFileReader(playlist.songs[index].filePath);
		var seek = (long)((double)Reader.Length * seekRatio);
		Reader.Seek(seek, SeekOrigin.Current);
		SampleQueue.Init(Reader.WaveFormat, seek);
		SampleQueue.Enqueue(Reader);

		WavePlayer.Init(SampleQueue);
		SelectSong(playlist, index);
		TogglePlay();
		ProgramSettings.playlist = playlist.playlistName;
		ProgramSettings.song = index;
	}

	public void PlayAudiobook(Playlist playlist, int index, float seekRatio)
	{
		ActiveBookmark = VisualBookmark;
		Playing = false;
		PlayingAudiobook = true;
		WavePlayer.Stop();

		Reader = new AudioFileReader(playlist.songs[index].filePath);
		var seek = (long)((double)Reader.Length * seekRatio);
		Reader.Seek(seek, SeekOrigin.Current);
		SpeedControl.Init(Reader, seek);
		SetSpeedPlayback(PlaybackSpeed.CurrentIndex);

		WavePlayer.Init(SpeedControl);
		SelectSong(playlist, index);
		TogglePlay();
	}

	public void SetVolume(float value)
	{
		if(WavePlayer != null)
		{
			WavePlayer.Volume = value * value;
		}
	}

	public void TogglePlay()
	{
		if(Playing)
		{
			Playing = false;
			PlayButton.Icon = PlayButtonIcon;
			WavePlayer.Pause();
		}
		else
		{
			Playing = true;
			PlayButton.Icon = PauseButtonIcon;
			WavePlayer.Play();
		}
	}

	public void SetPlaylist(Playlist playlist)
	{
		SelectedPlaylist = playlist;
		foreach(var child in SongList.GetChildren())
			child.QueueFree();
		if(playlist == null)
		{
			ArtistName.UpdateText("");
			AlbumName.UpdateText("");
			return;
		}
		for(int i = 0; i < playlist.songs.Count; ++i)
		{
			AddSongButton(playlist, i);
		}
		AlbumName.UpdateText(playlist.playlistName);
		switch(playlist.GetPlaylistType())
		{
			case Playlist.Type.PLAYLIST:
				ArtistName.UpdateText("");
				ShowAlbumMode();
				break;
			case Playlist.Type.ALBUM:
				ArtistName.UpdateText(((Album)playlist).albumArtist);
				ShowAlbumMode();
				break;
			case Playlist.Type.AUDIOBOOK:
				ArtistName.UpdateText(((Album)playlist).albumArtist);
				ShowAudiobookMode();
				UpdateBookmarkVisual();
				break;
		}
	}

	public void SetSpeedPlayback(int index)
	{
		if(SpeedControl != null)
			SpeedControl.SetSpeed(SpeedOptions[index]);
	}

	public void ShowAudiobookMode()
	{
		PlaybackType.Visible = false;
		PlaybackSpeed.Visible = true;
		SetBookmark.Visible = true;
		BookmarkLabel.Visible = true;
		ArtistName.Visible = false;
		if(SelectedPlaylist == PlayingPlaylist && ActiveBookmark != null)
			VisualBookmark = ActiveBookmark;
		else
			VisualBookmark = ReadBookmark(((Audiobook)SelectedPlaylist).bookmarkPath);
	}

	public void ShowAlbumMode()
	{
		PlaybackType.Visible = true;
		PlaybackSpeed.Visible = false;
		SetBookmark.Visible = false;
		BookmarkLabel.Visible = false;
		ArtistName.Visible = true;
		VisualBookmark = null;
	}

	public void AddSongButton(Playlist playlist, int index)
	{
		var button = SongButtonScene.Instantiate<SongButton>();
		SongList.AddChild(button);
		button.Initialize(playlist, index);
	}

	private struct AddFolderArguments
	{
		public string folder;
		public bool isRecursive;
		public bool isAudiobook;
	}

	public void AddFolder(string folder, bool isRecursive = false, bool isAudiobook = false)
	{
		var args = new AddFolderArguments();
		args.folder = folder;
		args.isRecursive = isRecursive;
		args.isAudiobook = isAudiobook;
		AddFolderQueue.Enqueue(args);
	}

	public void NewPlaylist(string name)
	{
		Playlist playlist = new Playlist();
		playlist.playlistName = name;
		playlist.songs = new List<Song>();
		LibraryObject libO = new()
		{
			Playlist = playlist,
			Path = $"~{Library.Count:0000}0{playlist.GetCleanName()}.json"
		};
		Library.Add(name, libO);
		var button = AddPlaylistButton(playlist);
		libO.Button = button;
		SavePlaylist(libO);
	}

	private void AddFolder(AddFolderArguments args)
	{
		List<string> extensionList = new List<string>() {".mp3",".wma",".flac",".wav"};
		string[] fileNames = Directory.GetFiles(args.folder);
		SortedDictionary<TagLib.File, string> files = new SortedDictionary<TagLib.File, string>(
			Comparer<TagLib.File>.Create(
				(TagLib.File a, TagLib.File b) =>{
					int ret = a.Tag.Track.CompareTo(b.Tag.Track);
					if(ret != 0)
						return a.Tag.Track.CompareTo(b.Tag.Track);
					return a.Name.CompareTo(b.Name);
				}
			)
		);

		foreach(string fileName in fileNames)
		{
			string modedFileName = fileName.Replace("\\","/");
			string extention = Path.GetExtension(modedFileName);

			if(extensionList.Contains(extention))
			{
				files.Add(TagLib.File.Create(modedFileName), modedFileName);
			}
		}

		Dictionary<string, Dictionary<int, int>> albumTrackIndex = new Dictionary<string, Dictionary<int, int>>();

		foreach(var file in files)
		{
			string album = file.Key.Tag.Album;
			album ??= "";
			if(Library.TryGetValue(album, out LibraryObject libO) == false)
			{
				libO = new();
				if(args.isAudiobook)
					libO.Playlist = MakeAudiobookWithBookmark(file.Key.Tag, args.folder);
				else
					libO.Playlist = new Album(file.Key.Tag);
				libO.Path = $"{Library.Count:0000}0{libO.Playlist.GetCleanName()}.json";
				Library.Add(album, libO);
				libO.Button = AddAlbumButton((Album)libO.Playlist, args.isAudiobook);
			}
			if(albumTrackIndex.TryGetValue(album, out Dictionary<int, int> trackIndex) == false)
			{
				trackIndex = new Dictionary<int, int>();
				albumTrackIndex.Add(album, trackIndex);
				for(int i = 0; i < Library[album].Playlist.songs.Count; ++i)
				{
					trackIndex.Add(Library[album].Playlist.songs[i].trackNumber, i);
				}
			}
			if(trackIndex.TryGetValue((int)file.Key.Tag.Track, out int index))
				libO.Playlist.songs[index] = MakeSong(file.Key, file.Value);
			else
				libO.Playlist.songs.Add(MakeSong(file.Key, file.Value));
		}

		if(args.isRecursive)
		{
			string[] directories = Directory.GetDirectories(args.folder);
			foreach(string directory in directories)
				AddFolder(directory, args.isRecursive, args.isAudiobook);
		}
	}

	public bool PreloadLibrary()
	{
		DirAccess.MakeDirAbsolute(LibraryPath);
		string[] files = DirAccess.GetFilesAt(LibraryPath);
		foreach(string fileName in files)
			LoadAlbumQueue.Add(fileName);
		return true;
	}
	
	public Playlist LoadPlaylist(string path)
	{
		var file = Godot.FileAccess.GetFileAsString(LibraryPath + path);
		var playlist = JsonConvert.DeserializeObject<Playlist>(file, SerializerSettings);
		LibraryObject libO = new()
		{
			Playlist = playlist,
			Path = path
		};
		Library.Add(playlist.playlistName, libO);
		return playlist;
	}

	public void SavePlaylist(LibraryObject libO)
	{
		var file = Godot.FileAccess.Open(LibraryPath + libO.Path, Godot.FileAccess.ModeFlags.Write);
		file.StoreString(JsonConvert.SerializeObject(libO.Playlist, Formatting.Indented, SerializerSettings));
		file.Close();
	}

	public void SaveSettings()
	{
		var file = Godot.FileAccess.Open(SettingsPath, Godot.FileAccess.ModeFlags.Write);
		file.StoreString(JsonConvert.SerializeObject(ProgramSettings, Formatting.Indented, SerializerSettings));
		file.Close();
	}

	public bool LoadSettings()
	{
		var file = Godot.FileAccess.GetFileAsString(SettingsPath);
		if(file == "")
			return false;
		var settings = JsonConvert.DeserializeObject<Settings>(file, SerializerSettings);
		if(settings == null)
			return false;
		ProgramSettings = settings;
		return true;
	}

	public void ReflectSettings()
	{
		switch(ProgramSettings.backgroundStyle)
		{
			case Settings.BackgroundStyle.RAINBOW:
				RainbowBackground.Visible = true;
				FlatBackground.Visible = false;
				break;
			case Settings.BackgroundStyle.FLAT:
				RainbowBackground.Visible = false;
				FlatBackground.Visible = true;
				break;
			case Settings.BackgroundStyle.CLEAR:
				RainbowBackground.Visible = false;
				FlatBackground.Visible = false;
				break;
		}
		AwakeControl.AlwaysAwake = !ProgramSettings.hideUI;
		FlatBackground.Color = ProgramSettings.flatColor;
		SetPlaybackMode((int)ProgramSettings.playbackMode);
	}

	public void PickDefaultPlaylist()
	{
		if(SelectedPlaylist != null)
			return;
		if(ProgramSettings.playlist != "" && ProgramSettings.song != -1 && Library.TryGetValue(ProgramSettings.playlist, out LibraryObject defaultLibO) && defaultLibO.Playlist.songs.Count > 0)
		{
			SetPlaylist(defaultLibO.Playlist);
			PlaySong(defaultLibO.Playlist, ProgramSettings.song);
			TogglePlay();
			return;
		}
		if(Library.Count > 0)
		{
			LibraryObject libO = Library.Values.First();
			SetPlaylist(libO.Playlist);
			PlaySong(libO.Playlist, 0);
			TogglePlay();
		}
		SetPlaylist(null);
	}

	public void RemoveAlbum()
	{
		Library[SelectedPlaylist.playlistName].Button.QueueFree();
		DirAccess.RemoveAbsolute(LibraryPath + Library[SelectedPlaylist.playlistName].Path);
		Library.Remove(SelectedPlaylist.playlistName);
		PickDefaultPlaylist();
	}

	public Control AddPlaylistButton(Playlist playlist)
	{
		var button = PlaylistButtonScene.Instantiate<PlaylistButton>();
		PlaylistContainer.AddChild(button);
		button.Initialize(playlist);
		return button;
	}

	public Control AddAlbumButton(Album album, bool isAudiobook)
	{
		var button = AlbumButtonScene.Instantiate<AlbumButton>();
		if(isAudiobook)
			AudiobookGrid.AddChild(button);
		else
			AlbumGrid.AddChild(button);
		button.Initialize(album);
		return button;
	}

	public override void _Process(double delta)
	{
		if(scheduleTogglePlay || Input.IsActionJustPressed("PLAY"))
		{
			scheduleTogglePlay = false;
			TogglePlay();
		}
		if(scheduleNext || Input.IsActionJustPressed("NEXT"))
		{
			scheduleNext = false;
			NextSong(true);
		}
		if(Playing)
		{
			UpdateTimeTracker();
			if(ActiveBookmark != null)
			{
				Bookmark current = new()
				{
					chapterIndex = PlayingIndex,
					chapterPositionRatio = GetCurrentSongTimeRatio(),
					chapterTime = ActiveBookmark.chapterPositionRatio * PlayingSong.songLength
				};

				if (current.LaterThan(ActiveBookmark))
				{
					ActiveBookmark.chapterIndex = current.chapterIndex;
					ActiveBookmark.chapterPositionRatio = current.chapterPositionRatio;
					ActiveBookmark.chapterTime = current.chapterTime;
					if(ActiveBookmark == VisualBookmark)
					{
						UpdateBookmarkVisual();
					}
				}
				
			}
			long frame = SampleQueue.length - SampleQueue.position;
			if(DefferedSongUpdate != null)
			{
				if(DefferedSongUpdate.Value.Item3)
					PlaySong(DefferedSongUpdate.Value.Item1, DefferedSongUpdate.Value.Item2, 0, false);
				else
					SelectSong(DefferedSongUpdate.Value.Item1, DefferedSongUpdate.Value.Item2);
				DefferedSongUpdate = null;
			}
		}
		if(AddFolderQueue.Count > 0)
		{
			var args = AddFolderQueue.Dequeue();
			AddFolder(args);
			if(AddFolderQueue.Count == 0)
			{
				foreach(LibraryObject libO in Library.Values)
					SavePlaylist(libO);
			}
		}
		int count = 0;
		while(LoadAlbumQueue.Count > 0)
		{
			count += 1;
			string path = LoadAlbumQueue.First();
			LoadAlbumQueue.Remove(path);
			var playlist = LoadPlaylist(path);
			if(playlist is Album album)
				Library[playlist.playlistName].Button = AddAlbumButton(album, album is Audiobook);
			else
				Library[playlist.playlistName].Button = AddPlaylistButton(playlist);
			if(playlist.playlistName == ProgramSettings.playlist)
				PickDefaultPlaylist();
			if(count >= 5)
				break;
		}
		while(BufferSavePlaylist.Count != 0)
		{
			var playlist = BufferSavePlaylist[0];
			BufferSavePlaylist.RemoveAt(0);
			SavePlaylist(Library[playlist]);
		}
	}

	public void ResetBookmark()
	{
		ActiveBookmark.chapterIndex = PlayingIndex;
		ActiveBookmark.chapterPositionRatio = GetCurrentSongTimeRatio();
		ActiveBookmark.chapterTime = ActiveBookmark.chapterPositionRatio * PlayingSong.songLength;
		if(PlayingPlaylist.GetPlaylistType() == Playlist.Type.AUDIOBOOK && ActiveBookmark != null)
			WriteBookmark(((Audiobook)PlayingPlaylist).bookmarkPath, ActiveBookmark);
	}

	public Audiobook MakeAudiobookWithBookmark(TagLib.Tag tag, string folder)
	{
		Audiobook audiobook = new Audiobook(tag, folder);

		var bookmark = new Bookmark();
		WriteBookmark(audiobook.bookmarkPath, bookmark);

		return audiobook;
	}

	public Song MakeSong(TagLib.File file, string filePath)
	{
		var song = new Song();
		song.filePath = filePath;
		song.artistName = file.Tag.JoinedPerformers;
		song.songName = file.Tag.Title;
		song.songLength = file.Properties.Duration;
		song.trackNumber = (int)file.Tag.Track;

		return song;
	}

	public void WriteBookmark(string filePath, Bookmark bookmark)
	{
		File.WriteAllText(filePath, JsonConvert.SerializeObject(bookmark, Formatting.Indented));
	}

	public Bookmark ReadBookmark(string filePath)
	{
		return JsonConvert.DeserializeObject<Bookmark>(File.ReadAllText(filePath));
	}
}

public class Settings
{
	public enum BackgroundStyle {RAINBOW, FLAT, CLEAR}
	public BackgroundStyle backgroundStyle = BackgroundStyle.RAINBOW;
	public Color flatColor = Colors.DarkSlateGray;
	public bool hideUI = true;
	public string playlist = "";
	public int song = -1;
	public Main.PlaybackMode playbackMode = Main.PlaybackMode.STOP;
}

public class LibraryObject
{
	public Playlist Playlist = null;
	public Control Button = null;
	public string Path = null;
}

public class Playlist
{
	public string playlistName;
	public List<Song> songs;

    public virtual Type GetPlaylistType() {return Type.PLAYLIST;}
	public enum Type
	{
		PLAYLIST,
		ALBUM,
		AUDIOBOOK
	}

	public string GetCleanName()
	{
		return playlistName.Replace(':','_').Replace('/','_');
	}
}

public class Album : Playlist
{
	public string albumArtist;
	public byte[] albumCover;
	public string albumCoverType;
	public override Type GetPlaylistType() {return Type.ALBUM;}
	public Album(TagLib.Tag tag = null)
	{
		songs = new List<Song>();
		if(tag != null)
		{
			playlistName = tag.Album;
			albumArtist = tag.JoinedAlbumArtists;
			if(tag.Pictures.Length == 0)
			{
				albumCover = null;
				albumCoverType = null;
			}
			else
			{
				albumCover = tag.Pictures[0].Data.Data;
				albumCoverType = tag.Pictures[0].MimeType;
			}
		}
	}
}

public class Audiobook : Album
{
	public string bookmarkPath;
	public override Type GetPlaylistType() {return Type.AUDIOBOOK;}
	public Audiobook(TagLib.Tag tag, string folder) : base(tag)
	{
		bookmarkPath = folder + "\\bookmark.json";
	}
}

public class Bookmark
{
	public TimeSpan chapterTime = TimeSpan.Zero;
	public double chapterPositionRatio = 0;
	public int chapterIndex = 0;

	public override string ToString()
	{
		return $" File #{chapterIndex+1} {Song.GetLengthMinutesSeconds(chapterTime)}";
	}

	public bool LaterThan(Bookmark other)
	{
		if(chapterIndex == other.chapterIndex)
			return chapterPositionRatio > other.chapterPositionRatio;
		return chapterIndex > other.chapterIndex;
		
	}
}

public class Song
{
	public string filePath;
	public string songName;
	public string artistName;
	public int trackNumber;
	public TimeSpan songLength;

	public static string GetLengthMinutesSeconds(TimeSpan time)
	{
		int minutes = (int)time.TotalMinutes;
		int seconds = time.Seconds;
		return $"{minutes}:{seconds:00}";
	}

	public string GetLengthMinutesSeconds()
	{
		return GetLengthMinutesSeconds(songLength);
	}

	public override bool Equals(object obj)
	{
		return obj.GetHashCode() == GetHashCode();
	}

	public override int GetHashCode()
	{
		return filePath.GetHashCode();
	}
}
