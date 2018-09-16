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
using System.Net.Http;

public class Translated
{
    public string type { get; set; }
    public string recognition { get; set; }
    public string translation { get; set; }
}

public class Translator : MonoBehaviour
{
    public static Translator instance;

    // Public fields accessible in the Unity Editor
    private string apiKey = "955f6747399a477e8f25c0fc52a2a1d6";
    private string hostServer = "http://requestbin.stevenshan.com/1kty6ju1";

    private ClientWebSocket client;
    private bool ready = false;
    private bool reading = false;

    async void Start()
    {
        instance = this;
        string from = "en-US";
        string to = "it-IT";
        string voice = "it-IT-Elsa";
        string api = "1.0";
        string host = "wss://dev.microsofttranslator.com";
        string path = "/speech/translate";
        string uri = host + path +
            "?from=" + from +
            "&to=" + to +
            "&api-version=" + api +
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
                    Translated translated = JsonConvert.DeserializeObject<Translated>(Encoding.UTF8.GetString(inbuf));
                    if (translated.type == "final")
                    {
                        StartCoroutine("coordinate", translated.recognition);
                    }
                    break;
                case WebSocketMessageType.Binary:
                    Debug.Log("Received binary data: " + result.Count + " bytes.");
                    break;
            }
            reading = false;
        }
    }

    IEnumerator coordinate(string sentence)
    {
        WWWForm form = new WWWForm();
        form.AddField("text", sentence);
        using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(hostServer, form))
        {
            yield return unityWebRequest.SendWebRequest();

            long responseCode = unityWebRequest.responseCode;

            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                Debug.Log("HTTP request error.");
            }
            else
            {
                string response = unityWebRequest.downloadHandler.text;
                Debug.Log("Got response from coordination server: " + response);
            }
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
