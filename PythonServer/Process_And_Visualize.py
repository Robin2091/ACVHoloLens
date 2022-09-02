import os
from pathlib import Path
from DataTools import DataTools
#from DataToolsCalibration import DataTools

def main():
    folder = "./Presentation/09-01-2022 11_13_08"
    frames_with_gaze_data_folder = Path(folder) / "gaze_frames"
    os.makedirs(frames_with_gaze_data_folder)

    tools = DataTools(folder)
    # gaze_data = tools.load_gaze_data()
    # tools.find_best_angle([1087,481], gaze_data)
    tools.save_audio_to_wave(100)
    tools.generate_audio_file()
    gaze_data = tools.load_gaze_data()
    frame_timestamps = tools.load_frame_timestamps()
    tools.generate_images_with_gaze(frame_timestamps, gaze_data, "gaze_frames")
    tools.generate_concat_file(frame_timestamps, "gaze_frames")
    tools.generate_video_file("gaze_frames")
    tools.combine_video_and_audio(frame_timestamps, "gaze_frames", None)


if __name__ == "__main__":
    main()