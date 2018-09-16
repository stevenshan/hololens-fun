using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class WavFile : MonoBehaviour
{
    public static byte[] GetWaveHeader(uint dataSize = 0, uint totalFileSize = 0, int sr = 16000)
    {
        var channels = (short)1;
        var sampleRate = sr;
        var bitsPerSample = (short)16;
        var extraSize = 0;
        var blockAlign = (short)(channels * (bitsPerSample / 8));
        var averageBytesPerSecond = sampleRate * blockAlign;

        using (MemoryStream stream = new MemoryStream())
        {
            BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8);
            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            // Total file size - zero is fine for streaming format..
            writer.Write(dataSize);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write((int)(18 + extraSize)); // wave format length 
            writer.Write((short)1);// PCM
            writer.Write((short)channels);
            writer.Write((int)sampleRate);
            writer.Write((int)averageBytesPerSecond);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);
            writer.Write((short)extraSize);

            writer.Write(Encoding.UTF8.GetBytes("data"));
            // dataSize - zero is fine for streaming format..
            writer.Write(dataSize);

            stream.Position = 0;
            byte[] buffer = new byte[stream.Length];
            stream.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}

