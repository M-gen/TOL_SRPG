using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;

namespace TOL_SRPG.App
{
    public class Unit : IDisposable
    {

        public class ActionStatus
        {
            public string system_name = "";
            public int    reflect_point = 100; // 行動の実力発揮%(100=100%)
        }

        public class BTS
        {
            public int now; // 現在値
            public int max; // 最大値
            public int def; // 基本値

            public BTS(int value): // 略式コンストラクタ
                this(value,value,value) {}

            public BTS( int now, int max, int def)
            {
                this.now = now;
                this.max = max;
                this.def = def;
            }
        }

        public class BattleStatus
        {
            //public int movement_force = 5;
            //public int jumping_force = 1;
            public string class_name = "";
            public List<ActionStatus> actions = new List<ActionStatus>();

            public Dictionary<string, BTS> status = new Dictionary<string, BTS>();
            
            public bool is_alive = true;
        }


        G3DModel model;
        S3DCube hit_check_cube = null; // 当たり判定用キューブ
        Script script;
        int direction; // 方向

        public int image_face;
        public string name;

        public int map_x;
        public int map_y;

        float pos3d_x;
        float pos3d_y;
        float pos3d_z;

        public BattleStatus bt = new BattleStatus();

        string last_motion_key = "";

        public Unit(string class_name, string model_path, string image_face_path, string name, int map_x, int map_y, int color_no = 0, int direction = 2 )
        {
            bt.class_name = class_name;
            Setup();
            var ucd = UnitDataManager.GetUnitClassData(bt.class_name);
            if (model_path == "")      model_path = ucd.model_default_path;
            if (image_face_path == "") image_face_path = ucd.image_default_path;

            model = new G3DModel(model_path);
            image_face = DX.LoadGraph(image_face_path);

            this.name = name;
            this.direction = direction;

            script = new Script(model.model_root_dir + "system.nst", _ScriptLineAnalyze);
            script.Run("Setup");
            //script.Run("Status");

            SetMotion("歩行");
            Move(map_x, map_y,true);
            model.RotAdd(0, (float)(-Math.PI* direction / 2.0), 0);


            if ( color_no!=0 )
            {
                SetColor(color_no);
            }
        }

        private void Setup()
        {
            var ucd = UnitDataManager.GetUnitClassData(bt.class_name);
            foreach( var st in ucd.status)
            {
                var bts = new BTS(st.Value.default_value);
                bt.status.Add(st.Key, bts);
            }
            foreach( var ad in ucd.actions)
            {
                var a = new ActionStatus();
                a.system_name = ad.system_name;
                a.reflect_point = ad.reflect_point;
                bt.actions.Add(a);

            }
        }

        public void Update()
        {
            if (!bt.is_alive) return;
            model.Update();
        }

        public void UpdateHitCheckCube()
        {
            var size_x = 10;
            var size_y = 15;
            var size_z = 5;
            var pos_y_offset = 7.5;
            switch (direction) // 向きで当たり判定を変えておく(3Dモデルがそうだとは限らないけど)
            {
                case 1:
                    size_x = 5;
                    size_z = 10;
                    break;
                case 3:
                    size_x = 5;
                    size_z = 10;
                    break;
            }

            if (hit_check_cube == null)
            {
                hit_check_cube = new S3DCube(new S3DPoint(pos3d_x, pos3d_y + pos_y_offset, pos3d_z), new S3DPoint(size_x, size_y, size_z));
            }
            else
            {
                hit_check_cube.SetPos(new S3DPoint(pos3d_x, pos3d_y + pos_y_offset, pos3d_z));

            }
        }

        public void Draw()
        {
            if (!bt.is_alive) return;
            model.Draw();
            //hit_check_cube.Draw();
        }

        public void Move( int map_x, int map_y, bool is_right_now )
        {
            this.map_x = map_x;
            this.map_y = map_y;

            var main = GameMain.GetInstance();
            main.g3d_map.Get3DPos(map_x, map_y, ref pos3d_x, ref pos3d_y, ref pos3d_z);
            model.Pos(pos3d_x, pos3d_y, pos3d_z);

            UpdateHitCheckCube();
        }

        public void SetColor( int no )
        {
            // とりあえず固定で...
            //// model側に作ったほうが良いかも...拡張考えるとどうするかな
            //var texture_index = 1;
            //var image_handle = DX.LoadGraph("data/mmd/ch02/uv_ch3_戦士_02_s2.png"); ;
            //DX.MV1SetTextureGraphHandle(model.model_handle, texture_index, image_handle, DX.FALSE);
            model.ChangeTexture(no);
        }

        public void SetMotion( string motion_key)
        {
            if (last_motion_key != "")
            {
                model.DeleteMotion(last_motion_key);
            }
            model.AddMotion( motion_key, 0.16666f * 2.00f);
            last_motion_key = motion_key;
        }

        public void SetAlpha( double alpha )
        {
            model.Alpha((float)alpha);
        }

        public bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            switch( t.command[0] )
            {
                case "SetColorChange":
                    {
                        var color_no       = t.GetInt(1);
                        var material_index = t.GetInt(2);
                        var texture_path   = t.GetString(3);
                        model.AddChangeTextureData( color_no, material_index, texture_path );
                    }
                    return true;
                case "ConectMotion":
                    {
                        var key = t.GetString(1);
                        var no  = t.GetInt(2);
                        model.AddConectMotionData(key, no);
                    }
                    return true;
            }
            return false;
        }


        // 戦闘参加準備
        public void SetupBattle()
        {
            // 位置を初期化
            // 高さが引き継がれてバグるので
            Move(map_x, map_y, true);

            // WTを初期化
            ResetWT();
        }

        public void Dispose()
        {
            DX.DeleteGraph(image_face);
        }

        public void SetAlive( bool is_alive, int join_map_x = -1, int join_map_y = -1)
        {
            bt.is_alive = is_alive;
            if (!bt.is_alive)
            {
                map_x = -1;
                map_y = -1;
                bt.status["WT"].now = 999;
            }
            else
            {
                map_x = join_map_x;
                map_y = join_map_y;
            }
        }

        public void ResetWT()
        {
            bt.status["WT"].now = bt.status["WT"].max; // 最大値まで
        }

        // Moveで指定された位置からオフセットで表示位置を変更する(累積しない)
        // Moveで打ち消される
        public void SetPosOffset( double x, double y, double z )
        {
            model.Pos(pos3d_x + (float)x, pos3d_y + (float)y, pos3d_z + (float)z);
        }

        public HitStatus CheckHit( S3DLine line )
        {
            return hit_check_cube.CheckHit(line);
        }
    }

    // 複数のユニットを管理するためのもの
    public class UnitManager
    {
        public class UnitManagerStatus
        {
            public Unit unit;
            public string group;
        }

        public List<UnitManagerStatus>        units = new List<UnitManagerStatus>();
        public List<UnitManagerStatus> active_units = new List<UnitManagerStatus>(); // 行動可能ユニット

        public UnitManager()
        {

        }

        public void Join( Unit unit, string group )
        {
            var u = new UnitManagerStatus();
            u.unit = unit;
            u.group = group;
            units.Add(u);

        }


        public Unit GetUnit(int map_x, int map_y)
        {
            foreach (var u in units)
            {
                if (u.unit.map_x == map_x && u.unit.map_y == map_y) return u.unit;
            }
            return null;
        }

        public string GetUnitGroup(int map_x, int map_y)
        {
            foreach (var u in units)
            {
                if (u.unit.map_x == map_x && u.unit.map_y == map_y) return u.group;
            }
            return "";
        }

        public void Update()
        {
            foreach (var u in units)
            {
                u.unit.Update();
            }

        }

        public void Draw()
        {
            foreach (var u in units)
            {
                u.unit.Draw();
            }
        }

        public void SetupBattle()
        {
            foreach (var u in units)
            {
                u.unit.SetupBattle();
            }
        }

        // 戦闘時間経過
        public void StepBattle()
        {
            active_units.Clear();
            foreach (var u in units)
            {
                if (!u.unit.bt.is_alive) continue; // 戦闘不能、退場しているキャラを除外する

                var now = u.unit.bt.status["WT"].now;
                if (now > 0)
                {
                    u.unit.bt.status["WT"].now--;
                    if (u.unit.bt.status["WT"].now==0)
                    {
                        // 行動可能としてリストに追加しておく
                        active_units.Add(u);
                    }
                }
            }
        }
    }

}
