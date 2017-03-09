using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TOL_SRPG.Base;

namespace TOL_SRPG.App.Map
{
    public class Wall
    {
        public enum DirectionID : int
        {
            S = 0, // 南
            E = 1, // 右、東
            N = 2, // 上
            W = 3, // 左
        }

        int map_x;
        int map_y;
        public MapWallMaterial material = null;
        public S3DPanel panel;
        public int height;
        DirectionID direction_id;

        public Wall(int map_x, int map_y, int height, DirectionID direction_id)
        {
            this.map_x = map_x;
            this.map_y = map_y;
            this.height = height;
            this.direction_id = direction_id;

            var size = new SDPoint(BattleMap.SQUARE_SIZE, BattleMap.HIGHT_ONE_VALUE);

            panel = null;
            switch (direction_id)
            {
                case DirectionID.S:
                    {
                        var pos = new S3DPoint(BattleMap.SQUARE_SIZE * (map_x + 0.5), BattleMap.HIGHT_ONE_VALUE * (height + 0.5), BattleMap.SQUARE_SIZE * (map_y + 0.0));
                        panel = new S3DPanel(pos, size, S3DPanel.Direction.Wall_NS);
                    }
                    break;
                case DirectionID.E:
                    {
                        var pos = new S3DPoint(BattleMap.SQUARE_SIZE * (map_x + 1.0), BattleMap.HIGHT_ONE_VALUE * (height + 0.5), BattleMap.SQUARE_SIZE * (map_y + 0.5));
                        panel = new S3DPanel(pos, size, S3DPanel.Direction.Wall_EW);
                    }
                    break;
                case DirectionID.N:
                    {
                        var pos = new S3DPoint(BattleMap.SQUARE_SIZE * (map_x + 0.5), BattleMap.HIGHT_ONE_VALUE * (height + 0.5), BattleMap.SQUARE_SIZE * (map_y + 1.0));
                        panel = new S3DPanel(pos, size, S3DPanel.Direction.Wall_NS);
                    }
                    break;
                case DirectionID.W:
                    {
                        var pos = new S3DPoint(BattleMap.SQUARE_SIZE * (map_x + 0.0), BattleMap.HIGHT_ONE_VALUE * (height + 0.5), BattleMap.SQUARE_SIZE * (map_y + 0.5));
                        panel = new S3DPanel(pos, size, S3DPanel.Direction.Wall_EW);
                    }
                    break;
            }
        }

        public void Draw()
        {
            if (panel != null) panel.Draw();
        }
    }
}
