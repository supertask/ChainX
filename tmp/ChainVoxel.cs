using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

/**
 * ChainVoxelを実装したクラス．<br>
 * <br>
 * negativeVoxelのvoxelチェインへの追加処理を簡略化するために，<br>
 * negativeVoxelをposIDに対応するvoxelチェインとは独立して管理する実装になっています．<br>
 * voxelチェインはこのクラスではvoxelのリストとして実装されています．<br>
 * <br>
 * K. Imae and N. Hayashibara, 
 * “ChainVoxel: A Data Structure for Scalable Distributed Collaborative Editing for 3D Models” 
 * The 14th IEEE International Conference on Dependable, Autonomic and Secure Computing, 8-12 Aug. 2016.
 *
 * @author kengo92i
 */
public class ChainVoxel {
    /**
     * posIDに対応するvoxelのリストを管理するSortedDictionary
     */
    private SortedDictionary<string, List<Voxel>> atoms;

    /**
     * posIDに対応する負のvoxelを管理するSortedDictionary
     */
    private SortedDictionary<string, Voxel> negativeVoxels;

    /**
     * 構造管理のためのStrutureTable
     */
    private StructureTable stt;

    private ChainXController controller;

    public List<string> insertedPosIDs;
    public List<string> deletedPosIDs;
    public static Dictionary<string,string> movedPosIDs;


    /**
     * ChainVoxelのコンストラクタ
     */
    public ChainVoxel(ChainXController controller) {
        this.atoms = new SortedDictionary<string, List<Voxel>>();
        this.negativeVoxels = new SortedDictionary<string, Voxel>();
        this.stt = new StructureTable();
        this.controller = controller;
        this.insertedPosIDs = new List<string> ();
        this.deletedPosIDs = new List<string> ();
        ChainVoxel.movedPosIDs = new Dictionary<string,string>();
    }

    /**
     * 操作オブジェクトに対応する操作を実行するメソッド．<br>
     * ChainVoxelに対する操作はapplyメソッドを用いて実行することを推奨しています．
     * @param op 操作オブジェクト
     * @see Operation
     */
    public void apply(Operation op) {
        string posID = op.getPosID();

        switch (op.getOpType()) {
        case Operation.INSERT:
            if (this.stt.isGrouped(posID)) break;
            this.insert(op);
            break;
        case Operation.DELETE:
            if (this.stt.isGrouped(posID)) break;
            this.delete(op);
            break;
        case Operation.CREATE:
            this.create(op);
            break;
        case Operation.JOIN:
            this.join(op);
            break;
        case Operation.LEAVE:
            this.leave(op);
            break;
        case Operation.MOVE:
            this.move(op);
            break;
        default:
            Debug.Assert (false);
            break;
        }
        ChainXController.log = this.show();
        return;
    }

    /**
     * ChainVoxel内にvoxelを挿入するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
    public void insert(Operation op) {
        int id = op.getSID();
        string posID = (op.getOpType() == Operation.MOVE) ? op.getDestPosID() : op.getPosID();
        //Debug.Log (posID + " at insert()");
        long timestamp = op.getTimestamp();
        Voxel insertVoxel = new Voxel(id, timestamp);
        //Debug.Log (GameObject.Find (posID));

        List<Voxel> voxelList = this.getVoxelList(posID);

        // step1: 負のvoxelの影響があるか調べる
        // 負のvoxelより新しいtsの場合は以降の処理に進む，そうではない場合は，ここで終了
        if (this.negativeVoxels.ContainsKey(posID)) {
            //Debug.Log (this.negativeVoxels[posID]);

            if (this.negativeVoxels[posID].getTimestamp() >= timestamp) {
                return; // 負のvoxelより前に挿入する操作は無駄な操作であるため
            }
        }

        // step2: insertVoxelを挿入する
        voxelList.Add(insertVoxel);
        voxelList.Sort(Voxel.Compare);
        if (op.getOpType () == Operation.INSERT) { this.insertedPosIDs.Add (posID); }
        return;
    }

    /**
     * ChainVoxel内の指定したvoxelを削除するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
    public void delete(Operation op) {
        //int id = op.getSID();
        string posID = op.getPosID();
        long timestamp = op.getTimestamp();

        // step1: 負のvoxelをnegativeVoxelsに追加・更新
        if (!this.negativeVoxels.ContainsKey(posID) || this.negativeVoxels[posID].getTimestamp() < timestamp) {
            this.negativeVoxels[posID] = new Voxel(timestamp);
        }

        List<Voxel> voxelList = this.getVoxelList(posID);

        // step2: 負のvoxelより古いvoxelを削除する
        for (int i = voxelList.Count - 1; i >= 0; --i) { // 先頭から削除するとイテレータがおかしくなる
            if (this.negativeVoxels[posID].getTimestamp() >= voxelList[i].getTimestamp()) {
                voxelList.RemoveAt(i); 
            }
        }

        voxelList.Sort(Voxel.Compare);
        if (op.getOpType () == Operation.DELETE) { this.deletedPosIDs.Add (posID); }

        return;
    }

    public void move(Operation op) {
        //op.getPosID() op.getDestPosID()をひも付けておいて、selectedObjectの遷移をposID(delete)からDestPosID先(insert)へ変更
        this.delete (op);
        this.insert (op);
        lock (ChainVoxel.movedPosIDs) {
            ChainVoxel.movedPosIDs [op.getPosID ()] = op.getDestPosID ();
        }
    }

    /**
     * 指定したグループを作成するメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
    public void create(Operation op) {
        this.stt.create(op.getGID());
    }

    /**
     * 指定したグループにvoxelを参加させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
    public void join(Operation op) {
        this.stt.join(op.getTimestamp(), op.getPosID(), op.getGID());
    }

    /**
     * 指定したグループからvoxelを脱退させるメソッド
     * @param op 操作オブジェクト
     * @see Operation
     */
    public void leave(Operation op) {
        this.stt.leave(op.getSID(), op.getTimestamp(), op.getPosID(), op.getGID());
        this.insert(op);
    }

    /**
     * 指定したposIDに対応するprimaryVoxelを返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxel，posIDに対応するものがない場合はnullを返す．
     * @see Voxel
     */
    public Voxel getVoxel(string posID) {
        List<Voxel> voxelList = this.atoms[posID];
        if (voxelList == null) {
            return null;
        }
        return voxelList[0]; // 先頭のvoxelがprimaryVoxel
    }

    /**
     * 指定したposIDに対応するvoxelのリストを返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxelのリスト
     * @see Voxel
     */
    public List<Voxel> getVoxelList(string posID){
        if (!this.atoms.ContainsKey (posID)) {
            this.atoms [posID] = new List<Voxel> ();
        }
        return this.atoms [posID];
    }

    /**
     * ChainVoxelの総容量を返すメソッド * @return ChainVoxelの総容量
     */
    public int size() {
        int totalSize = 0;
        foreach (KeyValuePair<string, List<Voxel>> p in this.atoms) {
            totalSize += p.Value.Count; 
        }
        return totalSize;
    }

    /**
     * 指定されたposIDのvoxel数を返すメソッド
     * @param posID voxelの識別子
     * @return posIDに対応するvoxel数
     */
    public int size(string posID) {
        return this.atoms[posID].Count;
    }


    /**
     * ChainVoxelの状態を表示する
     */
    public string show() {
        string res="ChainVoxel table\n";
        foreach (KeyValuePair<string, List<Voxel>> p in this.atoms)
        {
            if (p.Value.Count == 0) continue;
            res += "|" + p.Key + "|\n";

            List<Voxel> voxelList = p.Value;
            int n = voxelList.Count;
            foreach (Voxel voxel in voxelList) {
                string id = voxel.getId().ToString();
                string timestamp = voxel.getTimestamp().ToString();
                res += " -> (" + id + "," + timestamp + ")\n";
            }
            res += "\n";
        }
        res += "\n";
        //Debug.Log(res);

        return res;
    }

    /**
     * Test a ChainVoxel class.
     */
    public static void Main() {
        ChainVoxel cv = new ChainVoxel();        
        //SortedDictionary<string, Voxel> s = new SortedDictionary<string, Voxel>();
        //Debug.Log(s);
    }
}

