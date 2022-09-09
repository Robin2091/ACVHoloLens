using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using Microsoft.MixedReality.Toolkit.Input;

public class AudioSocket : MonoBehaviour
{
    private bool collectAndSendData = true;
    private ClientSocket socket;
#if !UNITY_EDITOR
    private AudioClip audioClip;
    private readonly int clipLength = 20;
    private int audioFrequency;
    private int lastSample;
    
    // Start is called before the first frame update
    async void Start()
    {
        socket = new ClientSocket(SocketData.Address, SocketData.AudioPort, SocketData.BufferSize);
        await socket.ConnectToServer();
        System.Diagnostics.Debug.WriteLine("IN START");
        lastSample = 0;
        audioFrequency = 44100;
        startRecording();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        sendAudioData();
    }

    public void startRecording()
    {
        audioClip = Microphone.Start(null, true, clipLength, audioFrequency);
        string startRecordingTimeStamp = DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss.fff");
        byte[] frameTimeStamp = Encoding.UTF8.GetBytes(startRecordingTimeStamp.PadLeft(SocketData.TimeStampLength));
        socket.SendData(Encoding.UTF8.GetBytes(SocketData.TimeStampLength.ToString()));
        socket.SendData(frameTimeStamp);
    }

    public void sendAudioData()
    {
        if (collectAndSendData)
        {
            if (audioClip != null)
            {
                int pos = Microphone.GetPosition(null);
                int diff = pos - lastSample;

                if (diff > 0)
                {
                    float[] samples = new float[diff * audioClip.channels];
                    audioClip.GetData(samples, lastSample);
                    byte[] audioData = new byte[samples.Length * sizeof(float)];
                    Buffer.BlockCopy(samples, 0, audioData, 0, audioData.Length);
                    string dataLength = audioData.Length.ToString().PadLeft(SocketData.HeaderSize);
                    byte[] dataLengthBytes = Encoding.UTF8.GetBytes(dataLength);
                    System.Diagnostics.Debug.WriteLine(dataLength);
                    socket.SendData(audioData);
                }
                lastSample = pos;
            }
        }
    }
#endif
    public void StopSendingData()
    {
        collectAndSendData = false;
        Microphone.End(null);
#if !UNITY_EDITOR
        string pauseMessage = "00000";
        string dataLength = pauseMessage.Length.ToString().PadLeft(SocketData.HeaderSize);
        byte[] dataLengthBytes = Encoding.UTF8.GetBytes(dataLength);
        byte[] pauseMessageBytes = Encoding.UTF8.GetBytes(pauseMessage);
        System.Diagnostics.Debug.WriteLine("AUDIO DATA PAUSED");
        socket.SendData(dataLengthBytes);
        socket.SendData(pauseMessageBytes);
#endif 
    }

    public void ResumeSendingData()
    {
        collectAndSendData = true;
#if !UNITY_EDITOR
        startRecording();
#endif
    }

    public bool IsSendingData()
    {
        return collectAndSendData;
    }

    public void CloseConnection()
    {
#if !UNITY_EDITOR
        Microphone.End(null);
        socket.CloseConnection();
#endif
    }
}
