import sys, socket, select

def broadcast(socklist, server_socket, sock, message):
    print(message)
    for socket in socklist:
        if socket != server_socket and socket != sock :
            try :
                socket.send(message.encode())
            except :
                socket.close()
                socklist.remove(socket)

if __name__ == '__main__':
    port, socklist, server = 5001, [], '127.0.0.1' if len(sys.argv) <= 1 else sys.argv[1]
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((server, port))
    server_socket.listen(10)
    socklist.append(server_socket)
    print('Start')
    while True:
        read_sockets, write_sockets, error_sockets = select.select(socklist, [], [])
        for sock in read_sockets:
            if sock == server_socket:
                sockfd, addr = server_socket.accept()
                socklist.append(sockfd)
                broadcast(socklist, server_socket, sockfd, '[%s:%s] Enter' % addr)
            else:
                try:
                    data = sock.recv(4096).decode()
                    if data == '': raise Exception('Done')
                    if data:
                        broadcast(socklist, server_socket, sock, data)                
                except Exception as e:
                    print(e)
                    broadcast(socklist, server_socket, sock, '[%s, %s] Exit' % addr)
                    sock.close()
                    socklist.remove(sock)
