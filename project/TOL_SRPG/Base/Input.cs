using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using DxLibDLL;
using System.Windows.Forms;


// 入力制御用クラス

namespace TOL_SRPG.Base
{
    public class Input
    {
        public enum MouseButtonKeyStatus
        {
            Free,
            OneCrick,
            DoubleCrick,
            DragStart,  // ドラッグ開始
            DragStay,   // ドラッグ中、移動していない
            DragMove,   // ドラッグ中、移動している
            DragEnd,    // ドラッグ終了
        }

        public class MouseButtonStatus
        {
            public MouseButtonKeyStatus key_status = MouseButtonKeyStatus.Free;
            public Point drag_start;
            public Point drag_end;

            // いったん安直に作る
            // ダブルクリックかワンクリック、ドラッグかなどの判定用
            public bool is_key_down = false;
            //public int key_down_time;
            //public int double_click_time; // ダブルクリックとして扱う

        }

        public class MouseStatus
        {
            public Point position = new Point();
            public MouseButtonStatus left  = new MouseButtonStatus();
            public MouseButtonStatus right = new MouseButtonStatus();
        }

        public enum KeyInputStatus
        {
            Free,
            Down,       // 押下時
            DownStay,   // 押下中
            Up,         // 離したとき
        }

        public class KeySetup
        {
            public int key_code;
            public int key_code_dx_lib;
            public KeyInputStatus key_status;

        }

        
        Form mouse_target_form;
        public MouseStatus mouse_sutatus = new MouseStatus();
        Dictionary<int, KeySetup> key_status = new Dictionary<int, KeySetup>();

        public Input( Form mouse_target_form )
        {
            this.mouse_target_form = mouse_target_form;
        }

        public void Update()
        {
            // マウスの座標を取得
            // DxLibだと、ウィンドウを移動させるとなぜかマウス座標がズレるので
            mouse_sutatus.position.X = System.Windows.Forms.Cursor.Position.X;
            mouse_sutatus.position.Y = System.Windows.Forms.Cursor.Position.Y;
            mouse_sutatus.position = mouse_target_form.PointToClient(mouse_sutatus.position);

            for (int i = 0; i < 2; i++)
            {
                MouseButtonStatus mbs = mouse_sutatus.left;
                bool is_key_on = false;
                if (i == 1) {
                    mbs = mouse_sutatus.right;
                    is_key_on = (Control.MouseButtons & MouseButtons.Right) == MouseButtons.Right;
                }
                else
                {
                    is_key_on = (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;
                }

                switch (mbs.key_status)
                {
                    case MouseButtonKeyStatus.Free:
                        if (mbs.is_key_down)
                        {
                            if ( !is_key_on ) // 前回まで On 今回 Of = 離した = クリックした とする
                            {
                                // とりあえず、単純なワンクリックのみ対応
                                mbs.key_status = MouseButtonKeyStatus.OneCrick;
                            }
                        }
                        break;
                    case MouseButtonKeyStatus.OneCrick:
                        mbs.key_status = MouseButtonKeyStatus.Free; // ワンクリック状態は継続しないのでFreeに戻しておく
                        break;
                }
                mbs.is_key_down = is_key_on;
            }

            // 監視している各キーの状態を前回の状態を基準に更新する
            foreach ( var v0 in key_status)
            {
                var v = v0.Value;
                switch( v.key_status )
                {
                    case KeyInputStatus.Free:
                        if (DX.CheckHitKey(v.key_code_dx_lib) == DX.TRUE)
                        {
                            v.key_status = KeyInputStatus.Down;
                        }
                        break;
                    case KeyInputStatus.Down:
                        if (DX.CheckHitKey(v.key_code_dx_lib) == DX.TRUE)
                        {
                            v.key_status = KeyInputStatus.DownStay;
                        }
                        else
                        {
                            v.key_status = KeyInputStatus.Up;
                        }
                        break;
                    case KeyInputStatus.DownStay:
                        if (DX.CheckHitKey(v.key_code_dx_lib) != DX.TRUE)
                        {
                            v.key_status = KeyInputStatus.Up;
                        }
                        break;
                    case KeyInputStatus.Up:
                        if (DX.CheckHitKey(v.key_code_dx_lib) == DX.TRUE)
                        {
                            v.key_status = KeyInputStatus.Down;
                        }
                        else
                        {
                            v.key_status = KeyInputStatus.Free;
                        }
                        break;
                }

            }
        }

        // 監視するキーの登録
        public void SetupKeyInput( int key_code, int key_code_dx_lib )
        {
            var v = new KeySetup();
            v.key_code = key_code;
            v.key_code_dx_lib = key_code_dx_lib;
            v.key_status = KeyInputStatus.Free;
            key_status.Add(key_code, v);
        }

        public KeyInputStatus GetKeyInputStatus( int code)
        {
            var ks = key_status[code];
            return ks.key_status;
        }

    }
}
