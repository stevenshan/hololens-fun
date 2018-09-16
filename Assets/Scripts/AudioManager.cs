#define DONT_DOWNSAMPLE

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    AudioClip _clip;
    string _mic;

    public int SampleRate = 16000;

    float[] audioData;
    int _totalRead = 0;

    int _lastRead = 0;

    // 100ms of data
    public int ChunkSize = 1600;

    MemoryStream dataStream = new MemoryStream();

    ArraySegment<byte> _buffer = new ArraySegment<byte>();

    /// <summary>
    /// Convert floating point audio data in the range -1.0 to 1.0 to signed 16-bit audio data
    /// </summary>
    /// <param name="audioData"></param>
    /// <param name="stream"></param>
    /// <returns></returns>
    int BufferConvertedData(float[] audioData, Stream stream)
    {
#if DONT_DOWNSAMPLE
        // Can't just do a block copy here as we need to convert from float[-1.0f, 1.0f] to 16bit PCM
        int i = 0;
        while (i < audioData.Length)
        {
            stream.Write(BitConverter.GetBytes(Convert.ToInt16(audioData[i] * Int16.MaxValue)), 0, sizeof(Int16));
            ++i;
        }
#else
        // Downsample from 48Khz tp 16kHz
        for (int i=0;i<audioData.Length/3;i+=3)
        {
            float avg = (audioData[i] + audioData[i + 1] + audioData[i + 2]) / 3.0f;
            stream.Write(BitConverter.GetBytes(Convert.ToInt16(avg * Int16.MaxValue)), 0, sizeof(Int16));
        }
#endif

        return audioData.Length;
    }

    public bool IsRecording()
    {
        return _clip != null;
    }

    public void StartRecording()
    {
        // start streaming from the microphone
        if (Microphone.devices.Length > 0)
        {
            _mic = Microphone.devices[0];

            foreach (var mic in Microphone.devices)
            {
                Debug.Log(mic);
            }

            int minFreq = 0;
            int maxFreq = 0;

            Microphone.GetDeviceCaps(_mic, out minFreq, out maxFreq);

            //AudioSettings.outputSampleRate = 16000;

            // if we have a mic start capturing and streaming audio...

            _clip = Microphone.Start(_mic, true, 1, SampleRate);
            int numChannels = _clip.channels;
            while (Microphone.GetPosition(_mic) < 0) { } // HACK from Riro
            Debug.Log("Recording started...");
        }
        else
        {
            Debug.Log("No Microphone detected");
        }
    }

    public void StopRecording()
    {
        _clip = null;
    }

    // initialize 
    void Start()
    {
        StartRecording();
    }

    // called every frame
    void FixedUpdate()
    {
        if (_clip == null)
            return;

        int currentPos = Microphone.GetPosition(_mic);

        if (currentPos < _lastRead)
        {
            _lastRead = 0;
        }
        if ((currentPos - _lastRead) == 0)
            return;

        audioData = new float[currentPos - _lastRead];

        if (_clip.GetData(audioData, _lastRead))
        {
            _totalRead += BufferConvertedData(audioData, dataStream);
            _lastRead = currentPos;
        }

        // once chunk is full, send it
        if (dataStream.Position >= ChunkSize)
        {
            dataStream.Flush();
            if (!dataStream.TryGetBuffer(out _buffer))
            {
                Debug.Log("Couldn't read buffer");
                return;
            }

            // write data to socket
            var consumer = Translator.instance;
            consumer.WriteData(dataStream, (int)dataStream.Position);

            dataStream.Seek(0, SeekOrigin.Begin);
            dataStream.SetLength(0);
        }
    }
}
