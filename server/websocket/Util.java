import java.util.*;
import java.time.*;

public class Util
{
    public static long maxFrom(List<Long> values) {
        long maxV = 0L;
        for(long v : values) {
            maxV = Math.max(v, maxV);
        }
        return maxV;
    }

    public static long sumFrom(List<Long> values) {
        long sumV = 0L;
        for(long v : values) {
            sumV += v;
        }
        return sumV;
    }

    public static long currentTime100Nanos() {
        long currentTimeMillis = new Date(System.currentTimeMillis()).getTime(); //milliseconds(10^-3)
        long nano100T = (System.nanoTime()/100L);
        return (currentTimeMillis * 10000L) + (nano100T % 10000) + 621355968000000000L;
    }

    public static void test() {
        int N = 100; //10^6
        List<Long> nanos = new ArrayList<Long>();
        List<Long> sortingNanos = new ArrayList<Long>();

        for(int i = 0; i < N; i++) {
            long x = Util.currentTime100Nanos();
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

        Random r = new Random();
        List<Long> ans_values = new ArrayList<Long>();
        List<Long> values = new ArrayList<Long>();
        int n = r.nextInt(100);
        for(int i = 0; i < n; i++) {
            long l = r.nextLong();
            ans_values.add(l);
            values.add(l);
        }
        Collections.sort(ans_values);
        assert(ans_values.get(ans_values.size()-1) == Util.maxFrom(values));

        System.out.println("TEST COMPLETED!!");
    }

    public static void main(String[] args) {
        Util.test();
    }
}
