using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace DxlibGame
{
    class WaitTimer
    {
        [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        public static extern uint timeBeginPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        public static extern uint timeEndPeriod(uint uMilliseconds);

        [DllImport("winmm.dll", EntryPoint = "timeGetTime")]
        public static extern uint timeGetTime();


        long lest_time = 0; // オーバーしていた場合早く通過して平均化できるようにする
        long bef_time = 0;
        //Thread thread;
        //public delegate void ThreadEventFunction();
        //ThreadEventFunction call_function;

        static public void InitClass()
        {
            timeBeginPeriod(1);
        }

        static public void ReleaseClass()
        {
            timeEndPeriod(1);
        }

        public WaitTimer( )
        {

            bef_time = timeGetTime();
            //thread = new Thread(MainLoopHead);
            //call_function = _call_function;
        }

        int main_wait_time = 16;    // 基本となる待機時間（ミリ秒）
        int delay_msec = 3;         // Task.Delayの時間
        long cor_time = 0;          // 補正時間
        int cor_counter = 0;        // 補正用のカウント
        long last_wait_time = 0;     // 前回からの更新時間 情報保存用

        public bool IsNextMainLoop()
        {
            int[] cor_time_table = { 0, 1, 1 }; // 補正の進行テーブル

            // 補正値について
            // 16.666..msec
            // 1m = 60frame としたければ 16*60=960 40msec足りない
            // 60回中40回 3回中2回 +1msec する必要がある

            //while (true)
            //{
            //Task.Delay(delay_msec);

            long time = timeGetTime() - bef_time;

            if (((time + lest_time) < (main_wait_time + cor_time))) return false; // 端数でややおかしいので調整する
            // 補正値を進行させる
            cor_time = cor_time_table[cor_counter];
            cor_counter++;
            if (cor_counter >= cor_time_table.Length) cor_counter = 0;


            bef_time = bef_time + time;

            lest_time = time - main_wait_time;
            if (lest_time < 0) lest_time = 0;

            last_wait_time = time;
            //Console.WriteLine(time + " " + cor_time + " " + cor_counter);
            return true;
            //call_function(); // 設定した関数を呼び出す

            //}
        }

        public string GetInfoString()
        {
            return last_wait_time + " " + cor_time + " " + cor_counter;
        }

        public long GetLastWaitTime()
        {
            return last_wait_time;
        }
    }
}
