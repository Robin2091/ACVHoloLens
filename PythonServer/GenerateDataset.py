from SpeechToText import SpeechToTextConvertor
from SpeechToText import generate_coco_dataset_format
from SpeechToText import get_categories_json
from pathlib import Path
import argparse

parser = argparse.ArgumentParser(description='DATASET GENERATOR.')
parser.add_argument("data_folder", type=str, help="path to the folder containing image, audio and gaze data")


def main():
    data_folder = "./Presentation/08-22-2022 11_24_04"
    speech_to_text = SpeechToTextConvertor(data_folder)
    speech_to_text.create_dataset()
    speech_to_text.visualize_dataset()
    generate_coco_dataset_format(str(Path(data_folder) / "dataset.csv"),
                                 get_categories_json(Path(data_folder) / "categories.json"),
                                 data_folder)


if __name__ == "__main__":
    main()
