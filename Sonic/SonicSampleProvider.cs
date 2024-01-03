using System.Collections.Generic;
using Godot;
using NAudio.Wave;

public class SonicSampleProvider : ISampleProvider
{
	public WaveFormat WaveFormat { get; private set;}
    private AudioFileReader reader;
	public delegate void OnStoppedHandler();
	public event OnStoppedHandler OnStopped;
    private Sonic sonic;
    private long length;
    private long position;
    private float speed;
    private bool stopped;

    public SonicSampleProvider()
	{
	}

    public void SetSpeed(float value)
    {
        speed = value;
        if(sonic != null)
            sonic.setSpeed(value);
    }

	public void Init(AudioFileReader _reader, long seek)
	{
        if(sonic != null)
            sonic.flushStream();
        reader = _reader;
		WaveFormat = reader.WaveFormat;
        length = reader.Length;
        stopped = false;
        sonic = new Sonic(WaveFormat.SampleRate, WaveFormat.Channels);
		position = seek;
	}

    public int Read(float[] buffer, int offset, int count)
    {
        if(stopped)
            return 0;
        int samples = (int)(count*speed);
        float[] inBuffer = new float[samples];
        int readThisTime = reader.Read(inBuffer, (int)(offset*speed), samples);
        position = reader.Position;
        sonic.writeFloatToStream(inBuffer, samples/WaveFormat.Channels);
        int writeCount = sonic.readFloatFromStream(buffer, count/WaveFormat.Channels);
        if(readThisTime == 0)
        {
            stopped = true;
            OnStopped();
        }
        return writeCount*WaveFormat.Channels;
    }
}