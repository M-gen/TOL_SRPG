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

        ScriptManager sm = new ScriptManager();
        BattleMapScriptConector bmsc;

        public PythonScript(string script_path )
        {
            bmsc = new BattleMapScriptConector(sm);

            script_engine = Python.CreateEngine();
            script_scope = script_engine.CreateScope();

            //// その場で式を実行。あまり面白くない例。
            //int result1024 = python.Execute<int>("2 ** 10");
            //Console.WriteLine(result1024);
            //Console.WriteLine();

            //foreach( var obj in scope_add_object)
            //{
            //    script_scope = obj;
            //}
            script_scope.SetVariable("Map", bmsc);

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
    }

    // ファイルパスの管理ができないと話しにならんか、相対パス系
    public class ScriptManager
    {
        string path_current_dir = @"data/script/";

        public string GetPath(string path)
        {
            path = path.Replace("%CD%", path_current_dir);

            return path;
        }
    }

    public class BattleMapScriptConector
    {
        ScriptManager script_manager;

        public BattleMapScriptConector(ScriptManager script_manager)
        {
            this.script_manager = script_manager;
        }

        public void Load( string map_path )
        {
            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;

            map_path = script_manager.GetPath(map_path);
            g3d_map.Load(map_path);
        }

        public void AddUnit( int map_x, int map_y, string unit_class_name, string model_path, string image_face_path, string name, string group, int color_no, int direction )
        {
            var game_main = GameMain.GetInstance();
            var unit = new Unit(unit_class_name, model_path, image_face_path, name, map_x, map_y, color_no, direction);
            game_main.unit_manager.Join(unit, group);
        }
    }
}
