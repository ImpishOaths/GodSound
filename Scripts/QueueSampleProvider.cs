using System.Collections.Generic;
using NAudio.Wave;

public class QueueSampleProvider : ISampleProvider
{
	private Queue<AudioFileReader> readers;
	public delegate void OnEmptyHandler();
	public event OnEmptyHandler OnEmpty;
	public long length = 1;
	public long position = 0;


	public QueueSampleProvider()
	{
		readers = new Queue<AudioFileReader>();
	}

	public void Enqueue(AudioFileReader sample)
	{
		readers.Enqueue(sample);
		if(readers.TryPeek(out AudioFileReader reader))
			length = reader.Length;
	}

	public void Init(WaveFormat waveFormat, long seek)
	{
		readers = new Queue<AudioFileReader>();
		WaveFormat = waveFormat;
		position = seek;
	}

	public WaveFormat WaveFormat { get; private set;}

	public int Read(float[] buffer, int offset, int count)
	{
		var hasRead = 0;
		while (hasRead < count && readers.Count > 0)
		{
			var needed = count - hasRead;
			var readThisTime = 0;
			if(readers.TryPeek(out AudioFileReader reader))
			{
				readThisTime = reader.Read(buffer, hasRead, needed);
				if(position < length)
					position = reader.Position;
				if (readThisTime == 0)
				{
					readers.Dequeue();
					OnEmpty();
					if(readers.TryPeek(out reader))
					{
						readThisTime = reader.Read(buffer, hasRead, needed);
						position = 0;
					}
				}
			}
			
			hasRead += readThisTime;
		}
		return hasRead;
	}
}
