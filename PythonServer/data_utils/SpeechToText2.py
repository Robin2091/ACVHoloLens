import ast
import wave
import datetime
import json
from pathlib import Path
import soundfile as sf
from vosk import Model, KaldiRecognizer


class SpeechToTextConvertor2:
    def __init__(self, data_folder):
        self.data_folder = Path(data_folder)
        self.audio_folder = Path(self.data_folder) / "audio_data"

    def generate_labels(self):
        audio_start_timestamps = self.get_audio_start_timestamps()
        audio_timestamps = self.get_audio_end_timestamps(audio_start_timestamps)
        data = []
        for name in audio_timestamps.keys():
            data_dict = {}
            speech = self.generate_text_from_speech(name)
            filtered_speech = self.filter_text(speech)
            data_dict['name'] = name
            data_dict['timestamps'] = [timestamp.strftime("%m-%d-%Y %H:%M:%S.%f")
                                       for timestamp in audio_timestamps[name]]
            data_dict['speech'] = filtered_speech
            data.append(data_dict)
        return data

    def filter_text(self, speech):
        filtered_speech = []
        for results in speech:
            result_dict = ast.literal_eval(results)
            if result_dict['text'] != '':
                filtered_speech.append(result_dict)
        return filtered_speech

    def generate_text_from_speech(self, audio_file_name):
        speech = []
        wf = wave.open(str(self.audio_folder / audio_file_name), "rb")
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
                speech.append(rec.Result())
        return speech

    def get_audio_start_timestamps(self):
        timestamps_file = open(self.audio_folder / "audio_start_timestamps.txt", "r")
        time_stamps = [datetime.datetime.strptime(time_stamp.rstrip().lstrip(), "%m-%d-%Y %H:%M:%S.%f") for
                       time_stamp
                       in timestamps_file.readlines()]
        file_names_file = open(self.audio_folder / "audio_file_names.txt", "r")
        names = [name.rstrip().lstrip() for name in file_names_file.readlines()]
        audio_start_timestamps = {}
        for name, timestamp in (zip(names, time_stamps)):
            audio_start_timestamps[name] = timestamp
        return audio_start_timestamps

    def get_audio_end_timestamps(self, audio_start_timestamps):
        audio_timestamps = {}
        for name in audio_start_timestamps.keys():
            audio_start_timestamp = audio_start_timestamps[name]
            f = sf.SoundFile(self.audio_folder / name)
            num_seconds = f.frames / f.samplerate
            audio_end_timestamp = audio_start_timestamp + datetime.timedelta(seconds=num_seconds)
            timestamps = [audio_start_timestamp, audio_end_timestamp]
            audio_timestamps[name] = timestamps
        return audio_timestamps

    def save_to_json(self, data):
        data_json = json.dumps(data, indent=4)
        with open(self.data_folder / "speech.json", "w") as file:
            file.write(data_json)


