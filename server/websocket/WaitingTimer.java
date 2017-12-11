import java.util.*;
import java.util.regex.*;


public class WaitingTimer
{
    HashMap<String, Long> startTime = HashMap<String, Long>();
    HashMap<String, Long> endTimes = HashMap<String,<List<Long>> >();

    public static String getOpStr(int sid, long ts) {
        return "sid" + sid + "ts" + ts;
    }

    //opStr = sid0,ts525252
    public synchronized String startWaitingTime(int sid, long ts, long startSystemTime)
    {
        String key = WaitingTimer.getOpStr(sid, ts);
        this.put(key, startSystemTime);
        return key;
    }

    public synchronized long endWaitingTime(int sid, long ts, long endSystemTime)
    {
        String opStr = WaitingTimer.getOpStr(sid, ts);
        if (! this.containsKey(opStr)) { return -1; }
        long startSystemTime = this.get(opStr);
        this.remove(opStr);
        return endSystemTime - startSystemTime;
    }

    public void show() {
        for(Map.Entry<String, Long> entry : this.entrySet()) {
            String opKey = entry.getKey();
            System.out.println(opKey);
        }
    }

    public static void main(String[] args) {
        WaitingTimer t = new WaitingTimer();
        t.startWaitingTime(0,5829181L, 23);
        t.startWaitingTime(1,529181L, 23);
        System.out.println(t.endWaitingTime(0,5829181L, 200));
        System.out.println(t.endWaitingTime(1,529181L, 25));


        Matcher m = null;
        String s = "";
        String in = "\"sid\":\"52\",\"ts\":\"88624242\"";
        Pattern p1 = Pattern.compile("\"sid\":\"(\\d+)\"");
        Pattern p2 = Pattern.compile("\"ts\":\"(\\d+)\"");
        m = p1.matcher(in);
        if (m.find()) {
            s = m.group(1);
            System.out.println(s + "の部分にマッチしました");
        }
        m = p2.matcher(in);
        if (m.find()) {
            s = m.group(1);
            System.out.println(s + "の部分にマッチしました");
        }
        in = in.replaceFirst("\"sid\":\"(\\d+)\"", "\"sid\":" + "\"" + 10000 + "\"");
        in = in.replaceFirst("\"ts\":\"(\\d+)\"", "\"ts\":" + "\"" + 1000000 + "\"");
        System.out.println(in);
    }

    /*
    public void updateAllWaitingTime(long diffWaitingTime)
    {
        for(Map.Entry<String, Integer> entry : this.entrySet()) {
            String opStr = entry.getKey();
            this.put(opStr, this.get(opStr) + diffWaitingTime);
        }
    }
    */
    
}
