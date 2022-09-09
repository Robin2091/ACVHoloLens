import csv
import datetime
import cv2
import os
import ffmpeg
from pathlib import Path


class DataTools:
    def __init__(self, data_folder):
        self.data_folder = Path(data_folder)

    def create_visualization(self, video_folder):
        os.makedirs(self.data_folder / "output")

        frame_timestamps = self.load_frame_timestamps()
        gaze_data_timestamps = self.load_gaze_data()
        audio_data_timestamps = self.get_audio_start_timestamps()
        frame_timestamps_split = self.split_frames_between_pauses(frame_timestamps)

        self.generate_images_with_gaze(frame_timestamps, gaze_data_timestamps, video_folder)
        self.generate_concat_files(frame_timestamps_split, video_folder)
        self.generate_video_files(video_folder, len(frame_timestamps_split))
        self.combine_video_and_audio_files(frame_timestamps_split, audio_data_timestamps, video_folder)
        self.generate_video_concat_file()
        self.concatenate_videos()

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
                timestamp = datetime.datetime.strptime(row[1].lstrip(), "%m-%d-%Y %H:%M:%S.%f")
                file_name = row[0]
                frame_timestamp_data.append((file_name, timestamp))
        return frame_timestamp_data

    def load_gaze_data(self):
        csv_file = open(self.data_folder / "gaze_data.csv", 'r', newline='')
        csv_reader = csv.reader(csv_file)
        gaze_data = []
        header_read = False
        for row in csv_reader:
            if not header_read:
                header_read = True
            else:
                timestamp = datetime.datetime.strptime(row[0].lstrip(), "%m-%d-%Y %H:%M:%S.%f")
                gaze = [int(row[1]), int(row[2])]
                is_valid_eye_coord = 0 <= gaze[0] <= 2272 and 0 <= gaze[1] <= 1278
                if is_valid_eye_coord:
                    gaze_data.append((timestamp, gaze))
        return gaze_data

    @staticmethod
    def split_frames_between_pauses(frame_timestamps):
        # time in seconds between consecutive timestamps for a pause to have occurred
        pause_time = 2
        frame_timestamps_split = []
        split = []
        prev_frame_timestamp = None
        for frame_timestamp in frame_timestamps:
            if prev_frame_timestamp is None:
                prev_frame_timestamp = frame_timestamp
                split.append(frame_timestamp)
            else:
                time_diff = frame_timestamp[1] - prev_frame_timestamp[1]
                if time_diff >= datetime.timedelta(seconds=pause_time):
                    frame_timestamps_split.append(split)
                    split = [frame_timestamp]
                else:
                    split.append(frame_timestamp)
                prev_frame_timestamp = frame_timestamp
        frame_timestamps_split.append(split)
        return frame_timestamps_split

    def generate_concat_files(self, frame_timestamp_data, folder):
        for i, frame_timestamps in enumerate(frame_timestamp_data):
            prev_frame = None
            txt_file = open(self.data_folder / folder / f"concat_{i}.txt", 'w')
            for frame in frame_timestamps:
                if prev_frame is not None:
                    time_diff = (frame[1].timestamp() - prev_frame[1].timestamp()) * 1000
                    txt_file.write("duration " + str(round(time_diff)) + "\n")
                txt_file.write("file " + frame[0] + "\n")
                prev_frame = frame

    def generate_video_files(self, folder, num_videos):
        for i in range(num_videos):
            ffmpeg.input(str(self.data_folder / folder / f"concat_{i}.txt"),
                         f="concat").output(str(self.data_folder / folder / f"video_from_frames_{i}.avi"),
                                            r='1000',
                                            vf="settb=1/1000,setpts=PTS/1000",
                                            vsync="vfr").run()

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

    def combine_video_and_audio_files(self, frame_timestamp_data, audio_timestamp_data, video_folder):
        for i, audio_file_name in enumerate(audio_timestamp_data.keys()):
            audio_start_timestamp = audio_timestamp_data[audio_file_name]
            video_start_timestamp = frame_timestamp_data[i][0][1]
            time_diff = video_start_timestamp.timestamp() - audio_start_timestamp.timestamp()
            if time_diff > 0:
                audio_input = ffmpeg.input(str(self.data_folder / "audio_data" / audio_file_name))
                audio_cut = audio_input.audio.filter('atrim', start=time_diff)
                audio_output = ffmpeg.output(audio_cut, str(self.data_folder /
                                                            "audio_data" / f"{audio_file_name[:-4]}_cut.wav"))
                ffmpeg.run(audio_output)

                input_video = ffmpeg.input(str(self.data_folder / video_folder / f"video_from_frames_{i}.avi"))
                input_audio = ffmpeg.input(str(self.data_folder / "audio_data" / f"{audio_file_name[:-4]}_cut.wav"))
                ffmpeg.output(input_video,
                              input_audio,
                              str(self.data_folder / "output" / f"output_video_audio_{i}.mkv"),
                              vcodec='copy',
                              acodec='copy').run()
            else:
                time_diff = audio_start_timestamp.timestamp() - video_start_timestamp.timestamp()
                video_input = ffmpeg.input(str(self.data_folder / video_folder / f"video_from_frames_{i}.avi"))
                video_cut = video_input.video.filter('trim', start=time_diff)
                video_output = ffmpeg.output(video_cut, str(self.data_folder /
                                                            video_folder / f"video_from_frames_{i}_cut.avi"))
                ffmpeg.run(video_output)

                input_video = ffmpeg.input(str(self.data_folder / video_folder / f"video_from_frames_{i}_cut.avi"))
                input_audio = ffmpeg.input(str(self.data_folder / "audio_data" / audio_file_name))
                ffmpeg.output(input_video,
                              input_audio,
                              str(self.data_folder / "output" / f"output_video_audio_{i}.mkv"),
                              vcodec='copy',
                              acodec='copy').run()

    def generate_video_concat_file(self):
        video_files = os.listdir(self.data_folder / "output")
        txt_file = open(self.data_folder / "output" / "concat.txt", 'w')
        for video_file in video_files:
            txt_file.write("file " + video_file + "\n")
        txt_file.close()

    def concatenate_videos(self):
        ffmpeg.input(str(self.data_folder / "output" / "concat.txt"),
                     f="concat").output(str(self.data_folder / "output" / "final.mkv")).run()

    def get_audio_start_timestamps(self):
        timestamps_file = open(self.data_folder / "audio_data" / "audio_start_timestamps.txt", "r")
        time_stamps = [datetime.datetime.strptime(time_stamp.rstrip().lstrip(), "%m-%d-%Y %H:%M:%S.%f") for\
                       time_stamp
                       in timestamps_file.readlines()]
        file_names_file = open(self.data_folder / "audio_data" / "audio_file_names.txt", "r")
        names = [name.rstrip().lstrip() for name in file_names_file.readlines()]
        audio_start_timestamps = {}
        for name, timestamp in (zip(names, time_stamps)):
            audio_start_timestamps[name] = timestamp
        return audio_start_timestamps

    @staticmethod
    def convert_to_png(folder):
        filenames = os.listdir(folder)
        for filename in filenames:
            image = cv2.imread(folder + filename)
            cv2.imwrite(folder + filename[:-3] + "png", image)

