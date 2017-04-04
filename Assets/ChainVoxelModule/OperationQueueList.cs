using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OperationQueueList {
	/**
     * Siteの総数
     */
	private int numberOfSites;

	/**
     * 操作オブジェクトを管理するQueueのリスト    
     */
	List<Queue<Operation>> opq;

	/**
     * OperationQueueListのコンストラクタ
     */    
	public OperationQueueList() {
		this.numberOfSites = 0;
		this.opq = new List<Queue<Operation>>();
	}

	/**
     * OperationQueueListのコンストラクタ
     * @param n 使用するQueueの総数
     */    
	public OperationQueueList(int n) {
		this.numberOfSites = n;    
		this.opq = new List<Queue<Operation>>();
		for (int i = 0; i < this.numberOfSites; ++i) { this.opq.Add(new Queue<Operation>()); }
	}

	public void joinNewSite() {
		this.numberOfSites++;
		this.opq.Add(new Queue<Operation>());
	}

	/**
     * 現在シュミレータ上に存在しているSiteの総数を返す.
     * @return Siteの総数
     */
	public int getNumberOfSites(){ return this.numberOfSites; }

	/**
     * 識別子に対応するQueueに操作オブジェクトをenqueueする．
     * @param dest Queueの識別子
     * @param op 操作オブジェクト
     */
	public void enqueue(int dest, Operation op) {
		lock(this.opq) {
			this.opq[dest].Enqueue(op);
		}
	}

	/**
     * 識別子に対応するQueueから操作オブジェクトをdequeueする．<br>
     * Queueの要素が空の場合はnullを返す
     * @param id Queueの識別子
     * @return 操作オブジェクト
     */
	public Operation dequeue(int id) {
		lock(this.opq) {
			return this.opq[id].Dequeue();
		}
	}

	/**
     * 識別子に対応するQueueを空にする．
     * @param id Queueの識別子
     */    
	public void clear(int id) {
		lock(this.opq) {
			this.opq[id].Clear();
		}
	}

	/**
     * 識別子に対応するQueueの容量を返す
     * @param id Queueの識別子
     * @return Queueの容量
     */
	public int size(int id) { return this.opq[id].Count; }

	/**
     * 識別子に対応するQueueが空であるか確認するメソッド．
     * @param id Queueの識別子
     * @return 空の場合にtrueを返す．それ以外はfalse.
     */
	public bool isEmpty(int id) { return this.size(id) == 0; }
}

