import os
import threading
import queue
import csv
import cv2
from pathlib import Path


class VideoSaver:
    def __init__(self, folder_path):
        self.video_queue = queue.Queue()
        self.lock = threading.Lock()
        self.is_running = True

        os.makedirs(Path(folder_path) / "frames")
        self.folder_path = Path(folder_path) / "frames"

    def start(self):
        save_thread = threading.Thread(target=self.save_video)
        save_thread.start()

    def stop(self):
        self.lock.acquire()
        self.is_running = False
        self.lock.release()


    def save_video(self):
        filename = 0
        csv_file = open(str(self.folder_path.parent / "frame_timestamps.csv"), 'a', newline='')
        csv_writer = csv.writer(csv_file)
        header = ["Filename", "Timestamp"]
        header_written = False
        num_rows_written = 0
        while True:
            try:
                image, time_stamp = self.video_queue.get(block=True, timeout=1)
                if not header_written:
                    csv_writer.writerow(header)
                    header_written = True
                file_path = self.folder_path / f"{filename}.png"
                csv_writer.writerow([f"{filename}.png", time_stamp])
                cv2.imwrite(str(file_path), image)
                filename += 1
                if num_rows_written % 30 == 0:
                    csv_file.flush()
                    os.fsync(csv_file)
                num_rows_written += 1
            except queue.Empty:
                if not self.is_running:
                    csv_file.close()
                    break

    def send_to_queue(self, image, time_stamp):
        self.video_queue.put((image, time_stamp))
