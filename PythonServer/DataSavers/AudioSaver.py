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
        self.audio_frame_queue = queue.Queue()
        self.is_running = True
        self.lock = threading.Lock()

    def save_timestamp(self, timestamp):
        txt_file = open(self.folder_path / "audio_start_timestamp.txt", "w")
        txt_file.write(timestamp.lstrip())
        txt_file.close()

    def start(self, channels, audio_format, frequency):
        save_thread = threading.Thread(target=self.save_audio_data, args=(channels, audio_format, frequency))
        save_thread.start()

    def stop(self):
        self.lock.acquire()
        self.is_running = False
        self.lock.release()

    def save_audio_data(self, channels, audio_format, frequency):
        audio_data_pickle_file = open(self.folder_path / "audio_data.pkl", "wb")
        # serialize the audio meta data into the pickle file
        pickle.dump(channels, audio_data_pickle_file)
        pickle.dump(audio_format, audio_data_pickle_file)
        pickle.dump(frequency, audio_data_pickle_file)

        while True:
            try:
                frames = []
                while len(frames) < frequency:
                    audio_frame = self.audio_frame_queue.get(block=True, timeout=1)
                    frames.append(audio_frame)
                pickle.dump(frames, audio_data_pickle_file)
            except queue.Empty:
                if not self.is_running:
                    audio_data_pickle_file.close()
                    break

    def send_to_queue(self, audio_frame):
        self.audio_frame_queue.put(audio_frame)

