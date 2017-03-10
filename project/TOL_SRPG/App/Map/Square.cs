using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TOL_SRPG.Base;

namespace TOL_SRPG.App.Map
{

    public class Square
    {

        const int WALL_DIRECTION = 4;

        public int map_x;
        public int map_y;
        public int height;
        public int[] under = new int[WALL_DIRECTION]; // 周囲側面を作る必要があるかどうかと、その長さ

        public List<MapWallMaterial>[] wall_matelals;
        public List<Wall>[] walls;
        public MapGroundMaterial ground_material = null;

        S3DPanel top_ground;

        public Square( int map_x, int map_y, int height)
        {
            this.map_x = map_x;
            this.map_y = map_y;
            this.height = height;

            wall_matelals = new List<MapWallMaterial>[WALL_DIRECTION];
            walls = new List<Wall>[WALL_DIRECTION];
            for (var i = 0; i < WALL_DIRECTION; i++)
            {
                wall_matelals[i] = new List<MapWallMaterial>();
                walls[i] = new List<Wall>();
            }

            top_ground = new S3DPanel( new S3DPoint(BattleMap.SQUARE_SIZE*(map_x+0.5), BattleMap.HIGHT_ONE_VALUE * height, BattleMap.SQUARE_SIZE * (map_y+0.5)),
                new SDPoint(BattleMap.SQUARE_SIZE, BattleMap.SQUARE_SIZE ), S3DPanel.Direction.Top );
            top_ground.SetColor(255, 255, 255, 255);
            top_ground.SetSpecularColor(255,70,70,70);
        }

        public void Setup(MapMaterialManager material_manager, string wall_default_material_key)
        {
            // 壁をデフォルトで初期化する
            var wdm = material_manager.wall_materials[wall_default_material_key];
            var i = 0;
            foreach (var wms in wall_matelals)
            {
                wms.Clear();
                var h = under[i];
                for (var j = 0; j < h; j++)
                {
                    wms.Add(wdm);
                }
                i++;
            }

            i = 0;
            foreach ( var wall_line in walls)
            {
                Wall.DirectionID direction_id = Wall.DirectionID.S;
                switch (i)
                {
                    case 1: direction_id = Wall.DirectionID.E; break;
                    case 2: direction_id = Wall.DirectionID.N; break;
                    case 3: direction_id = Wall.DirectionID.W; break;
                }

                wall_line.Clear();
                var h = under[i];
                for (var j = 0; j < h; j++)
                {
                    var wall = new Wall(map_x, map_y, height - h + j, direction_id);
                    var h_now = 3 - (h + j) % 4; // 2.5で、画像チップの四分の一の高さとする、そのいちを決める

                    wall.panel.SetColor(255, 255, 255, 255);
                    wall.panel.SetTexture(wall_matelals[i][j].image_handle);
                    wall.panel.SetSpecularColor(255, 70, 70, 70);
                    wall.panel.SetUV(new SDPoint(0, h_now * 8), new SDPoint(32, 8));
                    wall_line.Add(wall);
                }
                i++;
            }

            ground_material = material_manager.ground_materials[""]; // ダミーを設定

        }

        public void Draw()
        {
            // 頂上のスクウェアを描画
            var handle = ground_material.image_handle;
            top_ground.SetTexture(handle); // todo ここは先に１回だけ初期化できるはず。。。
            top_ground.Draw();

            // 壁の描画
            foreach (var wall_line in walls)
            {
                foreach( var wall in wall_line)
                {
                    wall.Draw();
                }
            }

        }

        public HitStatus CheckHitGround(S3DLine line)
        {
            return top_ground.CheckHit(line);
        }

    }
}
