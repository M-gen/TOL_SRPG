using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;

namespace TOL_SRPG.Base
{
    public class S3DPoint
    {
        public double x = 0;
        public double y = 0;
        public double z = 0;

        public S3DPoint( double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public S3DPoint( DX.VECTOR vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }
        
        public DX.VECTOR GetDXV()
        {
            return DX.VGet((float)x, (float)y, (float)z);
        }
    }

    public class S3DLine
    {
        public S3DPoint p1;
        public S3DPoint p2;

        public S3DLine(S3DPoint p1, S3DPoint p2 )
        {
            this.p1 = p1;
            this.p2 = p2;
        }

        public static S3DLine GetMouseLay()
        {
            var game_base = GameBase.GetInstance();
            int mx, my;
            DX.VECTOR vp1, vp2;
            mx = game_base.input.mouse_sutatus.position.X;
            my = game_base.input.mouse_sutatus.position.Y;
            // マウスポインタがある画面上の座標に該当する３Ｄ空間上の Near 面の座標を取得
            vp1 = DX.ConvScreenPosToWorldPos(DX.VGet(mx, my, 0.0f));
            // マウスポインタがある画面上の座標に該当する３Ｄ空間上の Far 面の座標を取得
            vp2 = DX.ConvScreenPosToWorldPos(DX.VGet(mx, my, 1.0f));

            return new S3DLine( new S3DPoint( vp1.x, vp1.y, vp1.z), new S3DPoint(vp2.x, vp2.y, vp2.z));
        }

        public double GetRange()
        {
            var x = (p1.x - p1.x);
            var y = (p1.y - p1.y);
            var z = (p1.z - p1.z);
            return Math.Sqrt(x * x + y * y + z * z);

        }
    }

    // 長方形の図形を描画する
    // 単純な方向にしか展開できないので注意（天、地、上下左右）
    public class S3DPanel
    {
        S3DPoint pos;
        SDPoint size;
        int image_handle = -1;

        // 向いている方向
        public enum Direction
        {
            Top,
            Wall_NS, // 北,上,南,下
            Wall_EW, // 東,右,西,左 
        }
        Direction direction = Direction.Top;

        DX.COLOR_U8 dif_color;
        DX.COLOR_U8 spc_color;

        SDPoint uv_pos = new SDPoint(0, 0);
        SDPoint uv_size = new SDPoint(32, 32);

        public S3DPanel(S3DPoint pos, SDPoint size, Direction direction = Direction.Top)
        {
            this.pos = pos;
            this.size = size;
            this.direction = direction;

            dif_color = DX.GetColorU8(255,0,0,0);
            spc_color = DX.GetColorU8(0,0,0,255);
        }

        public void SetColor( int a, int r, int g, int b )
        {
            dif_color = DX.GetColorU8(r, g, b, a);
        }

        public void SetSpecularColor(int a, int r, int g, int b)
        {
            spc_color = DX.GetColorU8(r, g, b, a);
        }

        public void SetTexture( int image_handle )
        {
            this.image_handle = image_handle;
        }

        public void Draw()
        {
            if (image_handle == -1)
            {
                DrawSprite(DX.DX_NONE_GRAPH, 0, 0, 32, 32, 32);
            }
            else
            {
                DrawSprite(image_handle, (int)uv_pos.x, (int)uv_pos.y, (int)uv_size.x, (int)uv_size.y, 32);
            }
        }

        void GetDXVectors( out DX.VERTEX3D[] vectors )
        {
            var x = (float)(pos.x);
            var y = (float)(pos.y);
            var z = (float)(pos.z);
            var w = (float)(size.x);
            var h = (float)(size.y);
            vectors = new DX.VERTEX3D[6];
            switch (direction)
            {
                case Direction.Top:
                    x -= (float)(size.x / 2.0);
                    z -= (float)(size.y / 2.0);
                    vectors[0].pos = DX.VGet(x, y, z + h);
                    vectors[1].pos = DX.VGet(x + w, y, z + h);
                    vectors[2].pos = DX.VGet(x + w, y, z);
                    vectors[3].pos = DX.VGet(x, y, z + h);
                    vectors[4].pos = DX.VGet(x, y, z);
                    vectors[5].pos = DX.VGet(x + w, y, z);
                    break;
                case Direction.Wall_NS:
                    x -= (float)(size.x / 2.0);
                    y -= (float)(size.y / 2.0);
                    vectors[0].pos = DX.VGet(x, y + h, z);
                    vectors[1].pos = DX.VGet(x + w, y + h, z);
                    vectors[2].pos = DX.VGet(x + w, y, z);
                    vectors[3].pos = DX.VGet(x, y + h, z);
                    vectors[4].pos = DX.VGet(x, y, z);
                    vectors[5].pos = DX.VGet(x + w, y, z);
                    break;
                case Direction.Wall_EW:
                    y -= (float)(size.y / 2.0);
                    z -= (float)(size.x / 2.0);
                    vectors[0].pos = DX.VGet(x, y + h, z);
                    vectors[1].pos = DX.VGet(x, y + h, z + w);
                    vectors[2].pos = DX.VGet(x, y, z + w);
                    vectors[3].pos = DX.VGet(x, y + h, z);
                    vectors[4].pos = DX.VGet(x, y, z);
                    vectors[5].pos = DX.VGet(x, y, z + w);
                    break;
            }

        }

        //void DrawSprite(float x, float y, float z, float w, float h, int image, int uv_x, int uv_y, int uv_w, int uv_h, int image_wh_size)
        void DrawSprite( int image, int uv_x, int uv_y, int uv_w, int uv_h, int image_wh_size)
        {
            DX.VERTEX3D[] v6;
            GetDXVectors(out v6);
            SetupV4(ref v6, uv_x, uv_y, uv_w, uv_h, image_wh_size);

            DX.DrawPolygon3D(out v6[0], 2, image, DX.FALSE);
        }

        void SetupV4(ref DX.VERTEX3D[] v4, int uv_x, int uv_y, int uv_w, int uv_h, int image_wh_size)
        {
            v4[0].norm = DX.VGet(0.0f, 1.0f, 0.0f);
            v4[1].norm = DX.VGet(0.0f, 1.0f, 0.0f);
            v4[2].norm = DX.VGet(0.0f, 1.0f, 0.0f);
            v4[3].norm = DX.VGet(0.0f, 1.0f, 0.0f);
            v4[4].norm = DX.VGet(0.0f, 1.0f, 0.0f);
            v4[5].norm = DX.VGet(0.0f, 1.0f, 0.0f);
            v4[0].dif = dif_color;
            v4[1].dif = dif_color;
            v4[2].dif = dif_color;
            v4[3].dif = dif_color;
            v4[4].dif = dif_color;
            v4[5].dif = dif_color;
            v4[0].spc = spc_color;
            v4[1].spc = spc_color;
            v4[2].spc = spc_color;
            v4[3].spc = spc_color;
            v4[4].spc = spc_color;
            v4[5].spc = spc_color;

            float u0 = (float)uv_x / (float)image_wh_size;
            float v0 = (float)uv_y / (float)image_wh_size;
            float u1 = (float)(uv_x + uv_w) / (float)image_wh_size;
            float v1 = (float)(uv_y + uv_h) / (float)image_wh_size;

            v4[0].u = u0; v4[0].v = v0;
            v4[1].u = u1; v4[1].v = v0;
            v4[2].u = u1; v4[2].v = v1;
            v4[3].u = u0; v4[3].v = v0;
            v4[4].u = u0; v4[4].v = v1;
            v4[5].u = u1; v4[5].v = v1;
        }

        public HitStatus CheckHit( S3DLine line )
        {
            var res = new HitStatus();
            var p1 = line.p1.GetDXV();
            var p2 = line.p2.GetDXV();

            DX.VERTEX3D[] v6;
            GetDXVectors(out v6);

            // 三角ポリゴン2つで矩形と線分の判定の衝突判定をする
            var l1 = DX.HitCheck_Line_Triangle(p1, p2,
                DX.VGet(v6[0].pos.x, v6[0].pos.y, v6[0].pos.z),
                DX.VGet(v6[1].pos.x, v6[1].pos.y, v6[1].pos.z),
                DX.VGet(v6[2].pos.x, v6[2].pos.y, v6[2].pos.z));
            var l2 = DX.HitCheck_Line_Triangle(p1, p2,
                DX.VGet(v6[3].pos.x, v6[3].pos.y, v6[3].pos.z),
                DX.VGet(v6[4].pos.x, v6[4].pos.y, v6[4].pos.z),
                DX.VGet(v6[5].pos.x, v6[5].pos.y, v6[5].pos.z));

            if ( l1.HitFlag==DX.TRUE )
            {
                res.is_hit = true;
                res.point = new S3DPoint(l1.Position);
                res.range = (new S3DLine(line.p1, res.point)).GetRange();
            }
            else if (l2.HitFlag == DX.TRUE)
            {
                res.is_hit = true;
                res.point = new S3DPoint(l2.Position);
                res.range = (new S3DLine(line.p2, res.point)).GetRange();
            }

            return res;
        }

        public void SetPos( S3DPoint pos )
        {
            this.pos = pos;
        }

        public void SetSize(SDPoint size)
        {
            this.size = size;
        }

        public void SetUV( SDPoint pos, SDPoint size )
        {
            uv_pos = pos;
            uv_size = size;
        }
    }

    // 当たり判定の結果
    public class HitStatus
    {
        public bool is_hit = false;     // 当たり判定の結果
        public double range = 0;        // 距離
        public S3DPoint point;
    }

    // 3Dのキューブクラス
    // S3DPanelをもとにしているので回転はできない
    public class S3DCube
    {
        S3DPoint pos;
        S3DPoint size;

        S3DPanel[] panels = new S3DPanel[6];

        public S3DCube(S3DPoint pos, S3DPoint size)
        {
            this.pos = pos;
            this.size = size;

            panels[0] = new S3DPanel(new S3DPoint(pos.x, pos.y + size.y / 2.0, pos.z), new SDPoint(size.x, size.z), S3DPanel.Direction.Top);
            panels[1] = new S3DPanel(new S3DPoint(pos.x, pos.y - size.y / 2.0, pos.z), new SDPoint(size.x, size.z), S3DPanel.Direction.Top);
            panels[2] = new S3DPanel(new S3DPoint(pos.x, pos.y, pos.z - size.z / 2.0), new SDPoint(size.x, size.y), S3DPanel.Direction.Wall_NS);
            panels[3] = new S3DPanel(new S3DPoint(pos.x, pos.y, pos.z + size.z / 2.0), new SDPoint(size.x, size.y), S3DPanel.Direction.Wall_NS);
            panels[4] = new S3DPanel(new S3DPoint(pos.x - size.x / 2.0, pos.y, pos.z), new SDPoint(size.z, size.y), S3DPanel.Direction.Wall_EW);
            panels[5] = new S3DPanel(new S3DPoint(pos.x + size.x / 2.0, pos.y, pos.z), new SDPoint(size.z, size.y), S3DPanel.Direction.Wall_EW);
        }

        void UpdatePos()
        {
            panels[0].SetPos(new S3DPoint(pos.x, pos.y + size.y / 2.0, pos.z));
            panels[1].SetPos(new S3DPoint(pos.x, pos.y - size.y / 2.0, pos.z));
            panels[2].SetPos(new S3DPoint(pos.x, pos.y, pos.z - size.z / 2.0));
            panels[3].SetPos(new S3DPoint(pos.x, pos.y, pos.z + size.z / 2.0));
            panels[4].SetPos(new S3DPoint(pos.x - size.x / 2.0, pos.y, pos.z));
            panels[5].SetPos(new S3DPoint(pos.x + size.x / 2.0, pos.y, pos.z));
        }

        void UpdateSize()
        {
            panels[0].SetSize(new SDPoint(size.x, size.z));
            panels[1].SetSize(new SDPoint(size.x, size.z));
            panels[2].SetSize(new SDPoint(size.x, size.y));
            panels[3].SetSize(new SDPoint(size.x, size.y));
            panels[4].SetSize(new SDPoint(size.z, size.y));
            panels[5].SetSize(new SDPoint(size.z, size.y));
        }

        public void Draw()
        {
            foreach (var p in panels) p.Draw();
        }

        public void SetColor(int a, int r, int g, int b)
        {
            //var dif_color = DX.GetColorU8(r, g, b, a);
            foreach (var p in panels)
            {
                p.SetColor(a, r, g, b);
            }
        }

        public HitStatus CheckHit(S3DLine line)
        {
            var res = new HitStatus();
            var res_list = new List<HitStatus>();

            foreach (var p in panels)
            {
                var tmp_res = p.CheckHit(line);
                if (tmp_res.is_hit) res_list.Add(tmp_res);
            }

            var min_range = -1.0;
            foreach (var r in res_list)
            {
                res.is_hit = true;
                if ( min_range==-1 || min_range<r.range )
                {
                    res.point = r.point;
                    res.range = r.range;
                }
            }

            return res;
        }

        public void SetPos(S3DPoint pos)
        {
            this.pos = pos;
            UpdatePos();
        }

        public void SetSize(S3DPoint size)
        {
            this.size = size;
            UpdateSize();
        }
    }
}
