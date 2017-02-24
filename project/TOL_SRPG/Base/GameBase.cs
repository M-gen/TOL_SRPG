using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using DxLibDLL;

namespace DxlibGame
{
    public class GameMainBase
    {
        public virtual void Release()
        {
        }
    }

    public class GameBase : IDisposable
    {
        static GameBase game_base = null;

        public delegate void DlgMainLoopOneFrame();

        public GameMainBase game_main = null;
        public DlgMainLoopOneFrame dlg_main_loop_one_frame = null;
        WaitTimer wait_timer = new WaitTimer();
        List<long> frame_rate_calc_list = new List<long>(); // FPS計算用リスト
        bool is_fps_show;

        Form form;
        int window_size_w = 0;
        int window_size_h = 0;

        public Point mouse_point; // マウスの座標
        public Input input;

        public GameBase( Form form, int window_size_w, int window_size_h, DlgMainLoopOneFrame dlg_main_loop_one_frame = null, bool is_fps_show = false)
        {
            game_base = this;
            this.form = form;
            this.window_size_w = window_size_w;
            this.window_size_h = window_size_h;
            this.is_fps_show = is_fps_show;
            this.dlg_main_loop_one_frame = dlg_main_loop_one_frame;
            form.ClientSize = new Size(window_size_w, window_size_h);

            int sh = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
            int sw = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            form.Location = new Point( (sw - form.Size.Width) / 2, (sh - form.Size.Height) / 2);

            WaitTimer.InitClass();
            input = new Input(form);
            DX.SetUserWindow(form.Handle); //DxLibの親ウインドウをこのフォームウインドウにセット
            DX.DxLib_Init();
            DX.SetDrawMode(DX.DX_DRAWMODE_BILINEAR);
        }

        ~GameBase()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (game_main != null) game_main.Release();
            SFont.Release();
            WaitTimer.ReleaseClass();
            DX.DxLib_End();
        }

        public static GameBase GetInstance()
        {
            return game_base;
        }

        public void MainLoop()
        {
            //ループする関数
            //描画、FPS管理等ここで
            if (wait_timer.IsNextMainLoop())
            {
                //if (catan_2d_object != null) catan_2d_object.MouseMove(0, 0);

                UpdateFPSStateus(wait_timer.GetLastWaitTime());

                // 描画

                //DX.ClearDrawScreen();

                MainLoopOneFrame();

                //DX.DrawLine3D(DX.VGet(0.0f, 0.0f, 0.0f), DX.VGet(0.0f, 300.0f, 0.0f), DX.GetColor(255,0,0));

                //if (catan_2d_object != null) catan_2d_object.DrawMap(0, 0);

                //DX.ScreenFlip();
            }

        }

        private void MainLoopOneFrame()
        {
            // マウスの座標を取得
            // DxLibだと、ウィンドウを移動させるとなぜかマウス座標がズレるので
            mouse_point.X = System.Windows.Forms.Cursor.Position.X;
            mouse_point.Y = System.Windows.Forms.Cursor.Position.Y;
            mouse_point = form.PointToClient(mouse_point);

            input.Update();

            if (dlg_main_loop_one_frame != null) dlg_main_loop_one_frame();
        }

        private void UpdateFPSStateus(long now_wait_time)
        {
            frame_rate_calc_list.Add(now_wait_time);
            if (frame_rate_calc_list.Count > 5)
                frame_rate_calc_list.RemoveAt(0); // 
        }

        public void DrawGameBaseUI()
        {
            if (this.is_fps_show) DrawFPS();
        }

        private void DrawFPS()
        {
            // FPS計算 UpdateFPSStateusを毎フレーム呼び出していること
            double sum = 0.0f;
            foreach (long i in frame_rate_calc_list)
            {
                sum += i;
            }
            double fps = 1000.0f / (sum / frame_rate_calc_list.Count);

            DX.DrawString(4, 4, "FPS:" + fps.ToString("F3"), DX.GetColor(255, 255, 0), 1);
        }

        //static void ClearFontStyle()
        //{
        //    DX.SetFontSize(20);                             //サイズを20に変更
        //    DX.SetFontThickness(1);                         //太さを1に変更
        //    DX.ChangeFont("メイリオ");                      //
        //    DX.ChangeFontType(DX.DX_FONTTYPE_ANTIALIASING);//アンチエイリアス＆エッジ付きフォントに変更

        //    DX.DrawString(50, 300, "DXライブラリ入門！", DX.GetColor(255, 255, 255));

        //}
    }
}
