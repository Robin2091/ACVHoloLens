import argparse
from data_utils.dataset_generator import DatasetGenerator


def get_args_parser():
    arg_parser = argparse.ArgumentParser(description="Generate object names from audio", add_help=False)
    arg_parser.add_argument('--data_folder', type=str, help="Path to the folder that will store data for each run")
    arg_parser.add_argument('--visualize', default=False, action='store_true', help="want to visualize images")
    return arg_parser


def main(parser_args):
    generator = DatasetGenerator(parser_args.data_folder)
    generator.load_data()
    generator.generate_dataset()
    if parser_args.visualize:
        generator.visualize_dataset()


if __name__ == "__main__":
    parser = argparse.ArgumentParser('HololensACV', parents=[get_args_parser()])
    args = parser.parse_args()
    main(args)
