using System;
using System.Collections;
using System.Collections.Generic;

public class Util {
    public static long currentTimeNanos() {
        return System.DateTime.Now.Ticks; //Nano second
    }

    public static long currentTimeMillis() {
        //return System.DateTime.Now.Millisecond;
        return System.DateTime.Now.Ticks / 10000; //Milli second
    }    

    public static long Max(long left, long right) {
        if (left > right) { return left; }
        else { return right; }
    }

    public static long Min(long left, long right) {
        if (left < right) { return left; }
        else { return right; }
    }

    public static void Assert(bool condition) {
        if (!condition) {
            throw (new Exception());
        }
    }

    /*
       public static dynamic ConvertType(dynamic anObject) {
       if (anObject.GetType() == typeof(int)) { return (int) anObject; }
       else if (anObject.GetType() == typeof(string)) { return (string) anObject; }
       else if (anObject.GetType() == typeof(bool)) { return (bool) anObject; }
       else if (anObject.GetType() == typeof(float)) { return (float) anObject; }
       return anObject;
       }
     */
}
