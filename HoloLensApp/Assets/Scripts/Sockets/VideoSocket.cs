using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
#if !UNITY_EDITOR
using System;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Capture.Frames;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
#endif

public class VideoSocket : MonoBehaviour
{
    private bool collectAndSendData = true;
#if !UNITY_EDITOR
    private readonly string HOLOLENS_CAMERA_NAME = "QC Back Camera";
    private MediaCapture mediaCapture;
    private MediaFrameReader mediaFrameReader;
    private readonly int headerSize;
    private string timestampobject;
    private ConcurrentQueue<MediaFrameReference> videoFrameQueue;
    private ClientSocket socket;
    
    async void Start()
    {
        videoFrameQueue = new ConcurrentQueue<MediaFrameReference>();
        System.Diagnostics.Debug.WriteLine(SocketData.Address);
        socket = new ClientSocket(SocketData.Address, SocketData.VideoPort, SocketData.BufferSize);
        await socket.ConnectToServer();
        await initializeRecording();
        await startRecording();
    }

    public async Task startRecording()
    {
        if (mediaFrameReader != null)
        {
            await mediaFrameReader.StartAsync();
        }
    }

    public async Task initializeRecording()
    {
        mediaCapture = new MediaCapture();
        var frameSourceGroups = await MediaFrameSourceGroup.FindAllAsync();

        MediaFrameSourceGroup selectedGroup = null;
        MediaFrameSourceInfo colorSourceInfo = null;

        foreach (var sourceGroup in frameSourceGroups)
        {
            foreach (var sourceInfo in sourceGroup.SourceInfos)
            {
                if (sourceGroup.DisplayName.Equals(HOLOLENS_CAMERA_NAME)
                    && sourceInfo.MediaStreamType == MediaStreamType.VideoRecord)
                {
                    colorSourceInfo = sourceInfo;
                    selectedGroup = sourceGroup;
                }
            }
        }

        var settings = new MediaCaptureInitializationSettings()
        {
            SourceGroup = selectedGroup,
            SharingMode = MediaCaptureSharingMode.ExclusiveControl,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
            StreamingCaptureMode = StreamingCaptureMode.Video
        };

        await mediaCapture.InitializeAsync(settings);

        var colorFrameSource = mediaCapture.FrameSources[colorSourceInfo.Id];
        var preferredFormat = colorFrameSource.SupportedFormats.Where(format =>
        {
            return format.VideoFormat.Width >= 1000 && format.Subtype.Equals("NV12");

        }).FirstOrDefault();

        await colorFrameSource.SetFormatAsync(preferredFormat);

        mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Nv12);
        mediaFrameReader.FrameArrived += ColorFrameReader_FrameArrived;
    }

    private void ColorFrameReader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
    {
        if (collectAndSendData)
        {
            timestampobject = DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm:ss.fff");
            byte[] frameTimeStamp = Encoding.UTF8.GetBytes(timestampobject.PadLeft(25));
            var mediaFrameReference = sender.TryAcquireLatestFrame();

            if (mediaFrameReference != null)
            {
                videoFrameQueue.Enqueue(mediaFrameReference);
            }
        
            var videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            var softwarebitmap = videoMediaFrame?.SoftwareBitmap;
            SoftwareBitmap bitmapFrame = null;

            if (softwarebitmap != null)
            {
                if (softwarebitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8 ||
            softwarebitmap.BitmapAlphaMode != BitmapAlphaMode.Premultiplied)
                {
                    bitmapFrame = SoftwareBitmap.Convert(softwarebitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }
            }

            if (bitmapFrame != null) 
            {
                byte[] encodedVideoFrame = EncodedBytes(bitmapFrame, BitmapEncoder.JpegEncoderId).Result;
                socket.SendData(frameTimeStamp);
                SendVideoData(encodedVideoFrame);
            }
        }
     }

     private async Task<byte[]> EncodedBytes(SoftwareBitmap soft, Guid encoderId)
     {
        
        byte[] array = null;
        using (var ms = new InMemoryRandomAccessStream())
        {
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(encoderId, ms);
            encoder.SetSoftwareBitmap(soft);
            try
            {
                await encoder.FlushAsync();
            }
            catch (Exception) { return new byte[0]; }

            array = new byte[ms.Size];
            await ms.ReadAsync(array.AsBuffer(), (uint)ms.Size, InputStreamOptions.None);
        }

        return array;
     }

     private void SendVideoData(byte[] encodedFrameArray)
     {
        List<byte> encodedFrame = new List<byte>(encodedFrameArray);
        string dataLength = encodedFrame.Count.ToString().PadLeft(SocketData.HeaderSize);
        byte[] dataLengthBytes = Encoding.UTF8.GetBytes(dataLength);
        socket.SendData(dataLengthBytes);

        int begin = 0;
        int messageLength = encodedFrame.Count;

        while (begin <= messageLength)
        {
            if (messageLength - begin < SocketData.BufferSize)
            {
                var packet = encodedFrame.GetRange(begin, messageLength - begin).ToArray();
                socket.SendData(packet);
            }
            else
            {
                var packet = encodedFrame.GetRange(begin, SocketData.BufferSize).ToArray();   
                socket.SendData(packet);
            }
            begin += SocketData.BufferSize;
        }
     }

     private async Task SendVideoDataAsync(byte[] encodedFrameArray)
     {
        List<byte> encodedFrame = new List<byte>(encodedFrameArray);
        string dataLength = encodedFrame.Count.ToString().PadLeft(SocketData.HeaderSize);
        byte[] dataLengthBytes = Encoding.UTF8.GetBytes(dataLength);
        await socket.SendDataAsync(dataLengthBytes);

        int begin = 0;
        int messageLength = encodedFrame.Count;
        var packetsToSend = new List<byte[]>();

        while (begin <= messageLength)
        {
            if (messageLength - begin < SocketData.BufferSize)
            {
                var packet = encodedFrame.GetRange(begin, messageLength - begin).ToArray();
                //socket.SendData(packet);
                packetsToSend.Add(packet);
            }
            else
            {
                var packet = encodedFrame.GetRange(begin, SocketData.BufferSize).ToArray();   
                //socket.SendData(packet);
                packetsToSend.Add(packet);
            }
            begin += SocketData.BufferSize;
        }
        var pendingTasks = new System.Threading.Tasks.Task[packetsToSend.Count];
        for (int index = 0; index < packetsToSend.Count; ++index)
        {
            // track all pending writes as tasks, but don't wait on one before beginning the next.
            pendingTasks[index] = socket.Socket.OutputStream.WriteAsync(packetsToSend[index].AsBuffer()).AsTask();
            // Don't modify any buffer's contents until the pending writes are complete.
        }
        await socket.Socket.OutputStream.FlushAsync();
     }

     public ConcurrentQueue<MediaFrameReference> getQueue()
     {
         return videoFrameQueue;
     }
#endif
    public void StopSendingData()
    {
        System.Diagnostics.Debug.WriteLine("DATA SENDING PAUSED");
        collectAndSendData = false;
    }

    public void ResumeSendingData()
    {
        collectAndSendData = true;
    }

    public bool IsSendingData()
    {
        return collectAndSendData;
    }

    public void CloseConnection()
    {
#if !UNITY_EDITOR
        mediaFrameReader.StopAsync().GetAwaiter().GetResult();
        System.Diagnostics.Debug.WriteLine("CLOSED RECORDING");
        mediaFrameReader.FrameArrived -= ColorFrameReader_FrameArrived;
        mediaCapture.Dispose();
        mediaCapture = null;
        socket.CloseConnection();
#endif
    }
}
