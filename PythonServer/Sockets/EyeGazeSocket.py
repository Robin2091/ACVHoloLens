from Sockets.ServerSocket import ServerSocket


class EyeGazeSocket(ServerSocket):
    def __init__(self, ip_address, port, buffer_size, header_size, gaze_data_saver, is_calibration=False):
        ServerSocket.__init__(self, ip_address, port, buffer_size)
        # The number of bytes that will be used to represent the length of the eye gaze data message
        self.header_size = header_size
        self.gaze_data_saver = gaze_data_saver
        self.is_calibration = is_calibration

    def start(self):
        client_socket = self.accept_connections()
        self.gaze_data_saver.start()
        self.receive_gaze_data(client_socket)

    def receive_gaze_data(self, client_socket):
        while True:
            # Try receiving the length of the message otherwise exit and stop receiving data if client stops sending
            try:
                data_length_packet = client_socket.recv(self.header_size)
                data_length_packet = data_length_packet.decode('utf-8')
                data_length = int(data_length_packet)
                # Receive and decode the timestamp and gaze data bytes
                timestamp_gaze_bytes = client_socket.recv(data_length)
                if timestamp_gaze_bytes == b'':
                    self.gaze_data_saver.stop()
                    break
            except (ValueError, ConnectionResetError):
                self.gaze_data_saver.stop()
                break
            time_stamp = timestamp_gaze_bytes.decode('utf-8')[0:25]
            if self.is_calibration:
                angle = timestamp_gaze_bytes.decode('utf-8')[25:29]
                gaze_data = timestamp_gaze_bytes.decode('utf-8')[29:]
            else:
                gaze_data = timestamp_gaze_bytes.decode('utf-8')[25:]
            # send the gaze and timestamp to the saver
            try:
                eye_coord = [int(gaze_data[0:4]), int(gaze_data[4:8])]
            except ValueError:
                eye_coord = None
            if eye_coord is not None:
                is_valid_eye_coord = 0 <= eye_coord[0] <= 2272 and 0 <= eye_coord[1] <= 1278
                if is_valid_eye_coord:
                    if self.is_calibration:
                        self.gaze_data_saver.send_to_queue_cal_mode(time_stamp, angle, eye_coord)
                    else:
                        self.gaze_data_saver.send_to_queue(time_stamp, eye_coord)
        self.close_and_shutdown(client_socket)

