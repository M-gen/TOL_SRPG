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

        public const int WALL_DIRECTION = 4;

        public int map_x;
        public int map_y;
        public int height;
        public int[] under = new int[WALL_DIRECTION]; // 周囲側面を作る必要があるかどうかと、その長さ

        public List<Wall>[] walls;
        public MapGroundMaterial ground_material = null;

        S3DPanel top_ground;

        public Square( int map_x, int map_y, int height)
        {
            this.map_x = map_x;
            this.map_y = map_y;
            this.height = height;

            walls = new List<Wall>[WALL_DIRECTION];
            for (var i = 0; i < WALL_DIRECTION; i++)
            {
                walls[i] = new List<Wall>();
            }

            top_ground = new S3DPanel( new S3DPoint(BattleMap.SQUARE_SIZE*(map_x+0.5), BattleMap.HIGHT_ONE_VALUE * height, BattleMap.SQUARE_SIZE * (map_y+0.5)),
                new SDPoint(BattleMap.SQUARE_SIZE, BattleMap.SQUARE_SIZE ), S3DPanel.Direction.Top );
            top_ground.SetColor(255, 255, 255, 255);
            top_ground.SetSpecularColor(255,70,70,70);
        }

        public void Setup(MapMaterialManager material_manager, string wall_default_material_key)
        {
            SetupWall(material_manager, wall_default_material_key);
            ground_material = material_manager.ground_materials[""]; // ダミーを設定
        }

        public void SetupWall(MapMaterialManager material_manager, string wall_default_material_key)
        {
            var copy_walls = this.CopyWalls();

            // 壁をデフォルトで初期化する
            var wdm = material_manager.wall_materials[wall_default_material_key];
            var i = 0;
            i = 0;
            foreach (var wall_line in walls)
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
                    var h_now = 3 - (height - h + j) % 4; // 2.5で、画像チップの四分の一の高さとする、そのいちを決める

                    wall.material = wdm;
                    wall.panel.SetColor(255, 255, 255, 255);
                    wall.panel.SetTexture(wdm.image_handle);
                    wall.panel.SetSpecularColor(255, 70, 70, 70);
                    wall.panel.SetUV(new SDPoint(0, h_now * 8), new SDPoint(32, 8));
                    wall_line.Add(wall);
                }
                i++;
            }

            PastWallMaterial(copy_walls);
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

        public void SetHeight( int height )
        {
            this.height = height;
            top_ground.SetPos(new S3DPoint(BattleMap.SQUARE_SIZE * (map_x + 0.5), BattleMap.HIGHT_ONE_VALUE * height, BattleMap.SQUARE_SIZE * (map_y + 0.5)));
            //top_ground = new S3DPanel(new S3DPoint(BattleMap.SQUARE_SIZE * (map_x + 0.5), BattleMap.HIGHT_ONE_VALUE * height, BattleMap.SQUARE_SIZE * (map_y + 0.5)),
            //    new SDPoint(BattleMap.SQUARE_SIZE, BattleMap.SQUARE_SIZE), S3DPanel.Direction.Top);
            //top_ground.SetColor(255, 255, 255, 255);
            //top_ground.SetSpecularColor(255, 70, 70, 70);
        }

        public HitStatus CheckHitGround(S3DLine line)
        {
            return top_ground.CheckHit(line);
        }

        public HitStatus CheckHitWall(S3DLine line, out Wall res_wall)
        {
            HitStatus tmp_hs = null;
            Wall tmp_wall = null;

            // 壁の描画
            foreach (var wall_line in walls)
            {
                foreach (var wall in wall_line)
                {
                    var hs = wall.panel.CheckHit(line);
                    if ( (tmp_hs==null|| tmp_hs.range > hs.range ) && (hs.is_hit) )
                    {
                        tmp_hs = hs;
                        tmp_wall = wall;
                    }
                }
            }
            if (tmp_hs == null)
            {
                tmp_hs = new HitStatus();
            }

            res_wall = tmp_wall;
            return tmp_hs;
        }

        /// <summary>
        /// コピーした壁の情報を返す
        /// </summary>
        /// <returns></returns>
        private List<Wall>[] CopyWalls()
        {
            var res = new List<Wall>[WALL_DIRECTION];

            for (var i = 0; i < WALL_DIRECTION; i++)
            {
                res[i] = new List<Wall>();
                
                foreach (var wall in walls[i])
                {
                    res[i].Add(new Wall(wall));
                }
            }

            return res;
        }

        /// <summary>
        /// 壁の素材をペーストする
        /// 高さを変更した時に、壁の初期化を上書きするためのもの
        /// </summary>
        /// <param name="src_wall"></param>
        private void PastWallMaterial(List<Wall>[] src_wall )
        {
            for (var i = 0; i < WALL_DIRECTION; i++)
            {
                var j_size = walls[i].Count();
                {
                    var j_size2 = src_wall[i].Count();
                    if (j_size2 < j_size) j_size = j_size2;
                }

                for( var j=0; j<j_size; j++)
                {
                    walls[i][j].material = src_wall[i][j].material;
                    walls[i][j].panel.SetTexture(walls[i][j].material.image_handle);
                }
                //foreach (var w in wall)
                //{
                //}
            }

        }

    }
}
