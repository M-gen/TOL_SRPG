using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;
using System.Windows.Forms;
using System.Drawing;

namespace TOL_SRPG.App
{
    public class FormCreateMapToolWindow : Form
    {
        public FormCreateMapToolWindow()
        {
            this.Size = new Size(200,400);
            this.Show();
            this.Paint += FormCreateMapToolWindow_Paint;    

        }

        private void FormCreateMapToolWindow_Paint(object sender, PaintEventArgs e)
        {
        }
    }
    
    public class SceneCreateMap : Scene
    {

        FormCreateMapToolWindow tool_window;

        public SceneCreateMap()
        {
            var game_main = GameMain.GetInstance();

            game_main.g3d_map = new G3DMap(game_main.game_base);
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
        }
        }
}
