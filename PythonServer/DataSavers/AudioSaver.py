import csv
import os
import soundfile as sf
import numpy as np
from pathlib import Path
import pickle
import threading
import queue


# Convert floating point audio data values to int16
def as_int16(data):
    data = data.clip(-1, 1)
    return (data * 32767).astype(np.int16)


class AudioSaver:
    def __init__(self, folder_path):
        self.folder_path = Path(folder_path)
        self.audio_data_folder = Path(folder_path) / "audio_data"
        os.makedirs(self.audio_data_folder)
        self.audio_frame_queue = queue.Queue()
        self.is_running = True
        self.lock = threading.Lock()

    def save_timestamp(self, timestamp):
        txt_file = open(self.audio_data_folder / "audio_start_timestamps.txt", "a+")
        txt_file.write(timestamp.lstrip() + "\n")
        txt_file.close()
        #self.lock.acquire()
        #self.most_recent_timestamp = timestamp
        #self.lock.release()

    def start(self, channels, audio_format, frequency):
        #csv_file = open(self.audio_data_folder / "audio_start_timestamps.csv", "w")
        #writer = csv.writer(csv_file)
        #writer.writerow(['File_Name', 'Start_Time'])
        #csv_file.close()
        save_thread = threading.Thread(target=self.save_audio_data, args=(channels, audio_format, frequency))
        save_thread.start()

    def stop(self):
        self.lock.acquire()
        self.is_running = False
        self.lock.release()

    def save_audio_data(self, channels, audio_format, frequency):
        i = 0
        txt_file = open(self.audio_data_folder / "audio_file_names.txt", "a")
        while True:
            file_name = f"audio_data_{i}.wav"
            txt_file.write(file_name + "\n")
            # initialize file by writing no audio frames
            sf.write(self.audio_data_folder / file_name, [], frequency, "PCM_16")
            audio_file = sf.SoundFile(self.audio_data_folder / file_name, "r+")

            received_data = False
            num_frames_written = 0
            while True:
                try:
                    frames = []
                    while len(frames) < frequency:
                        audio_frame = self.audio_frame_queue.get(block=True, timeout=2)
                        frames.append(audio_frame)
                    audio_file.seek(0, sf.SEEK_END)
                    audio_file.write(np.array(frames))
                    if num_frames_written % 1000 == 0:
                        audio_file.flush()
                    received_data = True
                except queue.Empty:
                    if received_data:
                        audio_file.close()
                        break
                    if not self.is_running:
                        break
            if not self.is_running:
                txt_file.close()
                break
            i += 1
    # def save_audio_data(self, channels, audio_format, frequency):
    #     i = 0
    #     while True:
    #         file_name = f"audio_data_{i}.pkl"
    #         audio_data_pickle_file = open(self.audio_data_folder / file_name, "wb")
    #         # serialize the audio meta data into the pickle file
    #         pickle.dump(channels, audio_data_pickle_file)
    #         pickle.dump(audio_format, audio_data_pickle_file)
    #         pickle.dump(frequency, audio_data_pickle_file)
    #
    #         received_data = False
    #         num_frames_written = 0
    #         while True:
    #             try:
    #                 frames = []
    #                 while len(frames) < frequency:
    #                     audio_frame = self.audio_frame_queue.get(block=True, timeout=1)
    #                     frames.append(audio_frame)
    #                 pickle.dump(frames, audio_data_pickle_file)
    #                 if num_frames_written % 1000 == 0:
    #                     audio_data_pickle_file.flush()
    #                     os.fsync(audio_data_pickle_file)
    #                 received_data = True
    #             except queue.Empty:
    #                 print("queue empty")
    #                 if received_data:
    #                     audio_data_pickle_file.close()
    #                     print("new")
    #                     break
    #         i += 1
    #         if not self.is_running:
    #             break

    def send_to_queue(self, audio_frame):
        self.audio_frame_queue.put(audio_frame)

