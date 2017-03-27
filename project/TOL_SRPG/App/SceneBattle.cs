using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;
using TOL_SRPG.App.Map;

// 戦闘シーンを管理する
namespace TOL_SRPG.App
{
    public class Scene : IDisposable
    {
        public Scene()
        {
        }

        public virtual void Update() {}
        public virtual void Draw( bool is_shadowmap )   {}
        public virtual void Dispose() {}
    }

    public class SceneBattle : Scene
    {
        protected Unit select_unit = null; // 選択中のユニット

        // 手番ユニット管理用
        public class TurnOnwerUnit
        {
            public UnitManager.UnitManagerStatus unit_manager_status;
            public bool end_action = false;
            public bool end_sub_action = false;
            public bool end_move = false;
        }

        public TurnOnwerUnit turn_owner_unit = new TurnOnwerUnit();
        //public UnitManager.UnitManagerStatus unit_turn_owner = null; // 手番ユニット


        public Script setup_script;
        public class ScriptData
        {
            public int map_w;
            public int map_h;
            public List<int> map_data = new List<int>();
        }
        public ScriptData setup_script_data = new ScriptData();

        bool is_continue_player_unit = false;
        UserInterface.Command select_command = null;

        BattleAI battle_ai = null;
        bool is_battle_end_check = true; // 戦闘中 → 勝利・敗北の判定を行うかどうか


        //S3DPanel s3dpanel = new S3DPanel(new S3DPoint(5, 5, 5), new SDPoint(10, 10), S3DPanel.Direction.Wall_EW);
        //S3DCube s3dcube = new S3DCube(new S3DPoint(15, 7.5, 15), new S3DPoint(10, 15, 5));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="is_continue_player_unit">プレーヤーユニットを引き継ぐかどうか trueならスクリプトから味方を生成しない</param>
        public SceneBattle( string script_path, bool is_continue_player_unit )
        {
            this.is_continue_player_unit = is_continue_player_unit;

            var game_main = GameMain.GetInstance();

            game_main.g3d_map = new BattleMap(game_main.game_base);
            game_main.g3d_camera = new G3DCamera();
            setup_script = new Script(script_path, _ScriptLineAnalyze);
            setup_script.Run("Setup");

            // 戦闘初期化
            turn_owner_unit.unit_manager_status = null;
            game_main.unit_manager.SetupBattle();
            NextUnitTurn();



            ActionManager.Add(new ActionBattleStart(0));
        }

        public override void Update()
        {
            _Update_KeyInput();

            var game_main = GameMain.GetInstance();
            game_main.g3d_map.UpdateInterface();
            game_main.action_manager.Update();
            game_main.unit_manager.Update();
            game_main.g3d_camera.Update();

            if (is_battle_end_check)
            {
                CheckGameEnd();
            }

        }

        public override void Draw(bool is_shadowmap)
        {
            var game_main = GameMain.GetInstance();

            if (is_shadowmap)
            {
                game_main.g3d_map.Draw();
                game_main.unit_manager.Draw();
                game_main.action_manager.Draw();
            }
            else
            {
                game_main.g3d_map.Draw();
                game_main.unit_manager.Draw();
                game_main.action_manager.Draw();
                game_main.user_interface.Draw();
            }

        }

        void _Update_KeyInput()
        {
            var game_main = GameMain.GetInstance();
            var user_interface = game_main.user_interface;
            var game_base = game_main.game_base;
            var g3d_camera = game_main.g3d_camera;
            var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;
            user_interface.Update(game_base.input.mouse_sutatus.position);

            if (game_main.action_manager.IsControlFreese())
            {
                g3d_map.is_draw_cursor_turn_owner = false;
                return;
            }
            else
            {
                g3d_map.is_draw_cursor_turn_owner = true;
            }

            //
            if (DX.CheckHitKey(DX.KEY_INPUT_Q) == DX.TRUE)
            {
                g3d_camera.AddRot(0.03f);
            }
            if (DX.CheckHitKey(DX.KEY_INPUT_E) == DX.TRUE)
            {
                g3d_camera.AddRot(-0.03f);
            }
            if (DX.CheckHitKey(DX.KEY_INPUT_A) == DX.TRUE)
            {
                g3d_camera.MoveYRot(1.00f, 90);
            }
            if (DX.CheckHitKey(DX.KEY_INPUT_D) == DX.TRUE)
            {
                g3d_camera.MoveYRot(1.00f, -90);
            }
            if (DX.CheckHitKey(DX.KEY_INPUT_S) == DX.TRUE)
            {
                g3d_camera.MoveFront(-1.0f);
            }
            if (DX.CheckHitKey(DX.KEY_INPUT_W) == DX.TRUE)
            {
                g3d_camera.MoveFront(1.0f);
            }

            if (DX.CheckHitKey(DX.KEY_INPUT_F) == DX.TRUE)
            {
                //g3d_camera.MoveFront(1.0f);
                g3d_map.SetMoveRouteEffect(g3d_map.map_cursor_x, g3d_map.map_cursor_y);
            }

            if (game_base.input.GetKeyInputStatus((int)GameMain.KeyCode.DebugView) == Input.KeyInputStatus.Up)
            {
                game_main.debug_is_view = !game_main.debug_is_view;
            }

            //if (user_interface.mode == UserInterface.Mode.NonePlayerTurn)
            if (battle_ai!=null)
            {
                if ( battle_ai.GetIsEnd() )
                {
                    battle_ai = null;
                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                    NextUnitTurn();
                }

            }
            else if (game_base.input.mouse_sutatus.left.key_status == Input.MouseButtonKeyStatus.OneCrick)
            {
                if (user_interface.IsHitUI()) // UIへの入力
                {
                    var command = user_interface.GetHitUICommand();
                    var action_data = ActionDataManager.GetActionData(command.system_name);
                    switch (user_interface.mode)
                    {
                        case UserInterface.Mode.TurnTopCommandShow:
                            select_command = command;
                            switch (command.system_name)
                            {
                                default:
                                    if (action_data != null)
                                    {
                                        user_interface.SetMode(UserInterface.Mode.AtackTargetSelect, game_base.input.mouse_sutatus.position);
                                        user_interface.mode_param1 = action_data.range_type;
                                        g3d_map.ClearRangeAreaEffect();
                                        g3d_map.SetActionTargetAreaEffect(select_unit.map_x, select_unit.map_y, action_data.range_min, action_data.range_max, 1);
                                    }
                                    else
                                    {
                                        user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                        g3d_map.ClearRangeAreaEffect();
                                    }
                                    break;
                                case "移動":
                                    user_interface.SetMode(UserInterface.Mode.MovePointSelect, game_base.input.mouse_sutatus.position);
                                    break;
                                case "待機":
                                    _Update_KeyInput_TurnOnwerEnd();
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    g3d_map.ClearRangeAreaEffect();
                                    break;
                                case "ステータス":
                                    user_interface.SetMode(UserInterface.Mode.UnitStatusView, game_base.input.mouse_sutatus.position);
                                    user_interface.SetStatusUnit(select_unit);
                                    g3d_map.ClearRangeAreaEffect();
                                    break;
                                case "AI":
                                    user_interface.SetMode(UserInterface.Mode.NonePlayerTurn, game_base.input.mouse_sutatus.position);
                                    g3d_map.ClearRangeAreaEffect();
                                    battle_ai = new BattleAI(this, select_unit);
                                    break;
                                case "Game Over":
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    game_main.NextScene(new SceneEndBattle("Game Over", ""));
                                    break;
                                case "Stage Clear":
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    game_main.NextScene(new SceneEndBattle("Stage Clear", ""));
                                    break;
                            }
                            break;
                        case UserInterface.Mode.SubTopCommandShow:
                            select_command = command;
                            switch (command.system_name)
                            {
                                default:
                                    if (action_data != null)
                                    {
                                        user_interface.SetMode(UserInterface.Mode.AtackTargetSelect, game_base.input.mouse_sutatus.position);
                                        user_interface.mode_param1 = action_data.range_type;
                                        g3d_map.ClearRangeAreaEffect();
                                        g3d_map.SetActionTargetAreaEffect(select_unit.map_x, select_unit.map_y, action_data.range_min, action_data.range_max, 1);
                                    }
                                    else
                                    {
                                        user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                        g3d_map.ClearRangeAreaEffect();
                                    }
                                    break;
                                case "ステータス":
                                    user_interface.SetMode(UserInterface.Mode.UnitStatusView, game_base.input.mouse_sutatus.position);
                                    user_interface.SetStatusUnit(select_unit);
                                    g3d_map.ClearRangeAreaEffect();
                                    break;
                                //case "AI":
                                //    user_interface.SetMode(UserInterface.Mode.NonePlayerTurn, game_base.input.mouse_sutatus.position);
                                //    g3d_map.ClearRangeAreaEffect();
                                //    battle_ai = new BattleAI(this, select_unit);
                                //    break;
                            }
                            break;
                    }
                }
                else if (g3d_map.map_cursor_x >= 0 && g3d_map.map_cursor_y >= 0) // 3Dマップへの入力
                {
                    switch (user_interface.mode)
                    {
                        case UserInterface.Mode.MovePointSelect: // 移動選択
                            {
                                var is_inside = g3d_map.move_area.IsInside(g3d_map.map_cursor_x, g3d_map.map_cursor_y);
                                if (is_inside)
                                { // 移動範囲内
                                    select_unit.Move(g3d_map.map_cursor_x, g3d_map.map_cursor_y, true);
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    if (!game_main.debug_status.is_battle_ui_debug_mode)
                                    {
                                        turn_owner_unit.end_move = true;
                                    }
                                }
                                else
                                { // 移動範囲外
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                }
                                g3d_map.ClearRangeAreaEffect();
                            }
                            break;
                        case UserInterface.Mode.AtackTargetSelect: // 攻撃対象選択
                            {
                                DoAction(select_command.system_name, user_interface.mode_param1, select_unit, g3d_map.map_cursor_x, g3d_map.map_cursor_y, true);
                            }
                            break;
                        case UserInterface.Mode.UnitStatusView:
                            user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                            break;
                        default:
                            {
                                var u = unit_manager.GetUnit(g3d_map.map_cursor_x, g3d_map.map_cursor_y);
                                if (game_main.debug_status.is_battle_ui_debug_mode && (u != null))
                                {
                                    turn_owner_unit.unit_manager_status = GetUnitManagerStatus(u);
                                    g3d_map.SetMoveAreaEffect(g3d_map.map_cursor_x, g3d_map.map_cursor_y, u.bt.status["MOVE"].now, u.bt.status["JUMP"].now, "");
                                    user_interface.SetMode(UserInterface.Mode.TurnTopCommandShow, game_base.input.mouse_sutatus.position, turn_owner_unit);
                                    select_unit = u;
                                }
                                else if ((u != null) && (u == turn_owner_unit.unit_manager_status.unit))
                                {

                                    //if (select_unit == u) 
                                    //{
                                    //    g3d_map.ClearRangeAreaEffect();
                                    //    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    //    select_unit = null;
                                    //}
                                    //else
                                    //{
                                        g3d_map.SetMoveAreaEffect(g3d_map.map_cursor_x, g3d_map.map_cursor_y, u.bt.status["MOVE"].now, u.bt.status["JUMP"].now, "");
                                        user_interface.SetMode(UserInterface.Mode.TurnTopCommandShow, game_base.input.mouse_sutatus.position, turn_owner_unit);
                                        select_unit = u;
                                    //}
                                }
                                else if ( u != null )
                                {
                                    g3d_map.SetMoveAreaEffect(g3d_map.map_cursor_x, g3d_map.map_cursor_y, u.bt.status["MOVE"].now, u.bt.status["JUMP"].now, "");
                                    
                                    //if(select_unit==u) // 2度クリックしたときはコマンド表示を消す
                                    //{
                                    //    g3d_map.ClearRangeAreaEffect();
                                    //    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    //    select_unit = null;
                                    //}
                                    //else
                                    //{
                                        user_interface.SetMode(UserInterface.Mode.SubTopCommandShow, game_base.input.mouse_sutatus.position, null);
                                        select_unit = u;
                                    //}
                                }
                                else
                                {
                                    g3d_map.ClearRangeAreaEffect();
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    select_unit = null;
                                }
                            }
                            break;
                    }
                }
            }
            else
            {
                switch (user_interface.mode)
                {
                    case UserInterface.Mode.UnitStatusView:
                        //if (!user_interface.IsHitUI())
                        //{
                        //    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                        //}
                        break;
                    default:
                        if (!user_interface.IsHitUI())
                        {
                            var u = unit_manager.GetUnit(g3d_map.map_cursor_x, g3d_map.map_cursor_y);
                            user_interface.SetStatusUnit(u);
                        }
                        else
                        {
                            user_interface.SetStatusUnit(select_unit);
                        }
                        break;
                }
            }
        }


        // todo: 行動させるのと、UI制御が混ざってるので、分離したほうが良さそう
        // 行動の実行できるかどうかについても判定しているからややこしい（AIのところで食い違う…多重チェックになってる）
        public void DoAction( string command, string sub_command, Unit action_unit, int target_map_x, int target_map_y, bool is_user_owner)
        {
            var game_main = GameMain.GetInstance();
            var user_interface = game_main.user_interface;
            var game_base = game_main.game_base;
            var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;
            var target_unit = unit_manager.GetUnit(target_map_x, target_map_y);

            var is_inside = false;
            var action_data = ActionDataManager.GetActionData(command);

            if ( !is_user_owner )
            {
                // 自動・NPCでの操作の場合
                g3d_map.ClearRangeAreaEffect();
                g3d_map.SetActionTargetAreaEffect(action_unit.map_x, action_unit.map_y, action_data.range_min, action_data.range_max, 1);
                is_inside = g3d_map.action_target_area.IsInside(target_map_x, target_map_y);
            }
            else
            {
                is_inside = g3d_map.action_target_area.IsInside(target_map_x, target_map_y);
            }

            var is_action_do_ok = false;
            switch ( action_data.range_ok_type)
            {
                case "範囲内": // 範囲内しか行動の実行を認めない
                    if (is_inside) is_action_do_ok = true;
                    break;

                case "延長可": // 射程最小値より大きければ、射程最大値外でも行動の実行を認める
                    //if (is_inside) action_do_ok = true;
                    {
                        var range = Math.Abs(target_map_x - action_unit.map_x) + Math.Abs(target_map_y - action_unit.map_y);
                        if (range >= action_data.range_min) is_action_do_ok = true;
                    }
                    break;
            }
            if (!is_action_do_ok)
            {
                // 行動不可なので、中断
                if (is_user_owner)
                {
                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                    g3d_map.ClearRangeAreaEffect();
                }
                return;
            }

            // todo: 行動のモーション、サウンドに関わるため、一旦はこのまま（先に他を整理）
            switch(command)
            {
                case "Basic／攻撃／剣":
                    {
                        var damage = GetDamage(action_unit, target_unit, action_data);
                        var pos = g3d_map.GetScreenPositionByUnitTop(target_map_x, target_map_y);
                        ActionManager.Add(new ActionSwing(0));
                        ActionManager.Add(new ActionDamage(15, pos.X, pos.Y, damage, target_unit));
                    }
                    break;
                case "Basic／攻撃／弓":
                    {
                        var damage = 0;
                        var is_success = target_unit != null;
                        if (is_success)
                        {
                            damage = GetDamage(action_unit, target_unit, action_data);
                        }
                        var start_map_x = g3d_map.action_target_area.start_map_x;
                        var start_map_y = g3d_map.action_target_area.start_map_y;

                        // todo: 攻撃アニメーションで必要な情報のため
                        // ここ変えたほうが良さそうだけどね...
                        // たぶんAction側に軌道データもたせたほうが良い
                        var height_offset = 1.0f;
                        var route_as_shoot = new RouteAsShoot(start_map_x, start_map_y, height_offset, target_map_x, target_map_y, height_offset, 0);

                        var pos = g3d_map.GetScreenPositionByUnitTop(target_map_x, target_map_y);

                        ActionManager.Add(new ActionShootArrow(0, route_as_shoot, is_success));
                        if (is_success)
                        {
                            ActionManager.Add(new ActionDamage(95, pos.X, pos.Y, damage, target_unit));
                        }
                    }
                    break;
                case "Basic／攻撃／ヒートボイル":
                    {
                        var damage = GetDamage(action_unit, target_unit, action_data);
                        var pos = g3d_map.GetScreenPositionByUnitTop(target_map_x, target_map_y);
                        ActionManager.Add(new ActionSimmple(0, "戦闘／攻撃／魔法・ヒートボイル"));
                        ActionManager.Add(new ActionDamage(15, pos.X, pos.Y, damage, target_unit));
                    }
                    break;
            }
            if (is_user_owner)
            {
                user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                g3d_map.ClearRangeAreaEffect();
            }

            // 手番ユニットの行動済みを設定
            if (!game_main.debug_status.is_battle_ui_debug_mode)
            {
                turn_owner_unit.end_action = true;
            }
        }

        public void _Update_KeyInput_TurnOnwerEnd()
        {
            NextUnitTurn();

        }

        // 勝利条件と敗北条件の確認
        public void CheckGameEnd()
        {
            var game_main = GameMain.GetInstance();
            var unit_manager = game_main.unit_manager;

            // 敵全滅チェック
            var is_alive = false;
            foreach (var um in unit_manager.units)
            {
                if (um.group.IndexOf("敵") >= 0 && um.unit.bt.is_alive)
                {
                    is_alive = true;
                }
            }
            if (!is_alive)
            {
                // 全滅である
                game_main.NextScene(new SceneEndBattle("Stage Clear", ""));
                return;
            }

            // 味方全滅チェック
            is_alive = false;
            foreach (var um in unit_manager.units)
            {
                if (um.group.IndexOf("味方") >= 0 && um.unit.bt.is_alive)
                {
                    is_alive = true;
                }
            }
            if (!is_alive)
            {
                // 全滅である
                game_main.NextScene(new SceneEndBattle("Game Over", ""));
                return;
            }
        }

        // 次の手番ユニットを決める
        public void NextUnitTurn()
        {
            // 旧手番ユニットのWTを調整しておく
            if (turn_owner_unit.unit_manager_status!=null && turn_owner_unit.unit_manager_status.unit.bt.is_alive)
            {
                turn_owner_unit.unit_manager_status.unit.ResetWT();
            }

            var game_main = GameMain.GetInstance();
            var unit_manager = game_main.unit_manager;



            if (unit_manager.active_units.Count() == 0)
            {
                while (unit_manager.active_units.Count() == 0)
                {
                    unit_manager.StepBattle();
                }
            }

            if (unit_manager.active_units.Count() > 0)
            {
                turn_owner_unit.unit_manager_status = unit_manager.active_units[0]; unit_manager.active_units.RemoveAt(0);
                turn_owner_unit.end_action     = false;
                turn_owner_unit.end_sub_action = false;
                turn_owner_unit.end_move       = false;
                //unit_manager.active_units.RemoveAt(0);
                game_main.g3d_map.SetTurnOwnerCursor(turn_owner_unit.unit_manager_status.unit);
            }
            else
            {
                // Err?
                turn_owner_unit.unit_manager_status = null;
                turn_owner_unit.end_action = false;
                turn_owner_unit.end_sub_action = false;
                turn_owner_unit.end_move = false;
            }

            if (!turn_owner_unit.unit_manager_status.unit.bt.is_alive)
            { // 生存してない
                NextUnitTurn();
                return;
            }

            // AIによる行動
            if (game_main.debug_status.is_auto_battle)
            {
                battle_ai = new BattleAI(this, turn_owner_unit.unit_manager_status.unit);
            }
            else if (!game_main.debug_status.is_battle_ui_debug_mode)
            {
                if (turn_owner_unit.unit_manager_status.group != "味方")
                {
                    battle_ai = new BattleAI(this, turn_owner_unit.unit_manager_status.unit);
                }
            }

        }

        public bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            var game_main = GameMain.GetInstance();
            var user_interface = game_main.user_interface;
            var game_base = game_main.game_base;
            //var g3d_camera = game_main.g3d_camera;
            var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;
            //var action_manager = game_main.action_manager;

            switch (t.command[0])
            {
                case "MapLoad":
                    {
                        var map_path = t.GetString(1);
                        g3d_map.Load(map_path);
                    }
                    return true;
                case "MapData":
                    {
                        var size = t.command.Count();
                        for (var i = 1; i < size; i++)
                        {
                            setup_script_data.map_data.Add(t.GetInt(i));
                        }
                        setup_script_data.map_w = size - 1;
                    }
                    return true;
                case "MapSetup":
                    {
                        setup_script_data.map_h = setup_script_data.map_data.Count() / setup_script_data.map_w;
                        g3d_map.map_w = setup_script_data.map_w;
                        g3d_map.map_h = setup_script_data.map_h;

                        g3d_map.Setup(setup_script_data.map_data.ToArray());

                    }
                    return true;
                case "Unit":
                    {
                        var x = t.GetInt(1);
                        var y = t.GetInt(2);
                        var unit_class_name = t.GetString(3);
                        var ucd = UnitDataManager.GetUnitClassData(unit_class_name);
                        var model_path = t.GetString(4);
                        var image_face_path = t.GetString(5);

                        var name = t.GetString(6);
                        var group = t.GetString(7);
                        var color_no = t.GetInt(8);
                        var direction = t.GetInt(9);

                        if ( !(is_continue_player_unit && group == "味方") )
                        {
                            var unit = new Unit( unit_class_name, model_path, image_face_path, name, x, y, color_no, direction);
                            unit_manager.Join(unit, group);
                        }

                    }
                    //unit_manager.Join(new Unit(path, 3, 5, 1, 0), "敵");
                    return true;
            }
            return false;
        }

        UnitManager.UnitManagerStatus GetUnitManagerStatus( Unit u )
        {
            var game_main = GameMain.GetInstance();
            var unit_manager = game_main.unit_manager;

            foreach ( var ums in unit_manager.units )
            {
                if (u == ums.unit) return ums;
            }
            return null;
        }

        int GetDamage( Unit atack_unit, Unit target_unit, ActionData action_data )
        {
            var dm = 0;
            var game_main = GameMain.GetInstance();
            var range = GetRangeBySq(atack_unit, target_unit);

            // 基礎ダメージの算出
            // = 参照能力値 * 参照能力値補正 + 直接指定 + 乱数指定
            var atk = (double)atack_unit.bt.status[action_data.effect_src_main].now * (double)action_data.effect_src_main_correction / 100.0
                + action_data.effect_src_direct + (double)action_data.effect_src_random * game_main.random.NextDouble();

            // 軽減の算出
            // = 参照能力値 * 参照能力値補正
            var def = (double)target_unit.bt.status[action_data.effect_dst_main].now * (double)action_data.effect_dst_main_correction / 100.0;

            // ダメージ算出と端数切り捨て
            dm = (int)(atk - def);
            if (dm < 0) dm = 0;
            
            return dm;
        }

        // マス単位の距離を取得
        int GetRangeBySq( Unit a, Unit b )
        {
            return Math.Abs(a.map_x - b.map_x) + Math.Abs(a.map_y - b.map_y);
        }
        
    }

    public class ActionBattleStart : Action
    {
        public int timer = 0;

        SFont font;

        //int add_y = -100;
        //int add_y_speed = 100;
        //int add_y = 0;
        //int add_y_speed = -50;
        //
        //int ox = 0;
        //int oy = 0;
        int timer_start_wait = 0;


        public ActionBattleStart(int start_wait)
        {
            //ox = 0; oy = 0;
            this.timer_start_wait = start_wait;

            font = new SFont(GameMain.main_font_name_b, 45, 0, SFont.Antialiasing.Normal, 2);
        }

        public override void Update()
        {
            if (timer_start_wait > 0)
            {
                timer_start_wait--;
                return;
            }

            timer++;

            if (timer >= 300) is_end = true;

        }


        public override void Draw()
        {
            if (timer_start_wait > 0) return;
            if (GameMain.GetInstance().is_shadowmap_draw) return;

            var t_max = 50;
            var t_end_start = 200;
            var t_end_time  = 50;
            var t = timer;
            var t2 = timer - t_max;
            if (t > t_max) t = t_max;
            if (t2 < 0) t2 = 0;
            var t3 = timer - t_end_start;
            if (t3 < 0) t3 = 0;
            if (t3 > t_end_time) t3 = t_end_time;
            var x = 100 + (t_max - t) * (t_max - t) * 0.5 + (t_max - t) * (t_max - t) * (t_max - t) * 0.3
               - ( t3 * t3 * 0.02 + t3*t3*t3 * 0.001 );
            //if (x < 100) x = 100;
            var color_text = DX.GetColor(255, 255, 255);
            var color_text_frame = DX.GetColor(80, 80, 80);
            var alpha = 1.0;
            if (t3 > 0) alpha = 1.0 - (double)t3 / (double)t_end_time;
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, (int)(alpha * 255));
            var text = "勝利条件";
            DX.DrawStringFToHandle((float)(x - t2 * 0.05), 120, text, color_text, font.GetHandle(), color_text_frame);

            text = "敵を全滅させろ";
            DX.DrawStringFToHandle((float)(x - t2 * 0.1), 190, text, color_text, font.GetHandle(), color_text_frame);
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);
            //var x = ox;
            //var y = oy;
            //var text = damage_value.ToString();
            //var color_text = DX.GetColor(255, 255, 255);
            //var color_text_frame = DX.GetColor(255, 0, 0);

            //y += add_y;

            //var a_blend = 255;
            //if (timer < 50)
            //{
            //    a_blend = timer * 7;
            //}
            //else if (timer > 130)
            //{
            //    a_blend = 255 - (timer - 130) * 20;
            //}
            //if (a_blend > 255) a_blend = 255;
            //if (a_blend < 0) a_blend = 0;


            //var w = DX.GetDrawStringWidthToHandle(text, text.Count(), font_handle);

            //DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, a_blend);
            //DX.DrawStringFToHandle(x - w / 2, y, text, color_text, font_handle, color_text_frame);
            //DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);

        }

    }
}
