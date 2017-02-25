using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using TOL_SRPG.Base;

namespace TOL_SRPG.App
{
    public class BattleAI
    {

        class Action
        {
            public string command;
            public Unit target;      // これだとターゲットが1体しか対応できないけど
            public int target_map_x;
            public int target_map_y;
            //public double evaluation;  // 評価
        }

        class ActionDatas
        {
            public List<Action> actions = new List<Action>();
        }

        class MovePoint
        { 
            public int map_x;       // 移動先
            public int map_y;
            public double evaluation;  // 評価
        }

        class RangeStatus // 距離のステータス
        {
            public int map_x;           // 場所
            public int map_y;
            public double evaluation_avg;  // 平均距離
            public double evaluation_min;  // 最短距離
            public int evaluation_num;  // 評価項目数
        }

        Thread thread;
        volatile bool is_thread_end = false;

        Unit select_unit;
        SceneBattle scene_battle;
        string select_unit_group;

        // 範囲系クラスをこちらで所有(g3d_mapで混用すると不都合がありそうなので)
        RangeAreaMove move_area;
        RangeAreaActionTarget action_target_area;

        public BattleAI(SceneBattle scene_battle, Unit select_unit)
        {
            this.scene_battle = scene_battle;
            this.select_unit = select_unit;

            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;
            select_unit_group = game_main.unit_manager.GetUnitGroup(select_unit.map_x, select_unit.map_y);

            // 範囲系クラス
            move_area = new RangeAreaMove(g3d_map.map_w, g3d_map.map_h);
            action_target_area = new RangeAreaActionTarget(g3d_map.map_w, g3d_map.map_h);

            thread = new Thread(new ThreadStart(Run)); // 可能ならスレッド分けて、画面が停滞しないようにはしたい
            thread.Start();
        }

        public void Run()
        {
            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;
            //var user_interface = game_main.user_interface;
            //var game_base = game_main.game_base;

            // まず移動させよう
            // 移動範囲を表示、算出して
            //g3d_map.SetMoveAreaEffect(select_unit.map_x, select_unit.map_y, select_unit.bt.movement_force, select_unit.bt.jumping_force, "");
            move_area.Check(select_unit.map_x, select_unit.map_y, select_unit.bt.status["MOVE"].now, select_unit.bt.status["JUMP"].now);

            // 移動できる場所をリストアップ
            var move_point_list = new List<MovePoint>(); 
            for( var mx=0; mx< move_area.map_w; mx++ )
            {
                for (var my = 0; my < move_area.map_h; my++)
                {
                    if (move_area.IsInside(mx, my) )
                    {
                        var mp = new MovePoint();
                        mp.map_x = mx;
                        mp.map_y = my;
                        mp.evaluation = 0; //とりあえず初期値
                        move_point_list.Add(mp);
                    }
                }
            }
            { // 移動しない場合
                var mp = new MovePoint();
                mp.map_x = select_unit.map_x;
                mp.map_y = select_unit.map_y;
                move_point_list.Add(mp);
            }

            // 移動できる場所を判定
            MovePoint max_mp = null;
            var max_evalution = 0.0;
            foreach ( var mp in move_point_list)
            {
                var evalution = 0.0;
                CalcMoveEvalution(mp);
                evalution = mp.evaluation * 3;
                var rs = CalcRangeStatus(mp.map_x, mp.map_y, false);
                evalution += (-rs.evaluation_min) / 5 + (-rs.evaluation_avg) / 10;

                //Console.WriteLine("{0},{1} ({2}) {3}  {4}", mp.map_x,mp.map_y, mp.evaluation, rs.evaluation_min, rs.evaluation_avg);

                if (max_mp==null || max_evalution< evalution)
                {
                    max_evalution = evalution;
                    max_mp = mp;
                }
            }
            Thread.Sleep(100);

            // 移動を実行
            if (max_mp.map_x != select_unit.map_x || max_mp.map_y != select_unit.map_y)
            {
                var t = new STask( (param) =>
                {
                    select_unit.Move(max_mp.map_x, max_mp.map_y, true);
                    GameMain.GetInstance().g3d_map.ClearRangeAreaEffect();
                }, null);
                STaskManager.Add(t);
                t.WaitEnd();

                //var t = new STask(_Run_DoMove,new SIPoint(max_mp.map_x, max_mp.map_y));
                //STaskManager.Add(t);
                //t.WaitEnd();
                ////select_unit.Move(max_mp.map_x, max_mp.map_y, true);
                ////g3d_map.ClearRangeAreaEffect();
            }

            Thread.Sleep(100);
            // 行動を決める
            {
                var action_datas = new ActionDatas();
                int num, effective_point_avg, effective_point_max;
                if (IsExistActionTarget(max_mp.map_x, max_mp.map_y, out num, out effective_point_avg, out effective_point_max, action_datas))
                {

                    Console.WriteLine("----A");
                    var t = new STask((param) =>
                    {
                        var a = action_datas.actions[0];
                        var ad = ActionDataManager.GetActionData(a.command);

                        var target_unit = game_main.unit_manager.GetUnit(a.target_map_x, a.target_map_y);
                        if (target_unit != null)
                        {
                            game_main.user_interface.SetStatusUnit(target_unit);
                        }

                        Console.WriteLine("DoAction {0} {1} {2} {3}", a.command, ad.range_type, select_unit.name, a.target.name);
                        scene_battle.DoAction(a.command, ad.range_type, select_unit, a.target_map_x, a.target_map_y, false);
                    }, null);
                    Console.WriteLine("----B");
                    STaskManager.Add(t);
                    Console.WriteLine("----C");
                    t.WaitEnd();
                }
            }
            Thread.Sleep(100);
            g3d_map.ClearRangeAreaEffect();

            //Thread.Sleep(1000);
            //scene_battle.NextUnitTurn();

            is_thread_end = true;
            Console.WriteLine("BattleAI Run End");
        }
        
        public bool GetIsEnd()
        {
            return is_thread_end;
        }

        void CalcMoveEvalution( MovePoint mp )
        {
            // 行動の対象がいるかどうかと、その価値
            var action_e = 5;
            {
                int num, effective_point_avg, effective_point_max;
                if (IsExistActionTarget( mp.map_x, mp.map_y, out num, out effective_point_avg, out effective_point_max, null) )
                {
                    action_e = 0;
                }
            }
            mp.evaluation -= action_e;
        }

        // 行動対象がいるかどうかと有効度の判定
        bool IsExistActionTarget( int map_x, int map_y, out int num, out int effective_point_avg, out int effective_point_max, ActionDatas action_datas )
        {
            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;
            num = 0;
            effective_point_avg = 0;
            effective_point_max = 0;

            foreach (var action in select_unit.bt.actions)
            {
                var ad = ActionDataManager.GetActionData(action.system_name);

                action_target_area.range_min = ad.range_min;
                action_target_area.Check(map_x, map_y, ad.range_max, 1);

                for (var mx = 0; mx < action_target_area.map_w; mx++)
                {
                    for (var my = 0; my < action_target_area.map_h; my++)
                    {
                        if (action_target_area.IsInside(mx, my))
                        {
                            string my_group = game_main.unit_manager.GetUnitGroup(select_unit.map_x, select_unit.map_y);
                            string target_group = game_main.unit_manager.GetUnitGroup(mx, my);
                            if (my_group != target_group)
                            {
                                num++;
                                var ep = 5;
                                if (effective_point_max < ep) effective_point_max = ep;
                                if (action_datas != null)
                                {
                                    var a = new Action();
                                    a.command = action.system_name;
                                    a.target = game_main.unit_manager.GetUnit(mx, my);
                                    a.target_map_x = mx;
                                    a.target_map_y = my;
                                    action_datas.actions.Add(a);
                                }
                            }

                        }
                    }
                }

                //switch (action.name)
                //{
                //    case "攻撃／剣":
                //        {
                //            action_target_area.range_min = 1;
                //            action_target_area.Check(map_x, map_y, 1, 1);

                //            for (var mx = 0; mx < action_target_area.map_w; mx++)
                //            {
                //                for (var my = 0; my < action_target_area.map_h; my++)
                //                {
                //                    if (action_target_area.IsInside(mx, my))
                //                    {
                //                        string my_group = game_main.unit_manager.GetUnitGroup(select_unit.map_x, select_unit.map_y);
                //                        string target_group = game_main.unit_manager.GetUnitGroup(mx, my);
                //                        if (my_group != target_group)
                //                        {
                //                            num++;
                //                            var ep = 5;
                //                            if (effective_point_max < ep) effective_point_max = ep;
                //                            if (action_datas!=null)
                //                            {
                //                                var a = new Action();
                //                                a.command = action.name;
                //                                a.target = game_main.unit_manager.GetUnit(mx, my);
                //                                a.target_map_x = mx;
                //                                a.target_map_y = my;
                //                                action_datas.actions.Add(a);
                //                            }
                //                        }

                //                    }
                //                }
                //            }
                //        }
                //        break;
                //    case "攻撃／弓":
                //        {
                //             とりあえず、3～6の射程扱いで計算する
                //            action_target_area.range_min = 3;
                //            action_target_area.Check(map_x, map_y, 6, 1);

                //            for (var mx = 0; mx < action_target_area.map_w; mx++)
                //            {
                //                for (var my = 0; my < action_target_area.map_h; my++)
                //                {
                //                    if (action_target_area.IsInside(mx, my))
                //                    {
                //                        string my_group = game_main.unit_manager.GetUnitGroup(select_unit.map_x, select_unit.map_y);
                //                        string target_group = game_main.unit_manager.GetUnitGroup(mx, my);
                //                        if (my_group != target_group)
                //                        {
                //                            num++;
                //                            var ep = 5;
                //                            if (effective_point_max < ep) effective_point_max = ep;
                //                            if (action_datas != null)
                //                            {
                //                                var a = new Action();
                //                                a.command = action.name;
                //                                a.target = game_main.unit_manager.GetUnit(mx, my);
                //                                a.target_map_x = mx;
                //                                a.target_map_y = my;
                //                                action_datas.actions.Add(a);
                //                            }
                //                        }

                //                    }
                //                }
                //            }
                //        }
                //        break;
                //    case "攻撃／ヒートボイル":
                //        {
                //            action_target_area.range_min = 2;
                //            action_target_area.Check(map_x, map_y, 4, 1);

                //            for (var mx = 0; mx < action_target_area.map_w; mx++)
                //            {
                //                for (var my = 0; my < action_target_area.map_h; my++)
                //                {
                //                    if (action_target_area.IsInside(mx, my))
                //                    {
                //                        string my_group = game_main.unit_manager.GetUnitGroup(select_unit.map_x, select_unit.map_y);
                //                        string target_group = game_main.unit_manager.GetUnitGroup(mx, my);
                //                        if (my_group != target_group)
                //                        {
                //                            num++;
                //                            var ep = 5;
                //                            if (effective_point_max < ep) effective_point_max = ep;
                //                            if (action_datas != null)
                //                            {
                //                                var a = new Action();
                //                                a.command = action.name;
                //                                a.target = game_main.unit_manager.GetUnit(mx, my);
                //                                a.target_map_x = mx;
                //                                a.target_map_y = my;
                //                                action_datas.actions.Add(a);
                //                            }
                //                        }

                //                    }
                //                }
                //            }
                //        }
                //        break;
                //}

                if (num>0)
                {
                    return true;
                }
            }
            return false;
        }

        // 敵までの距離を計る
        // 移動力を100扱いにして、最短距離、平均距離を計算していく
        // is_friendsで味方の距離と敵の距離どちらか選択できる（中立や第三勢力は不可）
        RangeStatus CalcRangeStatus( int map_x, int map_y, bool is_friends )
        {
            var rs = new RangeStatus();
            rs.map_x = map_x;
            rs.map_y = map_y;
            rs.evaluation_min = -1;
            rs.evaluation_avg = 0;
            rs.evaluation_num = 0;


            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;
            const int movement_force = 100;

            move_area.is_range_only = true;
            move_area.Check(map_x, map_y, movement_force, select_unit.bt.status["JUMP"].now); move_area.is_range_only = false;
            foreach ( var um in unit_manager.units)
            {
                if (!um.unit.bt.is_alive) continue;
                if ( is_friends == (select_unit_group==um.group) )
                {
                    // その座標までの距離を取得
                    var i = um.unit.map_x + um.unit.map_y * move_area.map_w;
                    var range = move_area.squares[i].step_count;

                    // Console.WriteLine("\t range {0}", range);
                    if (range > 0)
                    {
                        if (rs.evaluation_num == 0 || range < rs.evaluation_min) rs.evaluation_min = range;
                        rs.evaluation_num++;
                        rs.evaluation_avg += range;
                    }

                }
            }

            rs.evaluation_avg = rs.evaluation_avg / rs.evaluation_num;

            return rs;
        }
    }
}
