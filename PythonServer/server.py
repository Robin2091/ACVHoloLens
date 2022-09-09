import os
import argparse
from threading import Thread
from datetime import datetime
from pathlib import Path
import constants
from Sockets.AudioSocket import AudioSocket
from Sockets.VideoSocket import VideoSocket
from Sockets.EyeGazeSocket import EyeGazeSocket
from DataSavers.VideoSaver import VideoSaver
from DataSavers.AudioSaver import AudioSaver
from DataSavers.GazeDataSaver import GazeDataSaver


def get_args_parser():
    arg_parser = argparse.ArgumentParser(description="Active Computer Vision HoloLens 2", add_help=False)
    arg_parser.add_argument('--base_data_folder', type=str, help="Path to the folder that will store data "
                                                                 "for each run")
    arg_parser.add_argument('--is_calibration', default=False, action='store_true',
                            help="Whether the HoloLens 2 App is on"
                                 "Calibration mode")
    return arg_parser


def main(parser_args):
    server = Server(parser_args)
    server.start()


class Server:
    def __init__(self, parser_args):
        # Create a folder for storing the video, audio and eye gaze data streamed in this session
        data_storage_folder = Path(parser_args.base_data_folder) / \
                              f"{datetime.strftime(datetime.now(), '%m-%d-%Y %I_%M_%S')}"
        os.makedirs(data_storage_folder)

        # Create the respective data savers for each type of incoming data
        video_saver = VideoSaver(str(data_storage_folder))
        audio_saver = AudioSaver(str(data_storage_folder))
        gaze_saver = GazeDataSaver(str(data_storage_folder), is_calibration=parser_args.is_calibration)

        # Create a socket for each data stream (same IP but different port)
        self.video_socket = VideoSocket(constants.HOST, constants.VIDEO_PORT, constants.HEADER_SIZE,
                                        constants.BUFFER_SIZE, video_saver)
        self.audio_socket = AudioSocket(constants.HOST, constants.AUDIO_PORT, constants.BUFFER_SIZE,
                                        constants.HEADER_SIZE, constants.FORMAT,
                                        constants.CHANNELS, constants.FREQUENCY, audio_saver)
        self.gaze_socket = EyeGazeSocket(constants.HOST, constants.GAZE_PORT, constants.BUFFER_SIZE,
                                         constants.HEADER_SIZE, gaze_saver, is_calibration=parser_args.is_calibration)

    def start(self):
        # Start the sockets different threads for parallelization
        Thread(target=self.video_socket.start).start()
        Thread(target=self.audio_socket.start).start()
        Thread(target=self.gaze_socket.start).start()


if __name__ == "__main__":
    parser = argparse.ArgumentParser('HololensACV', parents=[get_args_parser()])
    args = parser.parse_args()
    main(args)
