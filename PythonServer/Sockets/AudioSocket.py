import socket
import struct
from Sockets.ServerSocket import ServerSocket


class AudioSocket(ServerSocket):
    def __init__(self, ip_address, port, buffer_size, audio_format, channels, frequency, audio_saver):
        ServerSocket.__init__(self, ip_address, port, buffer_size)
        # Initialize the data needed to format and save the received audio data
        self.audio_format = audio_format
        self.channels = channels
        self.frequency = frequency
        self.audio_saver = audio_saver

    def start(self):
        client_socket = self.accept_connections()
        # Once the client stops sending, save all of the audio data
        self.audio_saver.start(self.channels, self.audio_format, self.frequency)
        # Receive the audio data
        self.receive_audio(client_socket)
        #self.audio_saver.save_audio(self.channels, self.audio_format, self.frequency)

    def receive_audio(self, client_socket):
        # Receive timestamp that represents when the audio recording started
        timestamp_data_length = 25
        try:
            timestamp_packet = client_socket.recv(timestamp_data_length)
        except (ValueError, ConnectionResetError, socket.timeout):
            print("BREAK 3")
            self.audio_saver.stop()
            return
        time_stamp = timestamp_packet.decode("utf-8")
        self.audio_saver.save_timestamp(time_stamp)
        # Listen for the audio packets
        while True:
            try:
                packet = client_socket.recv(self.buffer_size)
            except (ValueError, ConnectionResetError, socket.timeout):
                print("BREAK 4")
                self.audio_saver.stop()
                break
            if packet == b'':
                print("BREAK 5")
                self.audio_saver.stop()
                break
            # Reformat the audio data so the floating point values can be converted to ints
            for i in range(0, len(packet), 4):
                try:
                    self.audio_saver.send_to_queue(struct.unpack('f', packet[i:i + 4])[0])
                except struct.error:
                    print(packet[i:i+4])
        print("AUDIO LOOP STOPPED")
        self.close_and_shutdown(client_socket)

