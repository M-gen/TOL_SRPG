﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;

using DxLibDLL;
using TOL_SRPG.Base;

namespace TOL_SRPG.App.Map
{
    public class BattleMap
    {
        public const double SQUARE_SIZE = 10.0;
        public const float HIGHT_ONE_VALUE = 2.5f;

        GameBase game_base;
        public MapMaterialManager material_manager = new MapMaterialManager();

        public int map_w = 12;
        public int map_h = 10;
        int[] layer_0_ground = {
            2,2,2,2,1,1,1,1,1,1,1,1, // 0
            2,2,2,2,1,1,1,1,1,1,1,1,
            2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2, // 4
            2,2,2,2,2,2,2,2,2,2,2,2,
            3,3,2,2,2,2,2,0,0,2,2,2,
            3,3,2,2,4,2,2,2,6,2,2,2,
            2,2,2,4,4,2,2,2,6,8,2,2, // 8
            2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,
        };

        int map_image_chips_wall   = 0; // 壁面
        public Square[] map_squares;
        public RangeAreaMove         move_area;
        public RouteAsRangeArea      route_as_move_area = null;
        public RangeAreaActionTarget action_target_area;
        public RouteAsShoot route_as_shoot = null;

        DX.COLOR_U8 dif_color = DX.GetColorU8(255, 255, 255, 255);
        public int map_cursor_x = 3;
        public int map_cursor_y = 3;

        G3DModel model_cursor_turn_owner;
        Unit cursor_turn_owner_unit = null;
        public bool is_draw_cursor_turn_owner = false;
        float cursor_turn_owner_offset_y_timer = 0;


        public BattleMap(GameBase game_base)
        {
            this.game_base = game_base;

            material_manager.AddGroundMaterial("", "data/image/map/ground_01_01.png"); // ダミー
            material_manager.AddGroundMaterial("平原_01", "data/image/map/ground_01_01.png");
            material_manager.AddGroundMaterial("土_01", "data/image/map/ground_02_01.png");
            material_manager.AddGroundMaterial("石_01", "data/image/map/ground_03_01.png");
            material_manager.AddGroundMaterial("白レンガ_01", "data/image/map/ground_04_01.png");
            material_manager.AddGroundMaterial("水_01", "data/image/map/ground_05_01.png");

            material_manager.AddWallMaterial("", "data/image/map/wall_01_01.png"); // ダミー
            material_manager.AddWallMaterial("土_01", "data/image/map/wall_01_01.png");
            material_manager.AddWallMaterial("土_02", "data/image/map/wall_01_02.png");
            material_manager.AddWallMaterial("白レンガ_01", "data/image/map/wall_02_01.png");

            map_image_chips_wall = DX.LoadGraph("data/image/map/wall_01_01.png");

            model_cursor_turn_owner = new G3DModel("data/model/action_items/cursor_turn_owner.pmd");
            
            Setup(layer_0_ground, "土_01");

            SetMapGroundMaterial(1, 1, "土_01");
            SetMapGroundMaterial(1, 2, "土_01");
            SetMapGroundMaterial(2, 1, "土_01");
            SetMapGroundMaterial(2, 2, "土_01");
            SetMapGroundMaterial(3, 1, "石_01");
            SetMapGroundMaterial(3, 2, "石_01");
            SetMapGroundMaterial(4, 1, "白レンガ_01");
            SetMapGroundMaterial(4, 2, "白レンガ_01");
            SetMapGroundMaterial(4, 3, "白レンガ_01");
            SetMapGroundMaterial(5, 3, "水_01");
            SetMapGroundMaterial(6, 3, "水_01");
            SetMapGroundMaterial(7, 3, "水_01");
        }

        public void Setup( int[] layer_0_ground, string wall_default_material_key = "")
        {
            map_squares = new Square[map_w * map_h];
            for (int i = 0; i < map_w * map_h; i++)
            {
                var map_x = i % map_w;
                var map_y = i / map_w;

                map_squares[i] = new Square(map_x, map_y, layer_0_ground[i]);
            }

            move_area = new RangeAreaMove(map_w, map_h);
            action_target_area = new RangeAreaActionTarget(map_w, map_h);

            UpdateLayer();

            for (int i = 0; i < map_w * map_h; i++)
            {
                map_squares[i].Setup(material_manager, wall_default_material_key);
            }
        }

        public void UpdateLayer()
        {
            for (int i = 0; i < map_w * map_h; i++)
            {
                var x = i % map_w;
                var y = i / map_w;
                var sq = map_squares[i];
                //map_squares[i] = new Square();
                //map_squares[i].height = layer_0_ground[i];
                sq.under[0] = 0; // 初期化
                sq.under[1] = 0;
                sq.under[2] = 0;
                sq.under[3] = 0;

                // 壁面の高さを算出
                var h = sq.height;
                for ( int j=0; j<4; j++)
                {
                    var h2 = 0;
                    switch(j)
                    {
                        case 0: h2 = GetHeight(x, y - 1); break;
                        case 1: h2 = GetHeight(x+1,y); break;
                        case 2: h2 = GetHeight(x, y + 1); break;
                        case 3: h2 = GetHeight(x-1, y); break;
                    }
                    var hd = h - h2;              // 差分
                    if (h2 == -1) hd = h - 0;     // -1:マップはし
                    if (hd > 0) sq.under[j] = hd; // 確定

                    //Console.WriteLine( "s " + i.ToString() + " " + hd + " " + h + " " + h2 );
                }

            }
        }

        public int GetHeight( int x, int y )
        {
            if (x < 0) return -1;
            if (x >= map_w) return -1;
            if (y < 0) return -1;
            if (y >= map_h) return -1;
            var h = map_squares[x+y*map_w].height;
            return h;
        }

        public void UpdateInterface()
        {
            // カーソル状態更新
            int mx, my;
            float distance_now = -1f;
            float distance_tmp = -1f;
            DX.VECTOR StartPos, EndPos;
            //DX.GetMousePoint(out mx, out my);
            mx = game_base.input.mouse_sutatus.position.X;
            my = game_base.input.mouse_sutatus.position.Y;
            // マウスポインタがある画面上の座標に該当する３Ｄ空間上の Near 面の座標を取得
            StartPos = DX.ConvScreenPosToWorldPos(DX.VGet(mx, my, 0.0f));
            // マウスポインタがある画面上の座標に該当する３Ｄ空間上の Far 面の座標を取得
            EndPos = DX.ConvScreenPosToWorldPos(DX.VGet(mx, my, 1.0f));

            DX.HITRESULT_LINE l1, l2;
            map_cursor_x = -1;
            map_cursor_y = -1;
            var mouse_lay = S3DLine.GetMouseLay();
            HitStatus ground_hit_status = null;
            for (int x = 0; x < map_w; x++)
            {
                for (int y = 0; y < map_h; y++)
                {
                    var sq = map_squares[x + y * map_w];
                    var hs = sq.CheckHitGround(mouse_lay);

                    if ( (ground_hit_status==null || ground_hit_status.range > hs.range) && (hs.is_hit) )
                    {
                        ground_hit_status = hs;
                        map_cursor_x = x;
                        map_cursor_y = y;
                    }
                }
            }

            // ユニットのカーソル判定（こっちがあるなら上書きする）
            {
                var game_main = GameMain.GetInstance();
                //var line = S3DLine.GetMouseLay();
                var range = 0.0;
                Unit tmp_u = null;
                foreach (var u in game_main.unit_manager.units)
                {
                    var hs = u.unit.CheckHit(mouse_lay);
                    if (hs.is_hit)
                    {
                        if (tmp_u == null || range > hs.range)
                        {
                            tmp_u = u.unit;
                            range = hs.range;
                        }
                    }
                }
                if (tmp_u != null)
                {
                    map_cursor_x = tmp_u.map_x;
                    map_cursor_y = tmp_u.map_y;
                }
            }

            UpdateInterface_Cursors();
        }

        // 3Dカーソルなどの更新
        void UpdateInterface_Cursors()
        {   
            if (cursor_turn_owner_unit!=null)
            {
                if (cursor_turn_owner_unit.bt.is_alive)
                {
                    var u = cursor_turn_owner_unit;
                    var y_offset = (float)Math.Sin(cursor_turn_owner_offset_y_timer);
                    float x = 0.0f, y = 0.0f, z = 0.0f;
                    Get3DPos(u.map_x, u.map_y, ref x, ref y, ref z);
                    y += 20.0f + y_offset;
                    if (is_draw_cursor_turn_owner)
                    {
                        model_cursor_turn_owner.Pos(x, y, z);
                    }

                    cursor_turn_owner_offset_y_timer += 0.1f;
                    var pi2 = 2.0f * Math.PI;
                    if (cursor_turn_owner_offset_y_timer >= pi2) cursor_turn_owner_offset_y_timer -= (float)(pi2);
                }
                else
                {
                    cursor_turn_owner_unit = null;
                }
            }

        }

        public void Draw()
        {
            // 描画
            for( int x=0; x<map_w; x++ )
            {
                for( int y=0; y<map_h; y++)
                {
                    var p = x + y * map_w;
                    var sq = map_squares[p];
                    var height = sq.height;

                    // 頂上のスクウェアを描画
                    sq.Draw();

                    // カーソル描画
                    if (x == map_cursor_x && y == map_cursor_y)
                    {
                        dif_color = DX.GetColorU8(255, 0, 0, 128);
                        DrawSpriteXZ(x * 10.0f, height * HIGHT_ONE_VALUE + 0.01f, y * 10.0f, 10, 10, DX.DX_NONE_GRAPH, 0, 0, 32, 32, 32);
                        dif_color = DX.GetColorU8(255, 255, 255, 255);
                    }
                    if (move_area.squares[p].is_ok)
                    {
                        // 移動範囲
                        dif_color = DX.GetColorU8(0, move_area.squares[p].step_count * 20, 255, 128);
                        DrawSpriteXZ(x * 10.0f, height * HIGHT_ONE_VALUE + 0.01f, y * 10.0f, 10, 10, DX.DX_NONE_GRAPH, 0, 0, 32, 32, 32);
                        dif_color = DX.GetColorU8(255, 255, 255, 255);
                    }
                    if (route_as_move_area!=null && route_as_move_area.squares[p].step_count >= 0)
                    {
                        // 移動経路
                        dif_color = DX.GetColorU8(255, 255, 255, 255);
                        DX.SetDrawBlendMode(DX.DX_BLENDMODE_ADD, 128);
                        DrawSpriteXZ(x * 10.0f, height * HIGHT_ONE_VALUE + 0.01f, y * 10.0f, 10, 10, DX.DX_NONE_GRAPH, 0, 0, 32, 32, 32);
                        dif_color = DX.GetColorU8(255, 255, 255, 255);
                        DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);
                    }
                    if (action_target_area.squares[p].is_pass) // is_okはユニットがいるマス、is_passは純粋な射程範囲
                    {
                        // 行動の対象選択範囲
                        dif_color = DX.GetColorU8(255, 0, 0, 128);
                        DrawSpriteXZ(x * 10.0f, height * HIGHT_ONE_VALUE + 0.01f, y * 10.0f, 10, 10, DX.DX_NONE_GRAPH, 0, 0, 32, 32, 32);
                        dif_color = DX.GetColorU8(255, 255, 255, 255);
                    }

                }
            }

            // 手番ユニットのカーソル表示
            if (cursor_turn_owner_unit!=null && is_draw_cursor_turn_owner)
            {
                model_cursor_turn_owner.Draw();
            }

        }

        void DrawSpriteXZ( float x, float y, float z, float w, float h, int image, int uv_x, int uv_y, int uv_w, int uv_h, int image_wh_size  )
        {
            var v4 = new DX.VERTEX3D[6];
            //var w = 10;
            v4[0].pos = DX.VGet(x, y, z + w);
            v4[1].pos = DX.VGet(x + w, y, z + w);
            v4[2].pos = DX.VGet(x + w, y, z);
            v4[3].pos = DX.VGet(x, y, z + w);
            v4[4].pos = DX.VGet(x, y, z);
            v4[5].pos = DX.VGet(x + w, y, z);
            SetupV4(ref v4, uv_x, uv_y, uv_w, uv_h, image_wh_size);

            DX.DrawPolygon3D(out v4[0], 2, image, DX.TRUE);
        }

        // dir 0:下 1:右 2:上 3:左 (反時計回り)
        void DrawSpriteWall( int dir, float x, float y, float z, float w, float h, int image, int uv_x, int uv_y, int uv_w, int uv_h, int image_wh_size)
        {
            var v4 = new DX.VERTEX3D[6];
            //var w = 10;
            switch (dir) {
                default:
                    v4[0].pos = DX.VGet(x, y + h, z);
                    v4[1].pos = DX.VGet(x + w, y + h, z);
                    v4[2].pos = DX.VGet(x + w, y, z);
                    v4[3].pos = DX.VGet(x, y + h, z);
                    v4[4].pos = DX.VGet(x, y, z);
                    v4[5].pos = DX.VGet(x + w, y, z);
                    break;
                case 1:
                    v4[0].pos = DX.VGet(x, y + h, z);
                    v4[1].pos = DX.VGet(x, y + h, z + w);
                    v4[2].pos = DX.VGet(x, y, z + w);
                    v4[3].pos = DX.VGet(x, y + h, z);
                    v4[4].pos = DX.VGet(x, y, z);
                    v4[5].pos = DX.VGet(x, y, z + w);
                    break;
                case 2:
                    v4[0].pos = DX.VGet(x, y + h, z);
                    v4[1].pos = DX.VGet(x - w, y + h, z);
                    v4[2].pos = DX.VGet(x - w, y, z);
                    v4[3].pos = DX.VGet(x, y + h, z);
                    v4[4].pos = DX.VGet(x, y, z);
                    v4[5].pos = DX.VGet(x - w, y, z);
                    break;
                case 3:
                    v4[0].pos = DX.VGet(x, y + h, z);
                    v4[1].pos = DX.VGet(x, y + h, z - w);
                    v4[2].pos = DX.VGet(x, y, z - w);
                    v4[3].pos = DX.VGet(x, y + h, z);
                    v4[4].pos = DX.VGet(x, y, z);
                    v4[5].pos = DX.VGet(x, y, z - w);
                    break;
            }
            SetupV4(ref v4, uv_x, uv_y, uv_w, uv_h, image_wh_size);

            DX.DrawPolygon3D(out v4[0], 2, image, 0);
        }

        void SetupV4( ref DX.VERTEX3D[] v4, int uv_x, int uv_y, int uv_w, int uv_h, int image_wh_size)
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
            DX.COLOR_U8 spc = DX.GetColorU8(70, 70, 70, 255);
            v4[0].spc = spc;
            v4[1].spc = spc;
            v4[2].spc = spc;
            v4[3].spc = spc;
            v4[4].spc = spc;
            v4[5].spc = spc;

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

        // 三次元空間の座標を返す
        public void Get3DPos( int map_x, int map_y, ref float pos_x, ref float pos_y, ref float pos_z )
        {
            pos_x = map_x * 10.0f + 5.0f;
            pos_z = map_y * 10.0f + 5.0f;
            
            pos_y = map_squares[map_x + map_y * map_w].height * HIGHT_ONE_VALUE;
        }

        // 移動範囲エフェクトの設定
        public void SetMoveAreaEffect(int map_x, int map_y, int move_power, int jump_power, string move_type_effect)
        {
            move_area.Check(map_x, map_y, move_power, jump_power);
            if (route_as_move_area != null) route_as_move_area = null;
        }

        // 移動経路エフェクトの設定
        public void SetMoveRouteEffect(int map_x, int map_y )
        {
            if (route_as_move_area == null) route_as_move_area = new RouteAsRangeArea(move_area);
            route_as_move_area.Check(map_x, map_y);
        }

        // 行動対象エフェクトの設定
        public void SetActionTargetAreaEffect( int map_x, int map_y, int range_min, int range_max, int jump_power )
        {
            action_target_area.range_min = range_min;
            action_target_area.Check(map_x, map_y, range_max, jump_power);
        }

        // 範囲エフェクト系を初期化する
        public void ClearRangeAreaEffect()
        {
            move_area.Clear();
            if (route_as_move_area != null) route_as_move_area = null;
            //if (route_as_shoot != null) route_as_shoot = null; // ここで消すと都合が悪い（前後する場合があるのでこのまま）
            action_target_area.Clear();
        }

        // ユニットの頭の位置くらいのスクリーン座標を返す（ダメージ表記用など）
        public Point GetScreenPositionByUnitTop( int map_x, int map_y )
        {
            var p = new Point(0,0);

            float dx = 0, dy = 0, dz = 0;
            Get3DPos(map_x, map_y, ref dx, ref dy, ref dz);

            dy += 20.0f; // とりあえず、頭の高さとして足しておく
            var dv = DX.VGet(dx, dy, dz);
            var v = DX.ConvWorldPosToScreenPos( dv ) ;
            p.X = (int)v.x;
            p.Y = (int)v.y;

            return p;
        }

        // 手番ユニットを示すカーソルの位置を設定
        public void SetTurnOwnerCursor( Unit u )
        {
            cursor_turn_owner_unit = u;
            //if ( u==null)
            //{
            //    is_draw_cursor_turn_owner = false;
            //}
            //else
            //{
            //    is_draw_cursor_turn_owner = true;
            //    float x=0.0f, y = 0.0f, z = 0.0f;
            //    Get3DPos(u.map_x, u.map_y, ref x, ref y, ref z);
            //    y += 20.0f;
            //    model_cursor_turn_owner.Pos(x, y, z);
            //}
        }

        public void SetMapGroundMaterial( int x, int y, string key )
        {
            var p = x + y * map_w;
            var sq = map_squares[p];

            var gm = material_manager.ground_materials[key];
            sq.ground_material = gm;
        }

    }
}
