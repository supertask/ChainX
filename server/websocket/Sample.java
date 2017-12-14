import java.util.*;
import java.time.*;

public class Sample
{
    public static void main(String[] args) {

        for(int i=0; i < 100; i++) {
            System.out.println(Util.currentTime100Nanos());

            try {
                //Thread.sleep(10); //0.01秒
                Thread.sleep(100); //0.1秒
            }
            catch(InterruptedException e) { e.printStackTrace(); }
        }
    }

}
