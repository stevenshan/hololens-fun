using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Xml.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

public class Translator : MonoBehaviour
{
    public static Translator instance;

    // Public fields accessible in the Unity Editor
    public string apiKey = "955f6747399a477e8f25c0fc52a2a1d6";

    private ClientWebSocket client;
    private bool ready = false;
    private bool reading = false;

    async void Start()
    {
        instance = this;
        string from = "en-US";
        string to = "it-IT";
        string features = "partial";
        string voice = "it-IT-Elsa";
        string api = "1.0";
        string host = "wss://dev.microsofttranslator.com";
        string path = "/speech/translate";
        string uri = host + path +
            "?from=" + from +
            "&to=" + to +
            "&api-version=" + api +
            "&features=" + features +
            "&voice=" + voice;

        Debug.Log("starting web socket");

        client = new ClientWebSocket();
        client.Options.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

        await client.ConnectAsync(new Uri(uri), CancellationToken.None);
        Debug.Log("connected to socket");

        var header = WavFile.GetWaveHeader(0);
        var wavHeaderBuffer = new ArraySegment<byte>(header);
        send(wavHeaderBuffer);
        ready = true;
    }

    private async void send(ArraySegment<byte> buff)
    {
        await client.SendAsync(buff, WebSocketMessageType.Binary, false, CancellationToken.None);
    }

    async void Update()
    {
        var inbuf = new byte[102400];
        var segment = new ArraySegment<byte>(inbuf);

        if (!reading && client.State == WebSocketState.Open && ready)
        {
            reading = true;
            var result = await client.ReceiveAsync(segment, CancellationToken.None);
            switch (result.MessageType)
            {
                case WebSocketMessageType.Close:
                    Debug.Log("Received close message. Status: " + result.CloseStatus + ". Description: " + result.CloseStatusDescription);
                    ready = false; 
                    await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                case WebSocketMessageType.Text:
                    Debug.Log("Received text.");
                    Debug.Log(Encoding.UTF8.GetString(inbuf).TrimEnd('\0'));
                    break;
                case WebSocketMessageType.Binary:
                    Debug.Log("Received binary data: " + result.Count + " bytes.");
                    break;
            }
            reading = false;
        }
    }

    public async Task WriteData(MemoryStream stream, int count)
    {
        if (!ready)
        {
            return;
        }
        ArraySegment<byte> buffer;
        if (stream.TryGetBuffer(out buffer))
        {
            send(buffer);
        }
    }

}
