import os
import argparse
from pathlib import Path
from data_utils.DataTools import DataTools


def get_args_parser():
    arg_parser = argparse.ArgumentParser(description="Active Computer Vision HoloLens 2", add_help=False)
    arg_parser.add_argument('--data_folder', type=str, help="Path to the folder that will store data "
                                                                 "for each run")
    return arg_parser


def main(args):
    frames_with_gaze_data_folder = Path(args.data_folder) / "gaze_frames"
    os.makedirs(frames_with_gaze_data_folder)
    tools = DataTools(args.data_folder)
    tools.create_visualization("gaze_frames")


if __name__ == "__main__":
    parser = argparse.ArgumentParser('HololensACV', parents=[get_args_parser()])
    args = parser.parse_args()
    main(args)
