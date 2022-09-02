import os
import socket
from threading import Thread
from datetime import datetime
from pathlib import Path
from Sockets.AudioSocket import AudioSocket
from Sockets.VideoSocket import VideoSocket
from Sockets.EyeGazeSocket import EyeGazeSocket
from DataSavers.VideoSaver import VideoSaver
from DataSavers.AudioSaver import AudioSaver
from DataSavers.GazeDataSaver import GazeDataSaver

BASE_DATA_FOLDER = "./Presentation"

# sockets
HOST = socket.gethostbyname(socket.gethostname())
VIDEO_PORT = 12345
AUDIO_PORT = 12346
GAZE_PORT = 12347

# data handling
HEADER_SIZE = 10
BUFFER_SIZE = 32768

# audio
FORMAT = 8
CHANNELS = 1
FREQUENCY = 44100

calibration_mode = False


def main():
    server = Server()
    server.start()


class Server:
    def __init__(self):
        # Create a folder for storing the video, audio and eye gaze data streamed in this session
        data_storage_folder = Path(BASE_DATA_FOLDER) / f"{datetime.strftime(datetime.now(), '%m-%d-%Y %I_%M_%S')}"
        os.makedirs(data_storage_folder)

        # Create the respective data savers for each type of incoming data
        video_saver = VideoSaver(str(data_storage_folder))
        audio_saver = AudioSaver(str(data_storage_folder))
        gaze_saver = GazeDataSaver(str(data_storage_folder),is_calibration=calibration_mode)

        # Create a socket for each data stream (same IP but different port)
        self.video_socket = VideoSocket(HOST, VIDEO_PORT, HEADER_SIZE, BUFFER_SIZE, video_saver)
        self.audio_socket = AudioSocket(HOST, AUDIO_PORT, BUFFER_SIZE, FORMAT, CHANNELS, FREQUENCY, audio_saver)
        self.gaze_socket = EyeGazeSocket(HOST, GAZE_PORT, BUFFER_SIZE, HEADER_SIZE,
                                         gaze_saver, is_calibration=calibration_mode)

    def start(self):
        # Start the sockets different threads for parallelization
        Thread(target=self.video_socket.start).start()
        Thread(target=self.audio_socket.start).start()
        Thread(target=self.gaze_socket.start).start()


if __name__ == "__main__":
    main()
