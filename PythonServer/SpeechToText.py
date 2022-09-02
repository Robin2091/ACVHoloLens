from vosk import Model, KaldiRecognizer, SetLogLevel
import wave
import ast
from pathlib import Path
from DataTools import DataTools
import datetime
import csv
import cv2
import json
import ntpath

# data_folder = "./test"
# tools = DataTools(data_folder)
#
# gaze_data = tools.load_gaze_data()
# frame_timestamps = tools.load_frame_timestamps()
# audio_timestamp = datetime.datetime.strptime(tools.load_audio_timestamp().lstrip(), "%m-%d-%Y %H:%M:%S.%f")
# image_folder = Path("./test") / "frames"
# object_categories = ["book", "laptop"]
# output_file = Path("./test") / "dataset.csv"
# audio_file = Path("./test") / "audio_cut.wav"


class SpeechToTextConvertor:
    def __init__(self, base_data_folder):
        self.base_data_folder = base_data_folder
        self.data_tools = DataTools(self.base_data_folder)
        self.gaze_timestamps_points = self.data_tools.load_gaze_data()
        self.frames_timestamps = self.data_tools.load_frame_timestamps()
        self.audio_start_timestamp = datetime.datetime.strptime(self.data_tools.load_audio_timestamp().lstrip(),
                                                                "%m-%d-%Y %H:%M:%S.%f")
        self.image_folder = Path(self.base_data_folder) / "frames"
        self.categories_file = Path(self.base_data_folder) / "categories.txt"
        self.audio_file = Path(self.base_data_folder) / "audio.wav"
        self.output = Path(self.base_data_folder) / "dataset.csv"
        self.speech = []

    def create_dataset(self):
        self.generate_text_from_speech()
        self.create_dataset_file()

    def create_dataset_file(self):
        csv_file = open(self.output, 'w', newline='')
        writer = csv.writer(csv_file)
        header = ['Image_Path', "Label" 'GazeX', 'GazeY']
        writer.writerow(header)
        for obj_cat in self.get_categories_list():
            instances = self.get_word_instances(obj_cat)
            for instance in instances:
                valid_imgs = self.get_images_from_word(instance)
                valid_gaze = self.get_gaze_data_from_label(instance)
                valid_single_gaze = self.choose_a_gaze(instance, valid_gaze)
                valid_img = self.choose_an_image(instance, valid_imgs)
                # img = cv2.imread(str(self.image_folder / valid_img[0]))
                # for gaze in valid_gaze:
                #     img = cv2.circle(img, (int(gaze[1][0]), int(gaze[1][1])), 20, (255, 0, 0), 5)
                # img = cv2.resize(img, (1280, 720))
                # #cv2.imshow("im", img)
                # #cv2.waitKey(0)
                if valid_single_gaze is not None and valid_img is not None:
                    row = [self.image_folder / valid_img[0], obj_cat, valid_single_gaze[1][0], valid_single_gaze[1][1]]
                    writer.writerow(row)
        csv_file.close()

    def generate_text_from_speech(self):
        wf = wave.open(str(self.audio_file), "rb")
        assert (wf.getnchannels() == 1)
        assert (wf.getsampwidth() == 2)
        assert (wf.getcomptype() == "NONE")
        if wf.getnchannels() != 1 or wf.getsampwidth() != 2 or wf.getcomptype() != "NONE":
            print("Audio file must be WAV format mono PCM.")
            exit(1)

        model = Model(lang="en-us")

        rec = KaldiRecognizer(model, wf.getframerate())
        rec.SetWords(True)
        rec.SetPartialWords(True)

        while True:
            data = wf.readframes(44100)
            if len(data) == 0:
                break
            if rec.AcceptWaveform(data):
                self.speech.append(rec.Result())

    def visualize_dataset(self):
        csv_file = open(str(Path(self.output)), 'r', newline='')
        csv_reader = csv.reader(csv_file)
        header_read = False
        for row in csv_reader:
            if not header_read:
                header_read = True
            else:
                image_path, label, gaze_x, gaze_y = row
                im = cv2.imread(image_path)
                im = cv2.circle(im, (int(gaze_x), int(gaze_y)), 15, (0, 0, 0), 8)
                cv2.putText(im, label, (int(gaze_x) - 10, int(gaze_y) - 20),
                            cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 0), 2, cv2.LINE_AA)
                im = cv2.resize(im, (1280, 720))
                cv2.imshow("image", im)
                cv2.waitKey(0)

    def get_word_instances(self, word_query):
        instances = []
        for results in self.speech:
            results = ast.literal_eval(results)
            try:
                list_of_words = results["result"]
                for word_data in list_of_words:
                    if word_data["word"] == word_query:
                        instances.append(word_data)
            except KeyError:
                print("No text was generated this instance")
                continue
        return instances

    def get_images_from_word(self, word):
        start = self.audio_start_timestamp + datetime.timedelta(seconds=word['start'])
        end = self.audio_start_timestamp + datetime.timedelta(seconds=word['end'])
        error = datetime.timedelta(milliseconds=200)
        valid_imgs = []
        for frame_tmsp in self.frames_timestamps:
            if start - error <= frame_tmsp[1] <= end + error:
                valid_imgs.append(frame_tmsp)
        if len(valid_imgs) == 0:
            return None
        else:
            return valid_imgs

    def get_gaze_data_from_label(self, word):
        start = self.audio_start_timestamp + datetime.timedelta(seconds=word['start'])
        end = self.audio_start_timestamp + datetime.timedelta(seconds=word['end'])
        error = datetime.timedelta(milliseconds=200)

        valid_gaze_data = []
        for gaze_data_point in self.gaze_timestamps_points:
            if start - error <= gaze_data_point[0] <= end + error:
                valid_gaze_data.append(gaze_data_point)
        return valid_gaze_data

    def choose_an_image(self, word, valid_frames):
        if valid_frames is None:
            return None

        start = self.audio_start_timestamp + datetime.timedelta(seconds=word['start'])
        end = self.audio_start_timestamp + datetime.timedelta(seconds=word['end'])
        middle = start + (end - start) / 2

        frame_differences = []
        for frame in valid_frames:
            frame_differences.append(frame[1] - middle)
        return valid_frames[frame_differences.index(min(frame_differences))]

    def choose_a_gaze(self, word, valid_gazes):
        if valid_gazes is None:
            return None
        start = self.audio_start_timestamp + datetime.timedelta(seconds=word['start'])
        end = self.audio_start_timestamp + datetime.timedelta(seconds=word['end'])
        middle = start + (end - start) / 2

        gaze_timestamp_diffs = []
        for gaze in valid_gazes:
            gaze_timestamp_diffs.append(gaze[0] - middle)

        return valid_gazes[gaze_timestamp_diffs.index(min(gaze_timestamp_diffs))]

    def get_categories_list(self):
        categories = []
        with open(self.categories_file, "r") as f:
            for line in f.readlines():
                cat = line.rstrip()
                categories.append(cat)
        return categories


def get_categories_list(categories_file):
    categories = []
    with open(categories_file, "r") as f:
        for line in f.readlines():
            cat = line.rstrip()
            categories.append(cat)
    return categories


def get_categories_json(categories_file):
    with open(categories_file, "r") as f:
        return json.loads(f.read())


def get_cat_id_from_label(label, category_dict):
    for category in category_dict:
        if category["name"] == label:
            return category["id"]


def generate_coco_dataset_format(dataset_file, category_dict, base_folder):
    info, licenses, images, annotations = dict(), [], [], []
    info["description"] = "Hololens Dataset"
    info["url"] = ""
    info["version"] = "1.0"
    info["year"] = 2022
    info["contributor"] = ""
    info["date_created"] = ""

    csv_file = open(dataset_file, 'r', newline='')
    csv_reader = csv.reader(csv_file)
    ann_id = 0

    header_read = False
    for row in csv_reader:
        if not header_read:
            header_read = True
        else:
            image_path, label, gaze_x, gaze_y = row
            image_dict = {"license": "", "file_name": ntpath.basename(image_path)}
            image = cv2.imread(image_path)
            image_dict["width"] = image.shape[1]
            image_dict["height"] = image.shape[0]
            image_dict["date_captured"] = ""
            image_dict["flickr_url"] = ""
            image_dict["indicator"] = 0
            image_dict["label_type"] = "pointsK"
            image_dict["id"] = int(image_dict["file_name"][:-4])
            images.append(image_dict)
            ann = {"point": (int(gaze_x), int(gaze_y)), "bbox": [2, 2, 4, 4], "iscrowd": 0, "area": 16.0, "image_id": image_dict["id"],
                   "category_id": get_cat_id_from_label(label, category_dict), "id": ann_id}
            annotations.append(ann)
            ann_id += 1

    train_data = {"info": info, "licenses": licenses, "images": images, "categories": category_dict,
                  "annotations": annotations}
    output_file = Path(base_folder) / "instances_train2017.json"
    # annotation_array = np.array(annotations)
    # np.random.shuffle(annotation_array)
    # train_anns, val_anns = np.split(annotation_array, [int(0.5 * annotation_array.size)])
    # train_file = "./test/instances_train2017.json"
    # val_file = "./test/instances_val2017.json"
    # train_data = {"info": info, "licenses": licenses, "images": images, "categories": category_dict,
    #               "annotations": train_anns.tolist()}
    # val_data = {"info": info, "licenses": licenses, "images": images, "categories": category_dict,
    #             "annotations": val_anns.tolist()}

    # with open(train_file, 'w') as f:
    #     print('writing to json output:', train_file)
    #     json.dump(train_data, f, sort_keys=True)

    with open(output_file, 'w') as f:
        print('writing to json output:', output_file)
        json.dump(train_data, f, sort_keys=True)

# generate_coco_dataset_format("./test/dataset.csv", get_categories_json("./test/categories.json"))
# generate_speech(audio_file)
