using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using DxlibGameSimRPG; // ここに、この名前空間つかいたくないけど、GameMainのが整理できてないので保留

namespace DxlibGame
{
    // メインループに処理を実行させるためのクラス
    // 1回だけ実行させたい処理を埋め込むためのもの
    public class STask
    {
        public delegate void Function( object param );
        Function function;
        volatile bool is_end = false;
        object param;

        public STask(Function function, object param)
        {
            this.function = function;
            this.param = param;
        }

        public void Run()
        {
            if (is_end) return;
            function(param);
            is_end = true;
        }

        public bool IsEnd()
        {
            return is_end;
        }

        public void WaitEnd()
        {
            while (!is_end && !STaskManager.IsNullManager())
            {
                Thread.Sleep(10); // 精度は気にしてもしょうがないかも
            }
        }

        public void Release()
        {
            is_end = true;
        }

    }

    public class STaskManager : IDisposable
    {
        static STaskManager stask_manager = null;
        public List<STask> stasks = new List<STask>();

        static object lock_foreach = new object();

        public STaskManager()
        {
            stask_manager = this;
        }

        public void Update()
        {
            lock (lock_foreach)
            {
                foreach (var t in stasks)
                {
                    t.Run();
                }
                stasks.Clear();
            }
        }

        public static void Add( STask stask )
        {
            if (stask_manager == null) return;
            lock (lock_foreach)
            {
                Console.WriteLine("\tAdd");
                stask_manager.stasks.Add(stask);
            }
        }

        public void Dispose()
        {
            lock (lock_foreach)
            {
                foreach (var t in stasks)
                {
                    t.Release();
                }
                stasks.Clear();
            }
            stask_manager = null;
        }

        public static bool IsNullManager()
        {
            if (stask_manager == null) return true;
            return false;
        }

    }
}
