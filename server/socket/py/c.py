import sys, socket, select, threading

def prompt(user) :
    sys.stdout.write('%s> ' % user)
    sys.stdout.flush()

if __name__ == "__main__":
    if len(sys.argv) < 3:
        print('Usage : python %s user host' % sys.argv[0])
        sys.exit()
    (user, host), port = sys.argv[1:3], 5001
    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    try :
        server_sock.connect((host, port))
    except :
        print('Unable to connect')
        sys.exit()
    print('Start')
    def listen():
        while True:
            read_sockets, write_sockets, error_sockets = select.select([server_sock], [], [])
            try:
                data = server_sock.recv(4096).decode()
            except:
                break
            sys.stdout.write('\r%s\n' % data)
            prompt(user)
        print('\rTerminated')
    t = threading.Thread(target=listen)
    t.start()
    prompt(user)
    while True:
        msg = sys.stdin.readline().strip()
        if not msg:
            server_sock.close()
            break
        try:
            server_sock.send(('%s| %s' % (user, msg)).encode())
        except:
            break
        prompt(user)
