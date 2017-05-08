using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class Example {
    public static Object thisLock = new Object(); 
    public ChainVoxel cv;

    public Example() {
        this.cv = new ChainVoxel();
    }

    public static void Main(string[] args) {
        Voxel.Test();
        StructureTable.Test();
        ChainVoxel.Test();

        Example ex = new Example();
        string posID = "";
        string destPosID = "1:1:1";
        Random r = new Random(1000);

        int receiveOperationNumber = 400;
        int receiveOperationSleepMillisecond = 12;
        int updateSelectObjectNumber = 500;
        int updateSelectObjectSleepMillisecond = 15;

        Thread thread = new Thread(
            () => {
                for(int i = 0; i < receiveOperationNumber; i++) {
                    /*
                     * Receiveした後の処理
                     */
                    lock(Example.thisLock) {
                        string[] xyz_str = destPosID.Split (':');
                        int[] xyz = new int[3];
                        for(int j=0; j<3; j++) { xyz[j] = int.Parse(xyz_str[j]); }

                        posID = String.Format("{0}:{1}:{2}", xyz[0], xyz[1], xyz[2]);
                        xyz[r.Next(3)]++;
                        destPosID = String.Format("{0}:{1}:{2}", xyz[0], xyz[1], xyz[2]);
                        Console.WriteLine("別スレッドだよ: posID=" + posID + ", destPosID=" + destPosID);
                        ex.cv.apply(new Operation(0, Operation.MOVE, posID, destPosID, "Group2"));
                    }
                    //Thread.Sleep(500); //ユーザのキー操作の速度で処理
                    //Thread.Sleep(25); //25のときバグが起こりやすい
                    Thread.Sleep(receiveOperationSleepMillisecond); //ユーザのキー操作の速度で処理
                }
            }
        );
        thread.Start();

        /*
         * Receiveにて更新されたmovedPosIDsを元にdestPosIDを更新
         */
        for (int i = 0; i < updateSelectObjectNumber; i++) {
            //send()、ここでの処理は省く
            //sendされた後、receiveで受け取られる。そこから記述している

            lock(Example.thisLock) {
                foreach (KeyValuePair<string,string> aPair in ChainVoxel.movedPosIDs) {
                    string posID_tmp = aPair.Key;
                    string destPosID_tmp = aPair.Value;
                    destPosID = destPosID_tmp;
                    //Console.WriteLine("本体スレッドだよ: destPosID="+ destPosID);
                }
                ChainVoxel.movedPosIDs.Clear();
            }
            Thread.Sleep(updateSelectObjectSleepMillisecond);
        }
    }
}
