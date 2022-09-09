from pathlib import Path
from data_utils.DataTools import DataTools
import json
import datetime
import cv2


class DatasetGenerator:
    def __init__(self, data_folder):
        self.data_folder = Path(data_folder)
        self.categories = []
        self.labels = None
        self.frames_timestamps = []
        self.gaze_timestamps_points = []

    def load_data(self):
        self.load_categories()
        self.labels = self.read_json_file()
        data_tools = DataTools(self.data_folder)
        self.frames_timestamps = data_tools.load_frame_timestamps()
        self.gaze_timestamps_points = data_tools.load_gaze_data()

    def generate_dataset(self):
        annotations = []
        for audio_file in self.labels:
            timestamps = audio_file['timestamps']
            speech = audio_file['speech']
            audio_start_timestamp = datetime.datetime.strptime(timestamps[0], "%m-%d-%Y %H:%M:%S.%f")
            for cat in self.categories:
                cat_instances = self.get_word_instances(speech, cat)
                for label in cat_instances:
                    data = {'label': cat}
                    valid_frames = self.get_possible_images_from_label(label, audio_start_timestamp)
                    valid_gazes = self.get_possible_gazes_from_label(label, audio_start_timestamp)
                    data_ann = self.sort_gaze_and_images(valid_frames, valid_gazes)
                    if data_ann is not None:
                        data_ann = self.prep_data_for_json(data_ann)
                        data['annotations'] = data_ann
                        annotations.append(data)
        data_json = json.dumps(annotations, indent=4)
        with open(self.data_folder / "annotations.json", "w") as file:
            file.write(data_json)

    def visualize_dataset(self):
        with open(self.data_folder / "annotations.json", "r") as file:
            json_data = json.load(file)
        for instance in json_data:
            label = instance['label']
            annotations = instance['annotations']
            for ann in annotations:
                frame = ann['frame']
                gazes = ann['gazes']
                image = cv2.imread(str(self.data_folder / "frames" / frame))
                image = cv2.putText(image, label, (0, 20),
                            cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2, cv2.LINE_AA)
                for gaze in gazes:
                    image = cv2.circle(image, gaze, 10, (0, 255, 0), 8)
                if gazes:
                    image = cv2.resize(image, (1280, 720))
                    cv2.imshow(frame, image)
                    cv2.waitKey(0)


    def get_possible_images_from_label(self, label, audio_start_timestamp, error=None):
        start = audio_start_timestamp + datetime.timedelta(seconds=label['start'])
        end = audio_start_timestamp + datetime.timedelta(seconds=label['end'])
        if error is not None:
            error = datetime.timedelta(milliseconds=error)
        valid_imgs = []
        for frame_tmsp in self.frames_timestamps:
            if error is not None:
                if start - error <= frame_tmsp[1] <= end + error:
                    valid_imgs.append(frame_tmsp)
            else:
                if start <= frame_tmsp[1] <= end:
                    valid_imgs.append(frame_tmsp)
        if len(valid_imgs) == 0:
            return None
        else:
            return valid_imgs

    def get_possible_gazes_from_label(self, label, audio_start_timestamp, error=None):
        start = audio_start_timestamp + datetime.timedelta(seconds=label['start'])
        end = audio_start_timestamp + datetime.timedelta(seconds=label['end'])
        if error is not None:
            error = datetime.timedelta(milliseconds=200)

        valid_gaze_data = []
        for gaze_data_point in self.gaze_timestamps_points:
            if error is not None:
                if start - error <= gaze_data_point[0] <= end + error:
                    valid_gaze_data.append(gaze_data_point)
            else:
                if start <= gaze_data_point[0] <= end:
                    valid_gaze_data.append(gaze_data_point)
        if len(valid_gaze_data) == 0:
            return None
        else:
            return valid_gaze_data

    @staticmethod
    def sort_gaze_and_images(valid_imgs, valid_gazes):
        if valid_gazes is None or valid_imgs is None:
            return None
        sorted_data = []
        endpoint = 0
        for frame in valid_imgs:
            data_dict = {'frame': frame}
            image_file_name, frame_timestamp = frame
            gaze_points = []
            for i, gaze_point in enumerate(valid_gazes[endpoint:]):
                endpoint += 1
                timestamp, point = gaze_point
                if frame_timestamp > timestamp:
                    gaze_points.append(gaze_point)
                else:
                    data_dict['gazes'] = gaze_points
                    sorted_data.append(data_dict)
                    break
        return sorted_data

    @staticmethod
    def prep_data_for_json(data):
        new_data = []
        for instance in data:
            data_dict = {'frame': instance['frame'][0]}
            gaze_points = []
            for gaze_point in instance['gazes']:
                gaze_points.append(gaze_point[1])
            data_dict['gazes'] = gaze_points
            new_data.append(data_dict)
        return new_data

    @staticmethod
    def get_word_instances(speech, word_query):
        instances = []
        for results in speech:
            try:
                list_of_words = results["result"]
                for word_data in list_of_words:
                    if word_data["word"] == word_query:
                        instances.append(word_data)
            except KeyError:
                continue
        return instances

    @staticmethod
    def choose_an_image(label, valid_frames, audio_start_timestamp):
        if valid_frames is None:
            return None

        start = audio_start_timestamp + datetime.timedelta(seconds=label['start'])
        end = audio_start_timestamp + datetime.timedelta(seconds=label['end'])
        middle = start + (end - start) / 2

        frame_differences = []
        for frame in valid_frames:
            frame_differences.append(frame[1] - middle)
        return valid_frames[frame_differences.index(min(frame_differences))]

    @staticmethod
    def choose_a_gaze(label, valid_gazes, audio_start_timestamp):
        if valid_gazes is None or valid_gazes == []:
            return None
        start = audio_start_timestamp + datetime.timedelta(seconds=label['start'])
        end = audio_start_timestamp + datetime.timedelta(seconds=label['end'])
        middle = start + (end - start) / 2

        gaze_timestamp_diffs = []
        for gaze in valid_gazes:
            gaze_timestamp_diffs.append(gaze[0] - middle)
        return valid_gazes[gaze_timestamp_diffs.index(min(gaze_timestamp_diffs))]

    def load_categories(self):
        with open(self.data_folder / "categories.txt", "r") as f:
            for line in f.readlines():
                cat = line.rstrip()
                self.categories.append(cat)

    def read_json_file(self):
        with open(self.data_folder / "speech.json", "r") as file:
            json_data = json.load(file)
        return json_data
