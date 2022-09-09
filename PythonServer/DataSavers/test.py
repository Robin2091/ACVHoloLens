import soundfile as sf

file_name = f"audio_data_{0}.wav"
sf.write(file_name, [], 44100, 'PCM_16')