
import java.net.URI;

import javax.websocket.ClientEndpoint;
import javax.websocket.ContainerProvider;
import javax.websocket.OnClose;
import javax.websocket.OnError;
import javax.websocket.OnMessage;
import javax.websocket.OnOpen;
import javax.websocket.Session;
import javax.websocket.WebSocketContainer;
import java.nio.ByteBuffer;
import java.nio.charset.Charset;

/**
 * Websocket Endpoint implementation class VSite
 */

@ClientEndpoint
public class VSite {

    public VSite() {
        super();
    }

    @OnOpen
    public void onOpen(Session session) {
        /* セッション確立時の処理 */
        System.err.println("[セッション確立]");
    }

    @OnMessage
    public void onMessage(String message) {
        /* メッセージ受信時の処理 */
        System.err.println("[受信]:" + message);
    }

    @OnError
    public void onError(Throwable th) {
        /* エラー発生時の処理 */
    }

    @OnClose
    public void onClose(Session session) {
        /* セッション解放時の処理 */
    }

    static public void main(String[] args) throws Exception {

        // 初期化のため WebSocket コンテナのオブジェクトを取得する
        WebSocketContainer container = ContainerProvider
                .getWebSocketContainer();
        // サーバー・エンドポイントの URI
        URI uri = URI.create("ws://localhost:18080");
        // サーバー・エンドポイントとのセッションを確立する
        Session session = container.connectToServer(new VSite(),
                uri);

        String msg = "OPERATION@{'sid': '0','ts':'525225252'}";
        //session.getBasicRemote().sendText(msg);
        session.getBasicRemote().sendBinary(
            ByteBuffer.wrap(msg.getBytes(Charset.forName("UTF-8") ))
        );

        /*
        while (session.isOpen()) {
            Thread.sleep(100 * 1000);
            System.err.println("open");
        }
        */
    }

}
