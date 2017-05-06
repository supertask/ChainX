using System;
using System.Threading;

class Program {
    static void Main(string[] args) {
        Bank bank = new Bank();
        AtmThread atmA = new AtmThread("A", bank);
        atmA.Start();
        AtmThread atmB = new AtmThread("B", bank);
        atmB.Start();
        AtmThread atmC = new AtmThread("C", bank);
        atmC.Start();
  
        Console.ReadLine();
    }
}

// 預金残高（balance）を保持するBankクラス
class Bank {
    private int balance = 1000;
    public int Balance { get { return balance; } set { balance = value; } }
}

// 預金の出し入れを行うスレッドクラス
// スレッドを使用している。
class AtmThread
{
    private string name;
    private Bank bank;

    public AtmThread(string name, Bank bank)
    {
        this.name = name;
        this.bank = bank;
    }

    public void Start()
    {
        Thread thread = new Thread(new ThreadStart(ThreadMethod));
        thread.Start();
    }

    private void ThreadMethod()
    {
        int balance = bank.Balance;
        Thread.Sleep(1000); // わざと競合を起こすため
        bank.Balance = balance + 200;
        Console.WriteLine("{0}: balance + 200 = {1}", name, balance + 200);
    }
}

