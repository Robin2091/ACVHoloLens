import csv
from datetime import datetime
import cv2
import os
from pathlib import Path
import ffmpeg
import wave
import pickle
import numpy as np
import pyaudio


# Convert floating point audio data values to int16
def as_int16(data):
    data = data.clip(-1, 1)
    return (data * 32767).astype(np.int16)


class DataTools:
    def __init__(self, data_folder):
        self.data_folder = Path(data_folder)

    def save_audio_to_wave(self, length_of_file):
        audio_storage_folder = self.data_folder / "audio"
        os.makedirs(audio_storage_folder)

        audio_data_pickle_file = open(self.data_folder / "audio_data.pkl", "rb")
        channels = pickle.load(audio_data_pickle_file)
        audio_format = pickle.load(audio_data_pickle_file)
        frequency = pickle.load(audio_data_pickle_file)
        txt_file = open(self.data_folder / "audio" / "concat.txt", "w")

        num_file = 0
        all_audio_read = False
        while not all_audio_read:
            frames = []
            seconds = 0
            while seconds < length_of_file:
                try:
                    frames_one_second = pickle.load(audio_data_pickle_file)
                except EOFError:
                    all_audio_read = True
                    break
                frames.append(frames_one_second)
                seconds += 1
            num_file += 1
            txt_file.write("file " + f"'{num_file}.wav'" + "\n")
            self.save_audio(frames, f"{num_file}.wav", channels, audio_format, frequency)

    def load_audio_timestamp(self):
        txt_file = open(self.data_folder / "audio_start_timestamp.txt", 'r')
        audio_start_timestamp = txt_file.readline()
        txt_file.close()
        return audio_start_timestamp

    def save_audio(self, frames, file_name, channels, audio_format, frequency):
        # save the audio frames using wave module
        wf = wave.open(str(self.data_folder / "audio" / file_name), 'wb')
        wf.setnchannels(channels)
        wf.setsampwidth(pyaudio.PyAudio().get_sample_size(audio_format))
        wf.setframerate(frequency)
        frames_to_write = as_int16(np.asarray(frames))
        wf.writeframes(frames_to_write.tobytes())
        wf.close()

    def generate_audio_file(self):
        ffmpeg.input(str(self.data_folder / "audio" / "concat.txt"),
                     f="concat").output(str(self.data_folder / "audio.wav"), safe=0, c="copy").run()

    def load_frame_timestamps(self):
        csv_file = open(self.data_folder / "frame_timestamps.csv", 'r', newline='')
        csv_reader = csv.reader(csv_file)
        frame_timestamp_data = []
        header_read = False
        for row in csv_reader:
            if not header_read:
                header_read = True
            else:
                timestamp = datetime.strptime(row[1].lstrip(), "%m-%d-%Y %H:%M:%S.%f")
                file_name = row[0]
                frame_timestamp_data.append((file_name, timestamp))
        return frame_timestamp_data

    def generate_concat_file(self, frame_timestamp_data, folder):
        prev_frame = None
        txt_file = open(self.data_folder / folder / "concat.txt", 'w')
        for frame in frame_timestamp_data:
            if prev_frame is not None:
                time_diff = (frame[1].timestamp() - prev_frame[1].timestamp()) * 1000
                txt_file.write("duration " + str(round(time_diff)) + "\n")
            txt_file.write("file " + frame[0] + "\n")
            prev_frame = frame

    def generate_video_file(self, folder):
        ffmpeg.input(str(self.data_folder / folder / "concat.txt"),
                     f="concat").output(str(self.data_folder / folder/ "video_from_frames.avi"),
                                        r='1000',
                                        vf="settb=1/1000,setpts=PTS/1000",
                                        vsync="vfr").run()

    def load_gaze_data(self):
        csv_file = open(self.data_folder / "gaze_data.csv", 'r', newline='')
        csv_reader = csv.reader(csv_file)
        gaze_data = []
        header_read = False
        for row in csv_reader:
            if not header_read:
                header_read = True
            else:
                timestamp = datetime.strptime(row[0].lstrip(), "%m-%d-%Y %H:%M:%S.%f")
                gaze = [int(row[1]), int(row[2])]
                is_valid_eye_coord = 0 <= gaze[0] <= 2272 and 0 <= gaze[1] <= 1278
                if is_valid_eye_coord:
                    gaze_data.append((timestamp, gaze))
        return gaze_data

    def generate_images_with_gaze(self, frame_timestamps, gaze_data, folder):
        endpoint = 0
        frames = 0
        for frame in frame_timestamps:
            frames += 1
            image_file_name, frame_timestamp = frame
            image_file = self.data_folder / "frames" / image_file_name
            image = cv2.imread(str(image_file))
            for i, gaze_point in enumerate(gaze_data[endpoint:]):
                endpoint += 1
                timestamp, point = gaze_point
                if frame_timestamp > timestamp:
                    cv2.circle(image, point, 10, (0, 0, 255), 5)
                else:
                    print(f"frame:{frames} | points: {i}")
                    break
            cv2.imwrite(str((Path(self.data_folder) / folder / image_file_name)), image)


    def combine_video_and_audio(self, frame_timestamp_data, video_folder, audio_file_name):
        video_start_timestamp = frame_timestamp_data[0][1]
        txt_file = open(self.data_folder / "audio_start_timestamp.txt", 'r')
        audio_start_timestamp = self.load_audio_timestamp()#txt_file.readline()
        txt_file.close()

        audio_start_timestamp = datetime.strptime(audio_start_timestamp.lstrip(), "%m-%d-%Y %H:%M:%S.%f")
        time_diff = video_start_timestamp.timestamp() - audio_start_timestamp.timestamp()

        audio_input = ffmpeg.input(str(self.data_folder / "audio.wav"))
        audio_cut = audio_input.audio.filter('atrim', start=time_diff)
        audio_output = ffmpeg.output(audio_cut, str(self.data_folder / "audio_cut.wav"))
        ffmpeg.run(audio_output)

        input_video = ffmpeg.input(str(self.data_folder / video_folder/ "video_from_frames.avi"))
        input_audio = ffmpeg.input(str(self.data_folder / "audio_cut.wav"))
        ffmpeg.output(input_video,
                      input_audio,
                      str(self.data_folder / "output_video_audio.mkv"),
                      vcodec='copy',
                      acodec='copy').run()


def convert_to_png(folder):
    filenames = os.listdir(folder)
    for filename in filenames:
        image = cv2.imread(folder + filename)
        cv2.imwrite(folder + filename[:-3] + "png", image)

