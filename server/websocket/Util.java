import java.util.*;
import java.time.*;

public class Util
{
    public static long currentTime100Nanos() {
        long currentTimeMillis = new Date(System.currentTimeMillis()).getTime(); //milliseconds(10^-3)
        long nano100T = (System.nanoTime()/100L);
        return (currentTimeMillis * 10000L) + (nano100T % 10000) + 621355968000000000L;
    }

    public static void test() {
        int N = 1000000; //10^6
        List<Long> nanos = new ArrayList<Long>();
        List<Long> sortingNanos = new ArrayList<Long>();

        for(int i = 0; i < N; i++) {
            long x = Util.currentTime100Nanos();
            nanos.add(x);
            sortingNanos.add(x);
        }
        Collections.sort(sortingNanos);

        for(int i = 0; i < N; i++) {
            assert(nanos.get(i) == sortingNanos.get(i));
        }
    }

    public static void main(String[] args) {
        Util.test();
    }
}
