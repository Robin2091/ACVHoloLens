using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_EDITOR
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Windows.Perception;
using Windows.Perception.Spatial;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.UI.Input.Spatial;
using Windows.Perception.People;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.OpenXR;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class GazeSocket : MonoBehaviour
{
    [SerializeField]
    private VideoSocket videoSender;

    public float gazeVectorAngleAdjustment;
    private bool collectAndSendData = true;
    public delegate void GazeDataStopped();
    public static event GazeDataStopped OnGazeDataStopped;
    private bool sendToQueue = false;
    public bool SendToQueue { get => sendToQueue; set => sendToQueue = value; }

#if !UNITY_EDITOR
    private ClientSocket socket;
    private DateTime lastEyeGazeTimeStamp = DateTime.MinValue;
    private MediaFrameReference currentFrame;
    private MediaFrameReference prevFrame = null;
    private bool firstFrame = true;
    private ConcurrentQueue<GazeData> gazeDataQueue;
    SpatialCoordinateSystem worldCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity) as SpatialCoordinateSystem;

    // Start is called before the first frame update
    async void Start()
    {
       gazeVectorAngleAdjustment = 2.35f;
       videoSender = GameObject.Find("VideoSender").GetComponent<VideoSocket>();
       socket = new ClientSocket(SocketData.Address, SocketData.GazePort, SocketData.BufferSize);
       gazeDataQueue = new ConcurrentQueue<GazeData>();
       await socket.ConnectToServer();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (collectAndSendData)
        {
            bool success = videoSender.getQueue().TryDequeue(out currentFrame);
            if (!firstFrame && !success)
            {
                DateTime? timeStamp;
                int[] screenCoord = GetEyeScreenCoordinatesUsingSpatialPointer(prevFrame, out timeStamp);
                if (screenCoord != null && timeStamp != null)
                {
                    bool sameData = EqualsUpToMillisecond(lastEyeGazeTimeStamp, timeStamp.Value);
                    if (!sameData)
                    {   
                        sendGazeData(screenCoord, timeStamp.Value, 25);
                        lastEyeGazeTimeStamp = timeStamp.Value;
                    }
                }
            }
            else if (success)
            {
                firstFrame = false;
                prevFrame = currentFrame;
                DateTime? timeStamp;
                int[] screenCoord = GetEyeScreenCoordinatesUsingSpatialPointer(currentFrame, out timeStamp);
                if (screenCoord != null && timeStamp != null)
                {
                    bool sameData = EqualsUpToMillisecond(lastEyeGazeTimeStamp, timeStamp.Value);
                    if (!sameData)
                    {
                        sendGazeData(screenCoord, timeStamp.Value, 25);
                        lastEyeGazeTimeStamp = timeStamp.Value;
                    } 
                }
            }

            DateTime currentTime = DateTime.UtcNow;
            TimeSpan timeSinceLastEyeGazePoint = currentTime - lastEyeGazeTimeStamp;
            if (timeSinceLastEyeGazePoint.TotalMilliseconds >= 5000 && lastEyeGazeTimeStamp != DateTime.MinValue) 
            {
                if (OnGazeDataStopped != null)
                {
                    OnGazeDataStopped();
                }
            }
        }
    }

    private void sendGazeData(int[] screenCoord, DateTime timeStamp, int timeStampLength)
    {
        string formattedEyeTrackingData = screenCoord[0].ToString().PadLeft(4) + screenCoord[1].ToString().PadLeft(4);
        string timeStampFormatted = timeStamp.ToString("MM/dd/yyyy HH:mm:ss.fff").PadLeft(timeStampLength);
        byte[] Data = Encoding.UTF8.GetBytes(timeStampFormatted + formattedEyeTrackingData);
        string dataLength = Data.Length.ToString().PadLeft(SocketData.HeaderSize);
        byte[] dataLengthBytes = Encoding.UTF8.GetBytes(dataLength);
        
        socket.SendData(dataLengthBytes);
        socket.SendData(Data);
    }

    private bool EqualsUpToMillisecond(DateTime dt1, DateTime dt2)
    {
        return dt1.Year == dt2.Year && dt1.Month == dt2.Month && dt1.Day == dt2.Day &&
               dt1.Hour == dt2.Hour && dt1.Minute == dt2.Minute && dt1.Second == dt2.Second && dt1.Millisecond == dt2.Millisecond;
    }

    private int[] GetEyeScreenCoordinatesUsingSpatialPointer(MediaFrameReference mediaFrameRef, out DateTime? timeStamp)
    {
        var cameraCoordinateSystem = mediaFrameRef.CoordinateSystem;
        var cameraIntrinsics = mediaFrameRef.VideoMediaFrame.CameraIntrinsics;
        
        if (cameraCoordinateSystem != null)
        {
            SpatialPointerPose pointerPose = SpatialPointerPose.TryGetAtTimestamp(cameraCoordinateSystem, 
                                                                                  PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));
            var eyes = pointerPose.Eyes;
        
            if (eyes != null)
            {
                var eyeGaze = eyes.Gaze;

                if (eyeGaze != null)
                {
                    var eyeGazeDirection = eyeGaze.Value.Direction;

                    var adjustedEyeGazeDirectionRay = changeVectorAngle(eyeGazeDirection, gazeVectorAngleAdjustment);

                    if (sendToQueue)
                    {
                        gazeDataQueue.Enqueue(new GazeData(cameraCoordinateSystem, eyeGaze.Value.Origin, adjustedEyeGazeDirectionRay));
                    }

                    var eyeGazePoint = System.Numerics.Vector3.Add(adjustedEyeGazeDirectionRay, eyeGaze.Value.Origin);
                    eyeGazePoint = new System.Numerics.Vector3(eyeGazePoint.X, eyeGazePoint.Y, -eyeGazePoint.Z);
                    var screenCoordinates = cameraIntrinsics.ProjectOntoFrame(eyeGazePoint);

                    int xCoord = (int)Math.Round(screenCoordinates.X);
                    int yCoord = (int)Math.Round(screenCoordinates.Y);

                    timeStamp = eyes.UpdateTimestamp.TargetTime.UtcDateTime;
                    return new int[] {xCoord, yCoord};
                }
                else 
                {
                    timeStamp = null;
                    return null;
                }
            }
            else 
            {
                timeStamp = null;
                return null;
            }
        }
        else 
        {
            timeStamp = null;
            return null;

        }
    }

    private System.Numerics.Vector3 changeVectorAngle(System.Numerics.Vector3 vector, double angleDegrees)
    {
        if (angleDegrees == 0)
        {
            return vector;
        }

        var normal = System.Numerics.Vector3.Cross(vector, new System.Numerics.Vector3(0f, 1f, 0f));
        var unitNormal = normal / normal.Length();
        var ax = System.Numerics.Vector3.Cross(unitNormal, vector);
        var angleRadians = angleDegrees * (Math.PI / 180.0);
        return System.Numerics.Vector3.Multiply(vector, (float)Math.Cos(angleRadians)) + System.Numerics.Vector3.Multiply(ax, (float)Math.Sin(angleRadians));
    }

    public ConcurrentQueue<GazeData> getQueue()
    {
         return gazeDataQueue;
    }
#endif

    public void StopSendingData()
    {
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
        socket.CloseConnection();
#endif
    }
}
