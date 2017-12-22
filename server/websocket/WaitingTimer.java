import java.util.*;
import java.util.regex.*;


/*
 * Raftではリーダーに対してメッセージを送信し受信する（合意）までクライアントは次の操作をできない．
 * この待ち時間をこのWaitingTimerによって計測する
 */
public class WaitingTimer
{
    private Map<String, Long> startTime = new HashMap<String, Long>();
    private Map<String, List<Long>> endTimes = new HashMap<String, List<Long>>();
    private int waitingNumOfSites;

    public WaitingTimer() {
        this.waitingNumOfSites = 1;
    }

    public static String getOpStr(int sid, long ts) {
        return "sid" + sid + "ts" + ts;
    }

    //opStr = sid0,ts525252
    public synchronized String startWaitingTime(int sid, long ts, long startSystemTime)
    {
        String id = WaitingTimer.getOpStr(sid, ts);
        this.startTime.put(id, startSystemTime);
        return id;
    }

    public synchronized long endWaitingTime(int sid, long ts, long endSystemTime)
    {
        String id = WaitingTimer.getOpStr(sid, ts);
        if (! this.startTime.containsKey(id)) { return -1; }

        List<Long> eTimes = null;
        if (this.endTimes.containsKey(id)) {
            eTimes = this.endTimes.get(id);
            eTimes.add(endSystemTime);
        }
        else {
            eTimes = new ArrayList<Long>();
            eTimes.add(endSystemTime);
        }
        this.endTimes.put(id, eTimes);
        //System.out.println("size: " + eTimes.size() + ", siteNum:" + this.waitingNumOfSites);

        //最初に記録したタイムと全サイトの中で最後に記録した差分を求める
        if (eTimes.size() >= this.waitingNumOfSites) {
            long sTime = this.startTime.get(id);
            long waitingTime = Util.maxFrom(eTimes) - sTime; //long - long
            this.startTime.remove(id);
            this.endTimes.remove(id);
            return waitingTime; 
        }
        else {
            return -1;
        }
    }

    public void show()
    {
        for(Map.Entry<String, Long> entry : this.startTime.entrySet()) {
            String id = entry.getKey();
            long sTime = entry.getValue();
            System.out.println("id: " + id + ", sTime: " + sTime);
        }
        for(Map.Entry<String, List<Long>> entry : this.endTimes.entrySet()) {
            String id = entry.getKey();
            List<Long> eTimes = entry.getValue();
            for (long e: eTimes) {
                System.out.println("id: " + id + ", eTime: " + e);
            }
        }
    }

    public static void main(String[] args)
    {
        //
        //WaitingTimerをテストする
        //
        WaitingTimer t = new WaitingTimer();
        long waitingTime = 0L;
        t.startWaitingTime(0,5829181L, 23);
        waitingTime = t.endWaitingTime(0,5829181L, 200);
        assert(waitingTime == (200 - 23));

        t.startWaitingTime(1,52L, 6);
        waitingTime = t.endWaitingTime(1,52L, 600);
        assert(waitingTime == (600 - 6));

        //
        //正規表現テスト
        //
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
