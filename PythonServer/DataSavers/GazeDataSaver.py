import threading
import queue
import csv
from pathlib import Path


class GazeDataSaver:
    def __init__(self, file_path, is_calibration=False):
        self.gaze_data_queue = queue.Queue()
        self.file_path = Path(file_path) / "gaze_data.csv"
        self.lock = threading.Lock()
        self.is_running = True
        self.is_calibration = is_calibration

    def start(self):
        save_thread = threading.Thread(target=self.save_data)
        save_thread.start()

    def stop(self):
        self.lock.acquire()
        self.is_running = False
        self.lock.release()

    def save_data(self):
        header_written = False
        if self.is_calibration:
            header = ['Timestamp', 'Angle', 'GazeX', 'GazeY']
        else:
            header = ['Timestamp', 'GazeX', 'GazeY']
        csv_file = open(self.file_path, 'w', newline='')
        writer = csv.writer(csv_file)
        while self.is_running:
            try:
                if self.is_calibration:
                    time_stamp, angle, gaze = self.gaze_data_queue.get(block=True, timeout=1)
                else:
                    time_stamp, gaze = self.gaze_data_queue.get(block=True, timeout=1)
                if not header_written:
                    writer.writerow(header)
                    header_written = True
                if self.is_calibration:
                    writer.writerow([time_stamp, angle, gaze[0], gaze[1]])
                else:
                    writer.writerow([time_stamp, gaze[0], gaze[1]])
            except queue.Empty:
                if not self.is_running:
                    csv_file.close()
                    break

    def send_to_queue(self, time_stamp, gaze):
        self.gaze_data_queue.put((time_stamp, gaze))

    def send_to_queue_cal_mode(self, time_stamp, angle, gaze):
        self.gaze_data_queue.put((time_stamp, angle, gaze))
