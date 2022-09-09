import struct
from Sockets.ServerSocket import ServerSocket


class AudioSocket(ServerSocket):
    def __init__(self, ip_address, port, buffer_size, header_size, audio_format, channels, frequency, audio_saver):
        ServerSocket.__init__(self, ip_address, port, buffer_size)
        # Initialize the data needed to format and save the received audio data
        self.audio_format = audio_format
        self.channels = channels
        self.frequency = frequency
        self.audio_saver = audio_saver
        self.header_size = header_size

    def start(self):
        client_socket = self.accept_connections()
        # Once the client stops sending, save all of the audio data
        self.audio_saver.start(self.channels, self.audio_format, self.frequency)
        # Receive the audio data
        self.receive_audio(client_socket)

    def receive_audio(self, client_socket):
        receive_data = True
        while receive_data:
            # Receive timestamp that represents when the audio recording started
            try:
                data_length = int(client_socket.recv(self.header_size))
                data_packet = client_socket.recv(data_length)
            except (ValueError, ConnectionResetError):
                receive_data = False
                self.audio_saver.stop()
                break
            data = data_packet.decode("utf-8")
            if data == "00000":
                break
            time_stamp = data
            self.audio_saver.save_timestamp(time_stamp)
            # Listen for the audio packets
            while True:
                try:
                    data_packet = client_socket.recv(self.buffer_size)
                except (ValueError, ConnectionResetError) as e:
                    self.audio_saver.stop()
                    receive_data = False
                    break
                if data_packet == b'00000':
                    break
                if data_packet == b'':
                    self.audio_saver.stop()
                    receive_data = False
                    break
                # Reformat the audio data so the floating point values can be converted to ints
                for i in range(0, len(data_packet), 4):
                    try:
                        self.audio_saver.send_to_queue(struct.unpack('f', data_packet[i:i + 4])[0])
                    except struct.error:
                        print(data_packet[i:i+4])
        self.close_and_shutdown(client_socket)

