using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using TOL_SRPG.Base;
using TOL_SRPG.App;

namespace TOL_SRPG.App.ScriptConector
{

    public class BattleMapScriptConector
    {
        ScriptManager script_manager;
        bool is_continue_player_unit = false;

        public BattleMapScriptConector(ScriptManager script_manager, bool is_continue_player_unit)
        {
            this.script_manager = script_manager;
            this.is_continue_player_unit = is_continue_player_unit;
        }

        public void Load(string map_path)
        {
            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;

            map_path = script_manager.GetPath(map_path);
            g3d_map.Load(map_path);
        }

        public void AddUnit(int map_x, int map_y, string unit_class_name, string name, string group, int color_no, int direction, string model_path = "", string image_face_path = "")
        {
            if (is_continue_player_unit && group == "味方") return; // 味方グループは引き継いで利用するため、生成しない

            var game_main = GameMain.GetInstance();

            if (model_path != "") model_path = script_manager.GetPath(model_path);
            if (image_face_path != "") model_path = script_manager.GetPath(image_face_path);

            var unit = new Unit(unit_class_name, model_path, image_face_path, name, map_x, map_y, color_no, direction);
            game_main.unit_manager.Join(unit, group);
        }
    }


    public class BattleMapEffectScriptConector : Action
    {
        public class Effect
        {
            ScriptManager script_manager;

            public Effect( ScriptManager script_manager )
            {
                this.script_manager = script_manager;
            }

            public void PlaySound(string sound_key, double volume)
            {
                SoundManager.PlaySound(sound_key, volume);
            }

            //public void Damage( Unit target_unit, int value )
            //{
            //    var game_main = GameMain.GetInstance();
            //    var g3d_map = game_main.g3d_map;
            //    var pos = g3d_map.GetScreenPositionByUnitTop(target_unit.map_x, target_unit.map_y);

            //    ActionManager.Add(new ActionDamage(15, pos.X, pos.Y, value, target_unit));
            //}
            public void ActionShoot( int start_map_x, int start_map_y, int start_height_offset, int target_map_x, int target_map_y, int target_height_offset)
            {
                var game_main = GameMain.GetInstance();
                var g3d_map = game_main.g3d_map;

                var height_offset = 1.0f;
                var route_as_shoot = new RouteAsShoot(start_map_x, start_map_y, height_offset, target_map_x, target_map_y, height_offset, 0);

                ActionManager.Add(new ActionShootArrow(0, route_as_shoot, true));
            }

            public void Action( string script_path, dynamic param )
            {
                var de = new DrawEffect(script_manager.GetPath(script_path), param);
                ActionManager.Add(de);
            }
        }

        public class Action
        {
            volatile int wait_timer = 0;
            volatile public bool is_work_wait = true; // 実行中なので、_Update時に待機させる

            public void Wait(int wait)
            {
                wait_timer = wait;
                is_work_wait = false;
                while (wait_timer > 0)
                {
                    WaitSleep.Do(0);
                }
                is_work_wait = true;
            }

            public void _Update()
            {
                if (wait_timer != 0)
                {
                    wait_timer--;
                    if (wait_timer <= 0) wait_timer = 0;
                }
                while (is_work_wait)
                {
                    WaitSleep.Do(0);
                }
            }
        }
        PythonScript python_script;
        Action action;
        Effect effect;
        Thread thread;
        bool is_hit = true;
        int effect_value = 0;
        Unit action_unit = null; // 行動ユニット
        Unit target_unit = null; // 対象ユニット

        public BattleMapEffectScriptConector(string script_path, bool is_hit, int effect_value, Unit action_unit, Unit target_unit)
        {
            var script_manager = new ScriptManager (script_path);
            effect = new Effect(script_manager);
            action = new Action();

            python_script = new PythonScript(script_path);
            python_script.SetVariable("effect", effect);
            python_script.SetVariable("action", action);

            this.is_hit = is_hit;
            this.effect_value = effect_value;
            this.action_unit = action_unit;
            this.target_unit = target_unit;
        }

        /// <summary>
        /// 新規スレッドを立てて、スクリプトを実行させる
        /// </summary>
        public void Run()
        {
            thread = new Thread(new ThreadStart(_Run));
            thread.Start();
        }

        public void _Run()
        {
            action.is_work_wait = true;
            python_script.script.Action(is_hit, action_unit, target_unit, effect_value);
            action.is_work_wait = false;
            this.is_end = true;
        }

        public override void Update()
        {
            action._Update();
        }

        public override void Draw()
        {
            python_script.script.ActionEffectDraw();
        }
    }

    public class BattleMapEffectScriptConectorManager : IDisposable
    {
        static BattleMapEffectScriptConectorManager battle_map_effect_script_conector_manager = null;
        List<BattleMapEffectScriptConector> scripts = new List<BattleMapEffectScriptConector>();
        object lock_action_foreach = new object();

        public BattleMapEffectScriptConectorManager()
        {
            battle_map_effect_script_conector_manager = this;
        }

        static public void Add(BattleMapEffectScriptConector a)
        {
            lock (battle_map_effect_script_conector_manager.lock_action_foreach)
            {
                a.Run();
                battle_map_effect_script_conector_manager.scripts.Add(a);
            }
        }

        public void Update()
        {
            lock (battle_map_effect_script_conector_manager.lock_action_foreach)
            {
                if (battle_map_effect_script_conector_manager.scripts.Count == 0) return;

                List<BattleMapEffectScriptConector> delete_scripts = new List<BattleMapEffectScriptConector>();
                foreach ( var s in battle_map_effect_script_conector_manager.scripts)
                {
                    s.Update();
                    if(s.IsEnd())
                    {
                        delete_scripts.Add(s);
                    }
                }

                foreach (var a in delete_scripts)
                {
                    a.Dispose();
                    scripts.Remove(a);
                }
            }
        }

        // 操作を停止させる必要のあるアクション中かどうか
        public bool IsControlFreese()
        {
            if (battle_map_effect_script_conector_manager.scripts.Count > 0) return true;
            return false;
        }

        // 開放処理
        public void Dispose()
        {
            lock (battle_map_effect_script_conector_manager.lock_action_foreach)
            {
                // スレッドが残ってしまうので
                // 処理がすべて終わるまで強制でUpdateさせる、（他にいい方法があったらいいんだが...）
                while (battle_map_effect_script_conector_manager.scripts.Count > 0)
                {
                    Update();
                }
            }
        }

    }


    // Taskを使ったスリープ
    // 他のスレッドに処理を意図して移したい場合に使う
    // 処理速度と時間の精度は当然落ちる（他の処理へ譲るので）
    public class WaitSleep
    {
        static public void Do(int time)
        {
            Task taskA = Task.Factory.StartNew(() => _Sleep_Task(time));
            taskA.Wait();
        }

        static private void _Sleep_Task(int time)
        {
            Thread.Sleep(time);
        }
    }

}
 