import java.util.*;
public class Ticks
{
    public long startTicksMillis;
    public long startNano100;

    public Ticks() {
    }

    public void start() {
        long currentTimeMillis = new Date(System.currentTimeMillis()).getTime();
        this.startTicksMillis = (currentTimeMillis * 10000L) + 621355968000000000L;
        this.startNano100 = System.nanoTime() / 100L;
    }

    public long end() {
        long endNano100 = System.nanoTime() / 100L;
        return this.startTicksMillis + (endNano100 - this.startNano100);
    }

    public static void main(String[] args)
    {
        Ticks ticks = new Ticks();
        ticks.start();
        /*
        for(int i = 0; i < 1000; i++) {
            long t = ticks.end();
            System.out.println(t);
            try { Thread.sleep(10); }
            catch (Exception e) { e.printStackTrace(); }
        }
        */

        int N = 100000; //10^6
        List<Long> nanos = new ArrayList<Long>();
        List<Long> sortingNanos = new ArrayList<Long>();

        for(int i = 0; i < N; i++) {
            long x = ticks.end();
            nanos.add(x);
            sortingNanos.add(x);
        }
        Collections.sort(sortingNanos);
        for(int i = 0; i < N; i++) {
            long x = nanos.get(i);
            long y = sortingNanos.get(i);
            if (x != y) {
                System.out.println("Actual Nano: " + x + ", Sorted Nano:" + y);
            }
            assert(x == y);
        }

        System.out.println("TEST COMPLETED!!");
        
    }
}
