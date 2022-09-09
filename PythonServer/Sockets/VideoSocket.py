import time

import cv2
import numpy as np
from Sockets.ServerSocket import ServerSocket


class VideoSocket(ServerSocket):
    def __init__(self, ip_address, port, header_size, buffer_size, video_saver, data_displayer=None):
        ServerSocket.__init__(self, ip_address, port, buffer_size)
        # The number of bytes that will be used to represent the length of the video message
        self.header_size = header_size
        self.video_saver = video_saver
        self.data_displayer = data_displayer

    def start(self):
        # Start receiving the video frames and start the video saver
        client_socket = self.accept_connections()
        self.video_saver.start()
        #if self.data_displayer is not None:
        #    self.data_displayer.start()
        self.receive_video(client_socket)

    def receive_video(self, client_socket, show=False):
        date_time_data_length = 25  # Number of bytes used to represent the timestamp when the frame was captured
        i = 0
        t = time.time()
        while True:
            full_image_bytes = b''
            # Try receiving the timestamp and the frame byte length packets.
            # If client stops sending or closes the connection, then close the socket and exit the data receiving loop.
            try:
                time_stamp_packet = client_socket.recv(date_time_data_length)
                image_size_packet = client_socket.recv(self.header_size)
                image_bytes_length = int(image_size_packet.decode('utf-8'))
            except (ValueError, ConnectionResetError):
                print("error")
                self.video_saver.stop()
                break
            num_image_bytes_received = 0
            # Receive the exact number of bytes specified in the header of the message
            while True:
                # Receive only the number of image bytes remaining
                if num_image_bytes_received + self.buffer_size >= image_bytes_length:
                    try:
                        last_packet = client_socket.recv(image_bytes_length - num_image_bytes_received)
                        full_image_bytes += last_packet
                        num_image_bytes_received += len(last_packet)
                        if num_image_bytes_received >= image_bytes_length:
                            break
                    except (ValueError, ConnectionResetError):
                        print("error")
                        self.video_saver.stop()
                        break
                # Otherwise receive the number of bytes specified by the buffer size
                else:
                    try:
                        packet = client_socket.recv(self.buffer_size)
                        full_image_bytes += packet
                        num_image_bytes_received += len(packet)
                    except (ValueError, ConnectionResetError):
                        print("error")
                        break
            i += 1
            #full_image_bytes = pickle.loads(full_image_bytes)
            # Decode and send the timestamp and image to the video saver queue for saving
            time_stamp = time_stamp_packet.decode('utf-8')
            #full_image_bytes = pickle.loads(full_image_bytes)
            encoded_image = np.frombuffer(full_image_bytes, dtype=np.uint8)
            image = cv2.imdecode(encoded_image, cv2.IMREAD_UNCHANGED)
            if image is not None:
                self.video_saver.send_to_queue(image, time_stamp)
                if self.data_displayer is not None:
                    self.data_displayer.send_to_displayer([time_stamp, image], is_gaze_data=False)
                if show:
                    cv2.imshow("Received Video", cv2.resize(image, (1280, 720)))
                    cv2.waitKey(1)
        if show:
            cv2.destroyAllWindows()
        self.close_and_shutdown(client_socket)
