using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;

namespace TOL_SRPG.App
{
    // 戦闘準備シーン
    public class ScenePreBattle : Scene
    {
        Script setup_script;
        Unit select_unit = null;
        //int font_handle_unit_list;
        SFont font_unit_list;
        SFont font_side_button;

        double unit_pos_offset_y_timer = 0.0;
        List<UIBoxString> ui_unit_names = new List<UIBoxString>();
        List<UIBoxString> ui_side_button = new List<UIBoxString>();

        string next_battle_setup_script_path = "";

        public ScenePreBattle( string script_path, string next_battle_setup_script_path)
        {
            this.next_battle_setup_script_path = next_battle_setup_script_path;
            var game_main = GameMain.GetInstance();
            game_main.g3d_map = new G3DMap(game_main.game_base);
            game_main.g3d_camera = new G3DCamera();

            int[] map_data = { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 };

            game_main.g3d_map.map_w = 5;
            game_main.g3d_map.map_h = 3;
            game_main.g3d_map.Setup(map_data);

            setup_script = new Script("data/script/pre_battle.nst", _ScriptLineAnalyze);
            setup_script.Run("Setup");

            game_main.g3d_camera.SetDirectPosAndRot(DX.VGet(-8.257049f, 0f, 63.04372f), DX.VGet(0.6088442f, 0f, 0.793291f));
            game_main.g3d_camera._NotToFixHard();

            font_unit_list = new SFont(GameMain.main_font_name_r, 22, 0, SFont.Antialiasing.Normal, 0);
            font_side_button = new SFont(GameMain.main_font_name_b, 22, 0, SFont.Antialiasing.Normal, 0);

            game_main.g3d_map.is_draw_cursor_turn_owner = true;

            // ユニット一覧をボタンとして作成
            var ox = GameMain.WindowSizeW - (240 + 16);
            var oy = 30 + 16 * 2;
            var w = 240;
            var h = 30;
            var i = 0;
            foreach (var ums in game_main.unit_manager.units)
            {
                var box = new UIBoxString( ox, oy + h * i, w, h, ums.unit.name, font_unit_list );
                ui_unit_names.Add(box);
                i++;
            }

            //
            {
                UIBoxString box;
                box = new UIBoxString(GameMain.WindowSizeW - (80 + 16), 16, 80, h, "完了", font_side_button);
                ui_side_button.Add(box);
                box = new UIBoxString(GameMain.WindowSizeW - (80 + 16 + 140), 16, 130, h, "ステータス", font_side_button);
                ui_side_button.Add(box);
            }
        }

        public override void Update()
        {
            _Update_KeyInput();

            var game_main = GameMain.GetInstance();
            game_main.g3d_map.UpdateInterface();
            game_main.action_manager.Update();
            game_main.unit_manager.Update();
            game_main.g3d_camera.Update();

            // 拾い上げた選択中のユニットをゆらゆら上下させる
            unit_pos_offset_y_timer += 0.1;
            if (unit_pos_offset_y_timer > Math.PI * 2) unit_pos_offset_y_timer -= Math.PI * 2;
            if ( select_unit!=null )
            {
                select_unit.SetPosOffset(0, Math.Sin(unit_pos_offset_y_timer) * 1.0 + 2.5, 0);
            }

            foreach( var box in ui_unit_names) box.Update(); 
            foreach( var box in ui_side_button) box.Update(); 
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

                foreach (var box in ui_unit_names) box.Draw();
                foreach (var box in ui_side_button) box.Draw();

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

            if (game_base.input.mouse_sutatus.left.key_status == Input.MouseButtonKeyStatus.OneCrick)
            {
                var ui_select_side_box = GetUISelectSideBox();
                var ui_select_unit = GetUISelectUnit();

                if (ui_select_side_box != null)
                { // UI への入力
                    switch (ui_select_side_box.GetString())
                    {
                        case "完了":
                            SoundManager.PlaySound("システム／出陣", 0.5);
                            user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                            user_interface.SetStatusUnit(null);
                            game_main.NextScene(new SceneBattle(next_battle_setup_script_path, true));
                            break;
                        case "ステータス":
                            if (select_unit != null)
                            {
                                if (user_interface.mode != UserInterface.Mode.UnitStatusView)
                                {
                                    SoundManager.PlaySound("システム／選択", 0.5);
                                    user_interface.SetMode(UserInterface.Mode.UnitStatusView, game_base.input.mouse_sutatus.position);
                                    user_interface.SetStatusUnit(select_unit);
                                }
                                else
                                {
                                    SoundManager.PlaySound("システム／解除", 0.5);
                                    user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                                    user_interface.SetStatusUnit(null);
                                }
                            }
                            else
                            {
                                SoundManager.PlaySound("システム／失敗", 0.5);
                            }
                            break;
                    }


                }
                else if (ui_select_unit != null)
                { // UI キャラクター一覧 への入力
                    if (select_unit == ui_select_unit)
                    {
                        if (user_interface.mode != UserInterface.Mode.UnitStatusView)
                        {
                            SetSelectUnit(null);
                        }
                        else
                        {
                            SoundManager.PlaySound("システム／失敗", 0.5);
                        }

                        //user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                        //user_interface.SetStatusUnit(null);
                    }
                    else
                    {
                        if (user_interface.mode == UserInterface.Mode.UnitStatusView)
                        {
                            user_interface.SetStatusUnit(ui_select_unit);
                        }
                        SetSelectUnit(ui_select_unit);
                    }
                }
                else if (g3d_map.map_cursor_x >= 0 && g3d_map.map_cursor_y >= 0)
                { // 3Dマップへの入力
                    switch (user_interface.mode)
                    {
                        case UserInterface.Mode.UnitStatusView:
                            user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                            user_interface.SetStatusUnit(null);
                            break;

                        default:
                            {
                                var u = unit_manager.GetUnit(g3d_map.map_cursor_x, g3d_map.map_cursor_y);
                                if (select_unit != u)
                                {
                                    if (u != null)
                                    {
                                        if (select_unit == null)
                                        { // 選択対象を変更
                                            SetSelectUnit(u);
                                        }
                                        else
                                        { // 位置を入れ替え
                                            SoundManager.PlaySound("システム／入れ替え", 0.5);
                                            var select_map_x = select_unit.map_x;
                                            var select_map_y = select_unit.map_y;

                                            select_unit.SetPosOffset(0, 0, 0);
                                            select_unit.Move(u.map_x, u.map_y, true);
                                            u.Move(select_map_x, select_map_y, true);
                                            //g3d_map.SetTurnOwnerCursor(null);
                                            //select_unit = null;
                                        }
                                    }
                                    else if (select_unit != null)
                                    { // 選択対象を移動
                                        if (g3d_map.map_cursor_x >= 0 && g3d_map.map_cursor_y >= 0)
                                        {
                                            SoundManager.PlaySound("システム／入れ替え", 0.5);
                                            select_unit.Move(g3d_map.map_cursor_x, g3d_map.map_cursor_y, true);
                                        }
                                        else
                                        {
                                            SetSelectUnit(null);
                                        }
                                    }
                                }
                                else if (select_unit != null)
                                { // 同じなら一旦選択解除
                                    SetSelectUnit(null);
                                }
                                user_interface.SetStatusUnit(select_unit);
                            }
                            break;
                    }
                }
                else
                { // 3Dマップ領域外

                    switch (user_interface.mode)
                    {
                        case UserInterface.Mode.UnitStatusView:
                            user_interface.SetMode(UserInterface.Mode.PlayerTurnFree, game_base.input.mouse_sutatus.position);
                            user_interface.SetStatusUnit(null);
                            break;
                        default:
                            if (select_unit != null)
                            {
                                SetSelectUnit(null);
                            }
                            break;
                    }
                }
            }
        }

        void SetSelectUnit( Unit u)
        {
            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;

            if (select_unit != null)
            {
                select_unit.SetPosOffset(0, 0, 0);
            }

            if (u != null)
            {
                SoundManager.PlaySound("システム／選択", 0.5);
                select_unit = u;
                g3d_map.SetTurnOwnerCursor(u);
                g3d_map.is_draw_cursor_turn_owner = true;
            }
            else
            {
                SoundManager.PlaySound("システム／解除", 0.5);
                select_unit = null;
                g3d_map.SetTurnOwnerCursor(null);
                g3d_map.is_draw_cursor_turn_owner = true;
            }

        }

        public bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            var game_main = GameMain.GetInstance();
            //var user_interface = game_main.user_interface;
            //var game_base = game_main.game_base;
            //var g3d_camera = game_main.g3d_camera;
            var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;
            //var action_manager = game_main.action_manager;

            switch (t.command[0])
            {
                //case "MapData":
                //    {
                //        var size = t.command.Count();
                //        for (var i = 1; i < size; i++)
                //        {
                //            setup_script_data.map_data.Add(t.GetInt(i));
                //        }
                //        setup_script_data.map_w = size - 1;
                //    }
                //    break;
                //case "MapSetup":
                //    {
                //        setup_script_data.map_h = setup_script_data.map_data.Count() / setup_script_data.map_w;
                //        g3d_map.map_w = setup_script_data.map_w;
                //        g3d_map.map_h = setup_script_data.map_h;

                //        g3d_map.Setup(setup_script_data.map_data.ToArray());

                //    }
                //    break;
                case "Unit":
                    {
                        var x = t.GetInt(1);
                        var y = t.GetInt(2);
                        var unit_class_name = t.GetString(3);
                        var ucd = UnitDataManager.GetUnitClassData(unit_class_name);
                        var model_path = "data/model/" + t.GetString(4) + "/_.pmd";
                        var image_face_path = "data/image/face/" + t.GetString(5);

                        // 下記のifはUnitのコンストラクタで処理しても良い？
                        if (t.GetString(4) == "") model_path = ucd.model_default_path;
                        if (t.GetString(5) == "") image_face_path = ucd.image_default_path;

                        var name = t.GetString(6);
                        var group = t.GetString(7);
                        var color_no = t.GetInt(8);
                        var direction = t.GetInt(9);
                        var unit = new Unit(unit_class_name, model_path, image_face_path, name, x, y, color_no, direction);
                        unit_manager.Join(unit, group);

                    }
                    //unit_manager.Join(new Unit(path, 3, 5, 1, 0), "敵");
                    break;
                //case "DebugMode":
                //    if (t.GetString(1) == "True")
                //    {
                //        is_debug_mode = true;
                //    }
                //    else
                //    {
                //        is_debug_mode = false;
                //    }
                //    break;
            }
            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            font_unit_list.Dispose();
        }

        
        Unit GetUISelectUnit()
        {
            int i = 0;
            foreach (var box in ui_unit_names)
            {
                if (box.IsHit()) {
                    var units = GameMain.GetInstance().unit_manager.units;
                    if (i<units.Count)
                    {
                        return units[i].unit;
                    }
                    else
                    {
                        return null;
                    }
                }
                i++;
            }
            return null;
        }

        UIBoxString GetUISelectSideBox()
        {
            foreach ( var box in ui_side_button )
            {
                if (box.IsHit()) return box;
            }
            return null;
        }

    }

    public class UIBoxBase : IDisposable
    {
        public virtual void Update() { }
        public virtual void Draw() { }
        public virtual bool IsHit() { return false; }
        public virtual void Dispose() { }
    }

    public class UIBoxString : UIBoxBase
    {
        int ox, oy, w, h;
        SFont font_view_text;
        string view_text;

        int offset_box_x = 0;
        int offset_box_y = 0;
        int offset_text_x = 5;
        int offset_text_y = 4;

        bool is_mouse_on = false;


        public UIBoxString( int x, int y, int w, int h, string view_text, SFont font_view_text)
        {
            this.ox = x;
            this.oy = y;
            this.w = w;
            this.h = h;
            this.view_text = view_text;
            this.font_view_text = font_view_text;
        }
        
        // 中央配置のオフセット
        // 引数に未対応
        public void SetCenter( bool is_react, bool is_text )
        {
            int sw = 0;
            int sh = 0;
            int sc = 0;
            DX.GetDrawStringSizeToHandle(out sw, out sh, out sc, view_text, view_text.Length, font_view_text.GetHandle());

            offset_box_x = -w / 2;
            offset_box_y = -h / 2;
            offset_text_x = - sw / 2;
            offset_text_y = - sh / 2 + 2;
        }

        public override void Update()
        {
            if (IsHit())
            {
                if (!is_mouse_on) SoundManager.PlaySound("システム／カーソル移動", 0.5);

                is_mouse_on = true;
            }
            else
            {
                is_mouse_on = false;
            }

        }

        public override void Draw()
        {
            var color_base = DX.GetColor(200, 200, 200);
            var color_outline = DX.GetColor(60, 60, 60);
            var color_text = DX.GetColor(20, 20, 20);

            var x = ox + offset_box_x;
            var y = oy + offset_box_y;

            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, 200);
            DX.DrawBox(x, y, x + w, y + h, color_base, DX.TRUE);
            DX.DrawBox(x, y, x + w, y + h, color_outline, DX.FALSE);
            if (is_mouse_on)
            {
                DX.SetDrawBlendMode(DX.DX_BLENDMODE_ADD, 128);
                DX.DrawBox(x, y, x + w, y + h, color_base, DX.TRUE);
            }
            //DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, 255);
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);

            var tx = ox + offset_text_x;
            var ty = oy + offset_text_y;
            DX.DrawStringFToHandle(tx, ty, view_text, color_text, font_view_text.GetHandle());

        }

        public override bool IsHit()
        {
            var game_main = GameMain.GetInstance();
            var pos = game_main.game_base.input.mouse_sutatus.position;

            var x = ox + offset_box_x;
            var y = oy + offset_box_y;
            if ((x <= pos.X && pos.X < x + w) && (y <= pos.Y && pos.Y < y + h))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Dispose() { }

        public string GetString()
        {
            return view_text;
        }
    }

}
