import java.util.*;


public class MessageQueue extends HashMap<String, List<String>>
{
    public void enqueue(String msgType, String msg)
    {
        if (this.containsKey(msgType)) {
            List<String> msges = this.get(msgType);
            msges.add(msg);
            this.put(msgType, msges);
        }
        else {
            List<String> msges = new ArrayList<String>();
            msges.add(msg);
            this.put(msgType, msges);
        }
    }

    public String dequeue(String msgType)
    {
        if (this.containsKey(msgType)) {
            return this.get(msgType).remove(0); //dequeue!
        }
        else { return ""; } 
    }


    public int queueSize(String msg) {
        if (this.containsKey(msg)) { return this.get(msg).size(); }
        else { return 0; }
    }

    public List<String> getMessages(String msg) {
        return this.get(msg);
    }

    public void clearQueue(String msg) {
        this.get(msg).clear();
    }
}
