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
    public class UserInterface
    {
        public enum Mode
        {
            NonePlayerTurn,

            PlayerTurnFree,     // 1-x 
            TurnTopCommandShow, // 1-1-x 
            SubTopCommandShow,  // ターンではないキャラクター用のコマンド
            MovePointSelect,
            AtackSelect,
            AtackTargetSelect,
            MagicSelect,
            MagicTargetSelect,
            ItemSelect,
            ItemTargetSelect,
            Talk,
            TalkNextWait,
            UnitStatusView,     // ユニットステータス表示中
        }

        public class Command
        {
            public string view_name; // 表示名
            public string system_name;      // システム名
            public Command( string view_name, string system_name )
            {
                this.view_name = view_name;
                this.system_name = system_name;
            }
        }

        public Mode mode = Mode.PlayerTurnFree;
        public string mode_param1 = "";

        SFont font_ui_text;
        //int font_handle;
        Point command_ui_position = new Point();
        int command_ui_w = 240;
        int command_ui_h = 30;
        List<Command> command_ui_text_list = new List<Command>();
        int command_ui_focus_no = -1;

        // ステータス系
        SFont font_handle_status_name;
        SFont font_handle_status_detail;
        int face_image;
        Unit status_target_unit; // 

        public UserInterface()
        {
            font_ui_text = new SFont(GameMain.main_font_name_r, 22, 0, SFont.Antialiasing.Normal, 0);
            font_handle_status_name = new SFont( GameMain.main_font_name_b, 18, 1, SFont.Antialiasing.Normal, 2);
            font_handle_status_detail = new SFont(GameMain.main_font_name_b, 18, 1, SFont.Antialiasing.Normal, 0);

            face_image = DX.LoadGraph("data/face_sammple.png");
        }

        public void Update( Point mouse_point )
        {
            var before_command_ui_focus_no = command_ui_focus_no;
            command_ui_focus_no = -1;
            switch (mode)
            {
                case Mode.TurnTopCommandShow:
                    for (var i = 0; i < command_ui_text_list.Count(); i++)
                    {
                        var ox = command_ui_position.X;// + command_ui_w * i;
                        var oy = command_ui_position.Y + command_ui_h * i;
                        var w = command_ui_w;
                        var h = command_ui_h;
                        if (ox <= mouse_point.X && (mouse_point.X <= ox + w) &&
                            oy <= mouse_point.Y && (mouse_point.Y <= oy + h))
                        {
                            if (before_command_ui_focus_no != i)
                            {
                                SoundManager.PlaySound("システム／カーソル移動", 0.5);
                            }

                            command_ui_focus_no = i;
                            break;
                        }
                    }
                    break;
                case Mode.SubTopCommandShow:
                    for (var i = 0; i < command_ui_text_list.Count(); i++)
                    {
                        var ox = command_ui_position.X;// + command_ui_w * i;
                        var oy = command_ui_position.Y + command_ui_h * i;
                        var w = command_ui_w;
                        var h = command_ui_h;
                        if (ox <= mouse_point.X && (mouse_point.X <= ox + w) &&
                            oy <= mouse_point.Y && (mouse_point.Y <= oy + h))
                        {
                            if (before_command_ui_focus_no != i)
                            {
                                SoundManager.PlaySound("システム／カーソル移動", 0.5);
                            }

                            command_ui_focus_no = i;
                            break;
                        }
                    }
                    break;

            }
        }

        public void Draw()
        {
            var ox = command_ui_position.X;
            var oy = command_ui_position.Y;
            var w1 = command_ui_w;
            var h1 = command_ui_h;

            var color_base = DX.GetColor(200, 200, 200);
            var color_outline = DX.GetColor(60, 60, 60);
            var color_text = DX.GetColor(20, 20, 20);

            switch( mode )
            {
                case Mode.TurnTopCommandShow:
                    for( var i=0; i<command_ui_text_list.Count(); i++)
                    {
                        var focus = command_ui_focus_no == i;
                        Draw_UICommand(ox, oy + h1 * i, w1, h1, command_ui_text_list[i].view_name, color_base, color_outline, color_text, focus);
                    }
                    break;
                case Mode.SubTopCommandShow:
                    for (var i = 0; i < command_ui_text_list.Count(); i++)
                    {
                        var focus = command_ui_focus_no == i;
                        Draw_UICommand(ox, oy + h1 * i, w1, h1, command_ui_text_list[i].view_name, color_base, color_outline, color_text, focus);
                    }
                    break;
                case Mode.UnitStatusView:
                    Draw_UnitStatus();
                    break;
            }

            if ( mode!= Mode.UnitStatusView && status_target_unit != null)
            {
                Draw_SimpleUnitStatus();
            }

        }

        public void SetMode( Mode mode, Point mouse_position, SceneBattle.TurnOnwerUnit tou = null )
        {
            this.mode = mode;
            command_ui_position.X = mouse_position.X - 12;
            command_ui_position.Y = mouse_position.Y - 12;

            switch (mode)
            {
                case Mode.TurnTopCommandShow:
                    command_ui_text_list.Clear();
                    if (!tou.end_move ) {
                        command_ui_text_list.Add(new UserInterface.Command("移動","移動"));
                    }
                    if (!tou.end_action)
                    {
                        foreach (var a in tou.unit_manager_status.unit.bt.actions)
                        {
                            var ad = ActionDataManager.GetActionData(a.system_name);
                            command_ui_text_list.Add(new UserInterface.Command(ad.view_name, ad.system_name));
                        }
                    }
                    command_ui_text_list.Add(new UserInterface.Command("ステータス", "ステータス"));
                    command_ui_text_list.Add(new UserInterface.Command("待機", "待機"));
                    { // デバック用
                        var scene = GameMain.GetInstance().scene as SceneBattle;
                        if ( scene!=null)
                        {
                            if ( GameMain.GetInstance().debug_status.is_battle_ui_debug_mode )
                            {
                                command_ui_text_list.Add(new UserInterface.Command("AI", "AI"));
                                command_ui_text_list.Add(new UserInterface.Command("Stage Clear", "Stage Clear"));
                                command_ui_text_list.Add(new UserInterface.Command("Game Over", "Game Over"));
                            }
                        }
                    }
                    break;
                case Mode.SubTopCommandShow:
                    command_ui_text_list.Clear();
                    command_ui_text_list.Add(new UserInterface.Command("ステータス", "ステータス"));
                    break;
            }
        }

        void Draw_UICommand( int x, int y, int w, int h , string text, uint color_base, uint color_outline, uint color_text, bool focus)
        {
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, 200);
            DX.DrawBox( x, y, x + w, y + h, color_base, DX.TRUE);
            DX.DrawBox( x, y, x + w, y + h, color_outline, DX.FALSE);
            if (focus)
            {
                DX.SetDrawBlendMode(DX.DX_BLENDMODE_ADD, 128);
                DX.DrawBox(x, y, x + w, y + h, color_base, DX.TRUE);
            }
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);

            DX.DrawStringFToHandle(x + 5, y + 4, text, color_text, font_ui_text.GetHandle());
        }

        void Draw_SimpleUnitStatus()
        {
            var u = status_target_unit;
            var x = 180 + 8 * 2;
            var y = GameMain.WindowSizeH - 80;
            var text = "Lv10 " + u.name; ;
            var color_text2 = DX.GetColor(255, 255, 255);
            DX.DrawGraph(0, GameMain.WindowSizeH - 256, u.image_face, DX.TRUE);
            DX.DrawStringFToHandle(x + 5, y + 5, text, color_text2, font_handle_status_name.GetHandle());

            text = "HP:" + u.bt.status["HP"].now + "/" + u.bt.status["HP"].max;
            DX.DrawStringFToHandle(x + 5, y + 32, text, color_text2, font_handle_status_name.GetHandle());

            var w = 180;
            var w_hp = (int)(w * (double)u.bt.status["HP"].now / (double)u.bt.status["HP"].max);
            Draw_Sub_Bar(x + 5, y + 55, w, w_hp, DX.GetColor(0, 255, 255));
        }

        void Draw_UnitStatus()
        {
            if (status_target_unit == null) return; // 保険

            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, 200);
            DX.DrawBox(0, 0, GameMain.WindowSizeW, GameMain.WindowSizeH, DX.GetColor(255,255,255), DX.TRUE);
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);

            var u = status_target_unit;
            var x = 180 + 8 * 2 + 5;
            var y = 4;
            var text = "Lv10 " + u.name; ;
            var color_text2 = DX.GetColor(0, 0, 0);
            DX.DrawGraph(4, 4, u.image_face, DX.TRUE);

            DX.DrawStringFToHandle(x + 5, y + 5, text, color_text2, font_handle_status_detail.GetHandle());

            text = "HP:" + u.bt.status["HP"].now + "/" + u.bt.status["HP"].max;
            DX.DrawStringFToHandle(x + 5, y + 32, text, color_text2, font_handle_status_detail.GetHandle());

            var w = 180;
            var w_hp = (int)(w * (double)u.bt.status["HP"].now / (double)u.bt.status["HP"].max);
            Draw_Sub_Bar(x + 4, y + 55, w, w_hp, DX.GetColor(0, 255, 255));

            y += 80;
            var i = 0;
            foreach( var s in status_target_unit.bt.status )
            {
                if (s.Key == "HP") continue;
                text = s.Key + ":" + s.Value.now.ToString();
                DX.DrawStringFToHandle(x + 5, y + i * 27, text, color_text2, font_handle_status_detail.GetHandle());
                i++;
            }

        }

        void Draw_Sub_Bar( int x, int y, int w, int w_hp, UInt32 color)
        {
            var color_frame = DX.GetColor(0, 0, 0);
            var color_back = DX.GetColor(40, 40, 40);
            //var color = DX.GetColor(0, 255, 255);
            //var w = 180;
            //var w_hp = (int)(w * (double)u.bt.status["HP"].now / (double)u.bt.status["HP"].max);
            DX.DrawBox(x, y , x + w + 4, y + 20, color_frame, DX.FALSE);
            DX.DrawBox(x + 1, y + 1, x + w + 4 - 1, y + 20 - 1, color_frame, DX.FALSE);
            DX.DrawBox(x + 2, y + 2, x + w + 4 - 2, y + 20 - 2, color_back, DX.TRUE);
            DX.DrawBox(x + 2, y + 2, x + w_hp + 4 - 2, y + 20 - 2, color, DX.TRUE);
        }

        void Draw_HPBar( int x, int y, int w, int h, int w_hp, int color_main )
        {

        }

        public bool IsHitUI()
        {
            return command_ui_focus_no >= 0;
        }

        public Command GetHitUICommand()
        {
            if (command_ui_focus_no < 0) return null;
            return command_ui_text_list[command_ui_focus_no];
        }

        public void SetStatusUnit( Unit u)
        {
            status_target_unit = u;
        }

    }
}
