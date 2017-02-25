using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;

namespace TOL_SRPG.Base
{
    public class G3DCamera
    {
        //public enum Mode
        //{
        //    Normal,
        //}
        //Mode mode = Mode.Normal;

        // 最終値
        DX.VECTOR camera_pos;
        float camera_height;  // 高さ(y座標)
        DX.VECTOR target_pos; // 
        DX.VECTOR target_rot; // 対象を見る方向
        float distance;       // 対象までの距離

        // 途中経過用,
        DX.VECTOR now_camera_pos;
        DX.VECTOR now_target_pos;

        public G3DCamera()
        {
            camera_height = 100.0f;
            target_pos = DX.VGet( 45f, 0, 45f );
            target_rot = DX.VNorm( DX.VGet( -10, 0, -10 ) );

            distance = 100f;

            _UpdateCameraPos();
            _NotToFixHard(); // 初期値はゆるやかな変更をさせない
        }
        
        void _UpdateCameraPos()
        {
            var v = DX.VScale(target_rot, distance);
            var s = DX.VAdd(target_pos, v);
            s = DX.VAdd(s, DX.VGet(0, camera_height, 0));
            
            camera_pos = s;
        }
        
        public void _NotToFixHard()
        {
            for( int x=0; x<100; x++ )
            {
                _NowToFixFloatVector(ref now_camera_pos, ref camera_pos);
                _NowToFixFloatVector(ref now_target_pos, ref target_pos);
            }
        }

        // ゆるやかに現在座標を最終座標に変更する
        // 引数で effect の値を変えれたほうがいいかも...
        void _NowToFixFloatVector(ref DX.VECTOR now, ref DX.VECTOR fix)
        {
            if (now.x != fix.x ||
                now.y != fix.y ||
                now.z != fix.z
                )
            {
                var effect = 0.08f;
                var fix_to_min = 0.05f;
                now.x = now.x + ((fix.x - now.x) * effect);
                now.y = now.y + ((fix.y - now.y) * effect);
                now.z = now.z + ((fix.z - now.z) * effect);

                var v = 0.0f;
                v = fix.x - now.x;
                if (v < 0) v = -v;
                if (v < fix_to_min) now.x = fix.x;

                v = fix.y - now.y;
                if (v < 0) v = -v;
                if (v < fix_to_min) now.y = fix.y;

                v = fix.z - now.z;
                if (v < 0) v = -v;
                if (v < fix_to_min) now.z = fix.z;
            }
        }

        public void Update()
        {
            _NowToFixFloatVector(ref now_camera_pos, ref camera_pos);
            _NowToFixFloatVector(ref now_target_pos, ref target_pos);

            DX.SetCameraNearFar(1.0f, 350.0f);
            DX.SetCameraPositionAndTarget_UpVecY(
                now_camera_pos,
                now_target_pos);
        }

        public void AddRot( float rot )
        {
            var mr = DX.MGetRotY( rot );
            target_rot = DX.VTransform(target_rot, mr);
            _UpdateCameraPos();
        }

        public void MoveFront( float move_front )
        {
            var rev_rot = DX.VGet(-target_rot.x, target_rot.y, -target_rot.z); // カメラの方向と逆方向(y座標無視)
            var v = DX.VScale(rev_rot, move_front);
            target_pos = DX.VAdd(target_pos, v);

            _UpdateCameraPos();
        }

        // Yを軸とした方向を指定して移動量を決めて移動
        // かつ、方向の基準はカメラのローカル角
        public void MoveYRot(float move, float y_rot)
        {
            // たぶん、ここのしきおかしい 90度と-90度でしかテストしてないからわからないけど・・・
            var rev_rot = DX.VGet(-target_rot.x, target_rot.y, -target_rot.z); // カメラの方向と逆方向(y座標無視)
            //var v = DX.VScale(rev_rot, move);
            var y_rot2 = y_rot / 180.0 * Math.PI;
            var y_rot2_v = DX.VNorm(DX.VGet(0, (float)Math.Sin(y_rot2), 0)); // y_rotで指定されたベクトルを方向だけにして取得
            var v = DX.VCross( rev_rot, y_rot2_v);
            v = DX.VScale(v, move);

            target_pos = DX.VAdd(target_pos, v);

            _UpdateCameraPos();

        }

        public void SetDirectPosAndRot( DX.VECTOR pos, DX.VECTOR rot )
        {
            target_pos = pos;
            target_rot = rot;
            _UpdateCameraPos();
        }

        public string GetPositionString()
        {
            return string.Format("({0},{1},{2}) ({3},{4},{5})",target_pos.x, target_pos.y, target_pos.z,
                target_rot.x, target_rot.y, target_rot.z);
        }

    }
}
