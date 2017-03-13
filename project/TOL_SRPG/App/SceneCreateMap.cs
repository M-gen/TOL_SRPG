using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;
using TOL_SRPG.App.Map;
using System.Windows.Forms;
using System.Drawing;

namespace TOL_SRPG.App
{
    public class KeyButton : Button
    {
        public delegate void DelgateSetKeyWord(string key_word);

        string key_word;
        DelgateSetKeyWord action;

        public KeyButton( int x, int y, int w, int h, string key_word, DelgateSetKeyWord action)
        {
            this.Location = new Point(x, y);
            this.Size = new Size(w, h);
            this.key_word = key_word;
            this.action = action;

            this.Click += KeyButton_Click;
        }

        private void KeyButton_Click(object sender, EventArgs e)
        {
            this.action(key_word);
        }
    }

    public class ButtonBox
    {
        List<Button> buttons = new List<Button>();
        Form form;
        KeyButton.DelgateSetKeyWord action;

        Size button_size;
        int w_line_num = 0; // 横に並べられる数

        public ButtonBox( Form form, Size button_size, KeyButton.DelgateSetKeyWord action)
        {
            this.form = form;
            this.button_size = button_size;
            this.action = action;

            w_line_num = form.Width / button_size.Width;

        }

        public void Add(string key_word, string image_path = "")
        {
            var index = buttons.Count;
            var x = (index% w_line_num) * button_size.Width;
            var y = (index/ w_line_num) * button_size.Height;

            var button = new KeyButton(x, y, button_size.Width, button_size.Height, key_word, action);
            form.Controls.Add(button);
            buttons.Add(button);

            if (image_path!="")
            {
                button.BackgroundImage = Image.FromFile(image_path);// image_path;
            }

        }
    }

    public class FormCreateMapToolWindow : Form
    {
        //List<Button> buttons = new List<Button>();
        ButtonBox button_box;

        public string select_tool_key_word = "";

        public FormCreateMapToolWindow()
        {
            //this.Size = new Size(200,400);
            this.ClientSize = new Size(32*5, 400);
            this.Show();
            this.Paint += FormCreateMapToolWindow_Paint;

            var game_main = GameMain.GetInstance();
            var battle_map = game_main.g3d_map;

            button_box = new ButtonBox(this, new Size(32,32), SetKeyWord);

            var i = 0;
            foreach (var m in battle_map.material_manager.ground_materials)
            {
                var key_word = "ground " + m.Value.key;
                var image_path = m.Value.image_path;

                if (m.Value.key == "") continue;
                if (i == 0) select_tool_key_word = key_word;

                button_box.Add(key_word, image_path);
                i++;
            }

            foreach (var m in battle_map.material_manager.wall_materials)
            {
                var key_word = "wall " + m.Value.key;
                var image_path = m.Value.image_path;

                if (m.Value.key == "") continue;

                button_box.Add(key_word, image_path);
            }
        }

        private void FormCreateMapToolWindow_Paint(object sender, PaintEventArgs e)
        {
        }

        private void SetKeyWord(string key_word)
        {
            select_tool_key_word = key_word;
        }
    }
    
    public class SceneCreateMap : Scene
    {

        FormCreateMapToolWindow tool_window;

        public SceneCreateMap()
        {
            var game_main = GameMain.GetInstance();

            game_main.g3d_map = new BattleMap(game_main.game_base);
            game_main.g3d_camera = new G3DCamera();

            tool_window = new FormCreateMapToolWindow();

        }

        public override void Update()
        {
            _Update_KeyInput();

            var game_main = GameMain.GetInstance();
            game_main.g3d_map.UpdateInterface();
            game_main.action_manager.Update();
            game_main.unit_manager.Update();
            game_main.g3d_camera.Update();
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

        public override void Dispose()
        {
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

            if (game_base.input.mouse_sutatus.left.key_status == Input.MouseButtonKeyStatus.OneCrick)
            {
                if (g3d_map.map_cursor_x >= 0 && g3d_map.map_cursor_y >= 0) // 3Dマップへの入力
                {
                    var keys = tool_window.select_tool_key_word.Split(' ');

                    if (keys[0] == "ground")
                    {
                        g3d_map.SetMapGroundMaterial(g3d_map.map_cursor_x, g3d_map.map_cursor_y, keys[1]);
                    }
                }

                if (g3d_map.map_cursor_wall!=null)
                {
                    var keys = tool_window.select_tool_key_word.Split(' ');
                    if (keys[0] == "wall")
                    {
                        g3d_map.SetMapWallMaterial(g3d_map.map_cursor_wall, keys[1]);
                    }
                }
            }
        }
    }
}
