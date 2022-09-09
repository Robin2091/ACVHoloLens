import socket

# sockets
HOST = socket.gethostbyname(socket.gethostname())
VIDEO_PORT = 12345
AUDIO_PORT = 12346
GAZE_PORT = 12347

# data handling
HEADER_SIZE = 10
BUFFER_SIZE = 32768

# audio
FORMAT = 8
CHANNELS = 1
FREQUENCY = 44100