import argparse
from data_utils.SpeechToText2 import SpeechToTextConvertor2


def get_args_parser():
    arg_parser = argparse.ArgumentParser(description="Generate object names from audio", add_help=False)
    arg_parser.add_argument('--data_folder', type=str, help="Path to the folder that will store data for each run")
    return arg_parser


def main(parser_args):
    convertor = SpeechToTextConvertor2(parser_args.data_folder)
    label_data = convertor.generate_labels()
    convertor.save_to_json(label_data)


if __name__ == "__main__":
    parser = argparse.ArgumentParser('HololensACV', parents=[get_args_parser()])
    args = parser.parse_args()
    main(args)
