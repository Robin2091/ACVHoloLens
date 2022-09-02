import socket


class ServerSocket:
    def __init__(self, ip_address, port, buffer_size):
        # Create a new TCP server socket and initialize it to listen for only one client
        #socket.setdefaulttimeout(10.0)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.bind((ip_address, port))
        self.socket.listen(1)
        self.buffer_size = buffer_size
        print(f"Socket started on host: {ip_address}")

    def accept_connections(self, logging=True):
        # Accept any incoming connection from a client
        client_socket, client_address = self.socket.accept()
        if logging:
            print(f"Client connected from {client_address}")
        return client_socket

    def close_and_shutdown(self, client_socket):
        client_socket.shutdown(socket.SHUT_RDWR)
        #self.socket.shutdown(socket.SHUT_RDWR)
        client_socket.close()
        self.socket.close()
