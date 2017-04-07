using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace TOL_SRPG.App
{
    public class PythonScript
    {
        ScriptEngine script_engine;
        ScriptScope script_scope;
        public dynamic script;

        //ScriptManager sm;
        //ScriptConector.BattleMapScriptConector bmsc;
        List<object> scope_object_list = new List<object>();

        public PythonScript(string script_path, System.Action<PythonScript> action = null )
        {
            //sm = new ScriptManager(script_path);
            //bmsc = new ScriptConector.BattleMapScriptConector(sm);

            script_engine = Python.CreateEngine();
            script_scope = script_engine.CreateScope();

            if (action!=null) action(this); // 事前にscript_scopeを設定したい場合などに利用する

            //// その場で式を実行。あまり面白くない例。
            //int result1024 = python.Execute<int>("2 ** 10");
            //Console.WriteLine(result1024);
            //Console.WriteLine();

            //foreach( var obj in scope_add_object)
            //{
            //    script_scope = obj;
            //}
            //script_scope.SetVariable("Map", bmsc);

            // ファイルを実行。helloメソッドを呼ぶ。まだ面白くない。
            script = script_engine.ExecuteFile(script_path, script_scope);
            //global.hello("world");
            //Console.WriteLine();


            //// Python側のクラスのインスタンスを作ってみる。
            //dynamic foo = global.Foo();
            //foo();     // __call__
            //foo.x = 2; // dynamicだからこんなことができる！
            //foo.y = 10;
            //int result20 = foo.method();
            //Console.WriteLine(result20);
            //Console.WriteLine();

            //// Python側のメタクラスをこっちで使ってみる
            //dynamic Bar = global.getMetaClass();
            //dynamic bar = Bar(); // Barクラスのインスタンスを作る！
            //Console.WriteLine(bar.expt(8));

            //var abc = new ABC();
            //scope.SetVariable("ABC", abc);
            //global.UseABC();
        }

        public void SetVariable( string name, object value )
        {
            scope_object_list.Add(value);
            script_scope.SetVariable( name, value);
        }

    }

    // ファイルパスの管理ができないと話しにならんか、相対パス系
    public class ScriptManager
    {
        Dictionary<string, string> path_replace = new Dictionary<string, string>();

        public ScriptManager( string script_path )
        {
            var index = script_path.LastIndexOf('/');
            var current_dir = script_path.Substring(0,index+1);

            path_replace.Add("$DATA$", @"data/");
            path_replace.Add("$CD$", current_dir);
        }


        public string GetPath(string path)
        {
            foreach( var r in path_replace)
            {
                path = path.Replace(r.Key, r.Value);
            }

            return path;
        }
    }

}
