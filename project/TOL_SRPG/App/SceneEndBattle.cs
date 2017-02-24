using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using DxLibDLL;
using DxlibGame;

namespace DxlibGameSimRPG
{
    class SceneEndBattle : Scene
    {
        SFont font;
        string mode_name = "";
        string mode_title = "";
        int timer = 0;
        uint edge_color;

        UIBoxString ui_box_restart;

        public SceneEndBattle( string mode_name, string script_path )
        {
            font = new SFont(GameMain.main_font_name_b, 80, 0, SFont.Antialiasing.Normal, 4);
            this.mode_name = mode_name;

            var game_main = GameMain.GetInstance();
            game_main.user_interface.SetStatusUnit(null);
            game_main.g3d_map.is_draw_cursor_turn_owner = false;

            var font_ui = new SFont(GameMain.main_font_name_r, 22, 0, SFont.Antialiasing.Normal, 0);

            ui_box_restart = new UIBoxString(GameMain.WindowSizeW/2, GameMain.WindowSizeH/2+80, 200, 32, "Restart", font_ui);
            ui_box_restart.SetCenter(true,true);

            switch (mode_name)
            {
                case "Game Over":
                    mode_title = "Game Over";
                    edge_color = DX.GetColor(255, 0, 0);


                    break;
                case "Stage Clear":
                    mode_title = "Stage Clear";
                    edge_color = DX.GetColor(0, 0, 255);

                    break;
                default:
                    mode_title = mode_name;
                    edge_color = DX.GetColor(0,0,255);
                    break;
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

            ui_box_restart.Update();


            if (timer==0)
            {
                switch (mode_name)
                {
                    case "Game Over":
                        SoundManager.PlaySound("イベント／敗北", 0.5);
                        break;
                    case "Stage Clear":
                        SoundManager.PlaySound("イベント／勝利", 0.5);
                        break;
                    default:
                        break;
                }
            }

            timer++;
        }

        void _Update_KeyInput()
        {
            var game_main = GameMain.GetInstance();
            //var user_interface = game_main.user_interface;
            var game_base = game_main.game_base;
            //var g3d_camera = game_main.g3d_camera;
            //var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;

            if (game_base.input.mouse_sutatus.left.key_status == Input.MouseButtonKeyStatus.OneCrick)
            {
                if (ui_box_restart.IsHit())
                {
                    unit_manager.units.Clear();
                    game_main.NextScene(new SceneSetup("data/script/start.nst"));
                }

            }
        }

        public override void Draw(bool is_shadowmap)
        {
            var game_main = GameMain.GetInstance();

            game_main.g3d_map.Draw();
            game_main.unit_manager.Draw();
            game_main.action_manager.Draw();
            game_main.user_interface.Draw();

            var screen = new SDrawImageByScreen();
            screen.FilterGauss(2, 200);

            var alpha = timer * 4;
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            screen.Draw(new SDPoint(0, 0), new Rectangle(0, 0, GameMain.WindowSizeW, GameMain.WindowSizeH), (double)alpha / 255.0, false);
            screen.Dispose();

            alpha = timer * 4;
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, alpha);
            {
                var text = mode_title;
                int sw = 0;
                int sh = 0;
                int sc = 0;
                DX.GetDrawStringSizeToHandle( out sw, out sh, out sc, text, text.Length, font.GetHandle());
                var x = GameMain.WindowSizeW / 2 - sw / 2;
                var y = GameMain.WindowSizeH / 2 - sh / 2;
                DX.DrawStringFToHandle(x, y, text, DX.GetColor(255, 255, 255), font.GetHandle(), edge_color);
            }
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);

            ui_box_restart.Draw();

        }

        public override void Dispose()
        {
            base.Dispose();

            font.Dispose();
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
            }
            return false;
        }
    }
}
