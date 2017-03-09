using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

using TOL_SRPG.App.Map;

namespace TOL_SRPG.App
{
    // 距離から算出されるエリアについての処理群
    // 汎用性をもたせつつ、基礎のRangeAreaの呼び出す関数はg3d_mapを参照しているので注意（変更は可能、高さ取得のため）

    public class RangeSquare
    {
        public bool is_ok = false;   // 
        public bool is_pass = false; // 通過点として可かどうか（着地・選択地として不可の場合があるので）
        public int step_count = -1;  // 歩数、エリアの深さ

        public int _tmp_move_power = -1; // エリアへの進入強度（あと何スクウェア進めるか）
    }
    public delegate int RangeCalcGetMoveCost(int map_x, int map_y, object obj);
    public delegate int RangeCalcGetHeight(int map_x, int map_y, object obj);
    public class RangeArea
    {
        public int start_map_x;
        public int start_map_y;
        public int map_h;
        public int map_w;
        public RangeSquare[] squares;
        public RangeCalcGetMoveCost range_calc_get_move_cost = _RangeCalcGetMoveCost;
        public object range_calc_get_move_cost_object = null;
        public RangeCalcGetHeight range_calc_get_height = _RangeCalcGetHeight;
        public object range_calc_get_height_object = null;
        public int move_power;
        public int jump_power;

        public string _unit_group;

        public List<int> _tmp_next_point_list; // 進入できそうなエリアの座標

        public RangeArea(int map_w, int map_h)
        {
            this.map_w = map_w;
            this.map_h = map_h;

            squares = new RangeSquare[map_w * map_h];
            for (var i = 0; i < squares.Count(); i++) squares[i] = new RangeSquare();


        }

        // 移動エリアを計算する
        // その、全体の構造を汎用的にしているので、この関数をoverrideする必要はないと思われる
        public virtual void Check(int map_x, int map_y, int move_power, int jump_power)
        {
            this.start_map_x = map_x;
            this.start_map_y = map_y;
            this.move_power = move_power; // マスを進める力
            this.jump_power = jump_power; // 上下方向への強さ

            Clear(); // 初期化

            // 移動エリア計算用
            // リストをキューとして扱い、次に判定すべきマスを入れていく
            // これにより判定処理を、全体と個別_NextCheckに分けられる
            // また、判定が距離の近い順と、射程順に処理できる
            _tmp_next_point_list = new List<int>();

            _unit_group = GameMain.GetInstance().unit_manager.GetUnitGroup(map_x, map_y);

            var p0 = start_map_x + start_map_y * map_w;
            //squares[p0]._tmp_move_power = move_power + range_calc_get_move_cost(start_map_x, start_map_y, range_calc_get_move_cost_object); // 初期座標へは移動コストが要らないのでその分を補填
            squares[p0]._tmp_move_power = move_power;
            squares[p0].step_count = 0;
            _tmp_next_point_list.Add(p0);

            while (_tmp_next_point_list.Count() > 0)
            {
                var p = _tmp_next_point_list[0];
                try
                {
                    _tmp_next_point_list.RemoveAt(0);
                }
                catch
                {
                    Console.WriteLine("Err RangeArea Check");
                }
                var x = p % map_w;
                var y = p / map_w;

                _NextCheck(x - 1, y, x, y);
                _NextCheck(x + 1, y, x, y);
                _NextCheck(x, y - 1, x, y);
                _NextCheck(x, y + 1, x, y);

            }
        }

        // 共通となりえそうなマスごとの判定
        protected virtual bool _NextCheckBasic(int next_x, int next_y, int now_x, int now_y, ref int leave_move_power)
        {
            if (next_x < 0) return false;
            if (next_y < 0) return false;
            if (next_x >= map_w) return false;
            if (next_y >= map_h) return false;

            var p = next_x + next_y * map_w;
            var p0 = now_x + now_y * map_w;
            if (squares[p]._tmp_move_power != -1) return false; // 計算済み

            var g3d_map = GameMain.GetInstance().g3d_map;

            // 進入コスト
            var move_cost = range_calc_get_move_cost(p % map_w, p / map_w, range_calc_get_move_cost_object);
            leave_move_power = squares[p0]._tmp_move_power - move_cost;
            if (leave_move_power < 0) return false;// 移動力がコストを下回っているので移動できない

            return true;
        }

        protected virtual void _NextCheckIsOkPassBasic( int p, int p0, bool is_ok, bool is_pass, int leave_move_power )
        {
            // 侵入OK
            squares[p]._tmp_move_power = leave_move_power;
            squares[p].step_count = squares[p0].step_count + 1;
            _tmp_next_point_list.Add(p);

            squares[p].is_ok = is_ok;
            squares[p].is_pass = is_pass;
        }

        protected virtual void _NextCheck(int next_x, int next_y, int now_x, int now_y)
        {
            // 共通判定と残コストの取得
            int leave_move_power = 0;
            if (_NextCheckBasic(next_x, next_y, now_x, now_y, ref leave_move_power) == false) return;

            // 以下専用判定
            var p = next_x + next_y * map_w;
            var p0 = now_x + now_y * map_w;
            var g3d_map = GameMain.GetInstance().g3d_map;

            // 高低差
            var height_difference = Math.Abs(
                range_calc_get_height(p0 % map_w, p0 / map_w, range_calc_get_height_object) -
                range_calc_get_height(p % map_w, p / map_w, range_calc_get_height_object));
            if (height_difference > jump_power) return; // 高低差がジャンプ力を超えている

            // ユニットグループが違うため通過もできない、かどうか
            var u = GameMain.GetInstance().unit_manager.GetUnit(next_x, next_y);
            var ug = GameMain.GetInstance().unit_manager.GetUnitGroup(next_x, next_y);
            if ( u!=null && ug != _unit_group) return; // 同じグループではないユニットがいる → 通過できない

            // 侵入OK
            squares[p]._tmp_move_power = leave_move_power;
            squares[p].step_count = squares[p0].step_count + 1;
            _tmp_next_point_list.Add(p);

            if (u == null)
            {
                squares[p].is_ok = true;
                squares[p].is_pass = true;
            }
            else
            {
                squares[p].is_pass = true;
            }
        }

        static int _RangeCalcGetMoveCost(int map_x, int map_y, object obj) { return 1; }
        static int _RangeCalcGetHeight(int map_x, int map_y, object obj)
        {
            if (obj == null) return 0;
            var g3d_map = (BattleMap)obj;

            return g3d_map.map_squares[map_x + map_y * g3d_map.map_w].height;
        }
        static int _RangeCalcGetHeightZero(int map_x, int map_y, object obj) { return 0; } // 高さ計算をしない場合

        // エリア内かどうか
        public bool IsInside( int map_x, int map_y )
        {
            var p = map_x + map_y * map_w;
            return squares[p].is_ok;
        }

        public void Clear()
        {
            for (var i = 0; i < squares.Count(); i++)
            {
                var sq = squares[i];
                sq.is_ok = false;
                sq.is_pass = false;
                sq.step_count = -1;
                sq._tmp_move_power = -1;
            }
        }

    }

    public class RouteAsRangeArea
    {
        public class RouteSquare
        {
            public int step_count = -1;
            public int _tmp_calc = -1;
            public int _tmp_calc_multi = -1;        // 複数ルート選別用 重み
            public bool _tmp_calc_is_route = false; // 複数ルート選別用 選択した

            public void Init()
            {
                step_count = -1;
                _tmp_calc = -1;
                _tmp_calc_multi = -1;
                _tmp_calc_is_route = false;
            }
        }

        // 複数ルート判別用
        public class RouteMultiPoint
        {
            public int x;
            public int y;
            public int multi;
        }


        RangeArea range_area;
        int start_map_x;
        int start_map_y;
        int end_map_x;
        int end_map_y;
        public RouteSquare[] squares;
        public List<Point> route_points = new List<Point>(); // 経路をPoint(x,y)でリストに並べたもの(スタート、エンドあり）

        List<int> _tmp_next_point_list;  // 進入できそうなエリアの座標
        List<RouteMultiPoint> _tmp_multi_point_list; 
        int _tmp_multi_point = 0;
        bool result = false;

        public RouteAsRangeArea(RangeArea range_area)
        {
            this.range_area = range_area;
            squares = new RouteSquare[range_area.map_w * range_area.map_h];
            for (int i = 0; i < squares.Count(); i++) squares[i] = new RouteSquare();
            start_map_x = range_area.start_map_x;
            start_map_y = range_area.start_map_y;

            end_map_x = -1;
            end_map_y = -1;
        }

        public bool Check(int end_map_x, int end_map_y)
        {
            if (this.end_map_x == end_map_x && this.end_map_y == end_map_y) return result;
            result = false;

            this.end_map_x = end_map_x;
            this.end_map_y = end_map_y;

            var end_pos = GetPoint(end_map_x, end_map_y);
            var start_pos = GetPoint(start_map_x, start_map_y);

            if (!range_area.squares[end_pos].is_ok) return false;

            // 初期化
            for (int i = 0; i < squares.Count(); i++)
            {
                squares[i].Init();
            }
            _tmp_next_point_list = new List<int>();
            _tmp_multi_point_list = new List<RouteMultiPoint>();
            route_points.Clear();

            route_points.Add(new Point( start_map_x, start_map_y ));

            // 経路になりうる場所を探索
            _tmp_next_point_list.Add(end_pos);
            while (_tmp_next_point_list.Count() > 0)
            {
                var p = _tmp_next_point_list[0]; _tmp_next_point_list.RemoveAt(0);

                var next_step_count = range_area.squares[p].step_count - 1;
                var x = p % range_area.map_w;
                var y = p / range_area.map_w;
                _NextCheck(x - 1, y, x, y, next_step_count);
                _NextCheck(x + 1, y, x, y, next_step_count);
                _NextCheck(x, y - 1, x, y, next_step_count);
                _NextCheck(x, y + 1, x, y, next_step_count);
            }

            // 複数のルートがある場合の判別処理
            _tmp_next_point_list.Add(start_pos);
            while (_tmp_next_point_list.Count() > 0)
            {
                var p = _tmp_next_point_list[0]; _tmp_next_point_list.RemoveAt(0);

                var next_step_count = range_area.squares[p].step_count + 1;
                var x = p % range_area.map_w;
                var y = p / range_area.map_w;
                _NextCheckMultiRoute(x - 1, y, x, y, next_step_count);
                _NextCheckMultiRoute(x + 1, y, x, y, next_step_count);
                _NextCheckMultiRoute(x, y - 1, x, y, next_step_count);
                _NextCheckMultiRoute(x, y + 1, x, y, next_step_count);

                var multi_point_min = -1;
                var multi_no = 0;
                var count = 0;
                foreach (var m in _tmp_multi_point_list)
                {
                    var p2 = GetPoint(m.x, m.y);
                    if ((multi_point_min == -1) || (multi_point_min < m.multi))
                    {
                        multi_point_min = m.multi;
                        multi_no = count;
                    }
                    count++;
                    Console.WriteLine("xy " + m.x + "," + m.y + " " + m.multi);
                }
                Console.WriteLine("--");
                List<RouteMultiPoint> tmp_remove_multi_point_list = new List<RouteMultiPoint>();
                foreach (var m in _tmp_multi_point_list)
                {
                    var p2 = GetPoint(m.x, m.y);
                    if (multi_point_min == m.multi)
                    {
                        // 選択された経路
                        route_points.Add(new Point(m.x, m.y));
                        squares[p2].step_count = squares[p2].step_count;
                        squares[p2]._tmp_calc_is_route = true;
                    }
                    else
                    {
                        // 選択されなかった経路
                        _tmp_next_point_list.Remove(p2);
                        tmp_remove_multi_point_list.Add(m);
                    }
                }
                foreach(var m in tmp_remove_multi_point_list) { _tmp_multi_point_list.Remove(m); }

                _tmp_multi_point_list.Clear();
            }

            // 選択しなかったルートのstep_countを削除
            for (int i = 0; i < squares.Count(); i++)
            {
                if (!squares[i]._tmp_calc_is_route)
                {
                    squares[i].step_count = -1;
                }
            }

            // 終着点
            squares[end_pos].step_count = range_area.squares[end_pos].step_count;
            route_points.Add(new Point(end_map_x, end_map_y));



            result = true;
            return result;
        }

        int GetPoint(int x, int y)
        {
            return x + y * range_area.map_w;
        }

        void _NextCheck(int next_x, int next_y, int now_x, int now_y, int next_step_count)
        {
            if (next_x < 0) return;
            if (next_y < 0) return;
            if (next_x >= range_area.map_w) return;
            if (next_y >= range_area.map_h) return;

            var p = next_x + next_y * range_area.map_w;
            var p0 = now_x + now_y * range_area.map_w;

            if (squares[p]._tmp_calc != -1) return; // 計算済み
            if (next_step_count < 0) return; // 次の歩数が0未満
            if (range_area.squares[p].step_count != next_step_count) return; // 次の歩数として合わない

            // 高低差がジャンプ力を超えている
            var height_difference = Math.Abs(
                range_area.range_calc_get_height( now_x,  now_y, range_area.range_calc_get_height_object) -
                range_area.range_calc_get_height(next_x, next_y, range_area.range_calc_get_height_object));
            if (height_difference > range_area.jump_power) return; 

            squares[p]._tmp_calc = next_step_count;
            squares[p]._tmp_calc_multi = _tmp_multi_point; _tmp_multi_point++; // 判断処理の早い順に低い値（細かい思考なし）
            squares[p].step_count = next_step_count;
            _tmp_next_point_list.Add(p);
        }

        // 複数のルートが有る場合、それぞれに重みをつけて判定する
        void _NextCheckMultiRoute(int next_x, int next_y, int now_x, int now_y, int next_step_count)
        {
            if (next_x < 0) return;
            if (next_y < 0) return;
            if (next_x >= range_area.map_w) return;
            if (next_y >= range_area.map_h) return;

            var p = next_x + next_y * range_area.map_w;
            var p0 = now_x + now_y * range_area.map_w;

            if (squares[p]._tmp_calc == -2) return; // 計算済み
            if (squares[p]._tmp_calc == -1) return; // 計算済み
            if (squares[p]._tmp_calc < 0) return;
            if (next_step_count < 0) return; // 次の歩数が0未満
            if (range_area.squares[p].step_count != next_step_count) return; // 次の歩数として合わない

            var m = new RouteMultiPoint();
            m.x = next_x;
            m.y = next_y;
            m.multi = squares[p]._tmp_calc_multi;
            _tmp_multi_point_list.Add(m);
            _tmp_next_point_list.Add(p);

        }

    }

    // 移動範囲クラス
    public class RangeAreaMove : RangeArea
    {
        public bool is_range_only = false; // 距離のみの計算として、trueなら敵をすり抜けて計算させる(敵との距離を計るため)

        public RangeAreaMove(int map_w, int map_h):
            base(map_w, map_h )
        {
            var game_main = GameMain.GetInstance();
            var g3d_map = game_main.g3d_map;
            range_calc_get_height_object = g3d_map;
        }

        protected override void _NextCheck(int next_x, int next_y, int now_x, int now_y)
        {
            // 共通判定と残コストの取得
            int leave_move_power = 0;
            if (_NextCheckBasic(next_x, next_y, now_x, now_y, ref leave_move_power) == false) return;

            // 以下専用判定
            var p = next_x + next_y * map_w;
            var p0 = now_x + now_y * map_w;
            var gm = GameMain.GetInstance();
            var g3d_map = gm.g3d_map;
            var u = gm.unit_manager.GetUnit(next_x, next_y);
            var ug = gm.unit_manager.GetUnitGroup(next_x, next_y);

            // 高低差
            var height_difference = Math.Abs(
                range_calc_get_height(p0 % map_w, p0 / map_w, range_calc_get_height_object) -
                range_calc_get_height(p % map_w, p / map_w, range_calc_get_height_object));
            if (height_difference > jump_power) return; // 高低差がジャンプ力を超えている

            // ユニットグループが違うため通過もできない、かどうか
            if (!is_range_only)
            {
                if (u != null && ug != _unit_group) return; // 同じグループではないユニットがいる → 通過できない
            }

            // 侵入OK
            if (u == null)
            { // 通過・ここに移動 できる
                _NextCheckIsOkPassBasic(p, p0, true, true, leave_move_power);
            }
            else
            { // ユニットが既に配置されているので通過しかできない
                _NextCheckIsOkPassBasic(p, p0, false, true, leave_move_power);
            }
        }
    }

    // ユニットをターゲットにする範囲
    // is_ok   : ユニットがある
    // is_pass : 射程範囲かどうか
    public class RangeAreaActionTarget : RangeArea
    {
        public int  range_min = 0;       // 射程距離：最小
        public bool is_my_target = true; // 自身をターゲットにできるのかどうか

        public RangeAreaActionTarget(int map_w, int map_h):
            base(map_w, map_h )
        {
        }

        public override void Check(int map_x, int map_y, int move_power, int jump_power)
        {
            base.Check(map_x, map_y, move_power, jump_power); // この処理で概ねOK

            var gm = GameMain.GetInstance();
            var g3d_map = gm.g3d_map;
            var my_u = gm.unit_manager.GetUnit(map_x, map_y); // スタート地点のユニットを自身とみなす（異なる可能性をいったん考慮せず…）

            // 自身を選択範囲に含めるかと
            // 最小距離の設定を反映させる
            for ( var i=0; i<squares.Count(); i++ )
            {
                var s = squares[i];
                var x = i % map_w; var y = i / map_w;
                
                if ( is_my_target && s.step_count==0 && s.step_count>=range_min)
                {
                    var u = gm.unit_manager.GetUnit(x, y);
                    if ( u== my_u )
                    {
                        s.is_ok = true;
                        s.is_pass = true;
                    }
                }
                if (s.step_count < range_min)
                {
                    s.is_ok = false;
                    s.is_pass = false;
                }

            }
            
        }

        protected override void _NextCheck(int next_x, int next_y, int now_x, int now_y)
        {
            // 共通判定と残コストの取得
            int leave_move_power = 0;
            if (_NextCheckBasic(next_x, next_y, now_x, now_y, ref leave_move_power) == false) return;

            // 以下専用判定
            var p = next_x + next_y * map_w;
            var p0 = now_x + now_y * map_w;
            var gm = GameMain.GetInstance();
            var g3d_map = gm.g3d_map;
            var u = gm.unit_manager.GetUnit(next_x, next_y);
            var ug = gm.unit_manager.GetUnitGroup(next_x, next_y);

            // 高低差
            var height_difference = Math.Abs(
                range_calc_get_height(p0 % map_w, p0 / map_w, range_calc_get_height_object) -
                range_calc_get_height(p % map_w, p / map_w, range_calc_get_height_object));
            if (height_difference > jump_power) return; // 高低差がジャンプ力を超えている

            // 侵入OK
            squares[p]._tmp_move_power = leave_move_power;
            squares[p].step_count = squares[p0].step_count + 1;
            _tmp_next_point_list.Add(p);

            // 侵入OK
            if (u == null)
            { // ユニットがいない(is_passのみ)
                _NextCheckIsOkPassBasic(p, p0, false, true, leave_move_power);
            }
            else
            { // ユニットが配置されている
                _NextCheckIsOkPassBasic(p, p0, true, true, leave_move_power);
            }
        }

    }
    public class RouteAsShoot
    {

        public int start_map_x;
        public int start_map_y;
        public int end_map_x;
        public int end_map_y;
        public int map_ox;
        public int map_oy;
        public int map_w; //
        public int map_h;
        //public int map_height; // 高さ最大値
        public int[] height_map_xz;         // 上から見下ろしたXZ軸で見た高さマップ
        public int[] height_map_route;      // startからendまでを繋いで切った断面図を描く
        public List<Point> route_pos_list_xz = new List<Point>();          // 上から見下ろした場合の直線ルート
        public List<Point> route_pos_list_route = new List<Point>();       // 断面図の軌道
        public List<ExDPoint> direction_list_route = new List<ExDPoint>(); // route_pos_list_routeに対応して、各座標に対する速度から、軌道の角度を得る

        public int start_x;
        public int start_y;
        public int start_z;
        public int end_x;
        public int end_y;
        public int end_z;
        public int offset_x;
        //public int offset_y;
        public int offset_z;
        public int width_x;
        public int width_z;
        public int heigth_max;       // height_map_route1-2 の高さ
        public int width_height_map; // height_map_route1-2 の横幅

        public double direction = 0;

        public int sq_size = 40; // スクエア1つのサイズ、なお高さは÷4で10刻みである

        public class DPoint
        {
            public double X;
            public double Y;
            public DPoint( double x, double y)
            {
                X = x; Y = y;
            }
        }
        public class ExDPoint : DPoint
        {
            public double rot; // ベクトルの角度
            public ExDPoint(double x, double y):base(x,y)
            {
                rot = Math.Atan( y / x ) ;
            }
        }


        public RouteAsShoot(int start_map_x, int start_map_y, float start_height_offset, int end_map_x, int end_map_y, float end_height_offset, int rot_r)
        {
            this.start_map_x = start_map_x;
            this.start_map_y = start_map_y;
            this.end_map_x = end_map_x;
            this.end_map_y = end_map_y;
            map_ox = start_map_x; if (end_map_x < map_ox) map_ox = end_map_x;
            map_oy = start_map_y; if (end_map_y < map_oy) map_oy = end_map_y;

            var g3d_map = GameMain.GetInstance().g3d_map;
            g3d_map.route_as_shoot = this;

            var start_map_height = g3d_map.GetHeight(start_map_x, start_map_y);
            var end_map_height = g3d_map.GetHeight(end_map_x, end_map_y);

            start_x = (int)(start_map_x * sq_size);
            start_z = (int)(start_map_y * sq_size);
            start_y = (int)(start_map_height * sq_size / 4.0 + sq_size * start_height_offset);
            end_x = (int)(end_map_x * sq_size);
            end_z = (int)(end_map_y * sq_size);
            end_y = (int)(end_map_height * sq_size / 4.0 + sq_size * end_height_offset);

            width_x = Math.Abs(start_x - end_x) + sq_size;
            width_z = Math.Abs(start_z - end_z) + sq_size;
            heigth_max = 15 * 40;

            offset_x = start_x; if (offset_x > end_x) offset_x = end_x;
            offset_z = start_z; if (offset_z > end_z) offset_z = end_z;

            // 開始位置と終端位置を補正 width_x widht_z の確定後でないと補正できない
            start_x += sq_size / 2;
            start_z += sq_size / 2;
            end_x += sq_size / 2;
            end_z += sq_size / 2;

            // 真上から見下ろしたときの高さ情報(XZ平面空間)を作成
            height_map_xz = new int[width_x * width_z];

            var size = height_map_xz.Count();
            for (var i = 0; i < size; i++)
            {
                var x = i % width_x;
                var z = i / width_x;
                var map_x = map_ox + x / 40;
                var map_y = map_oy + z / 40;
                var h = g3d_map.GetHeight(map_x, map_y) * 10;
                height_map_xz[i] = h;
            }

            // XZ平面空間での開始から終端までのルートを算出
            {
                var dm_size = 0;
                var add_x = 1;
                var add_y = 1;
                if (start_x > end_x) add_x = -1;
                if (start_z > end_z) add_y = -1;

                if (width_x> width_z)
                { // 横幅のほうが長い → 横を1ずつ計算して進める
                    dm_size = width_x - sq_size + 1;
                    var x = start_x;
                    var y = start_z;
                    var diff_x = end_x - start_x;
                    var diff_z = end_z - start_z;
                    for ( int i=0; i<dm_size; i++ )
                    {
                        y = (int)((double)diff_z / diff_x * ((i+0.5) * add_x) + start_z);
                        route_pos_list_xz.Add(new Point(x, y));
                        x += add_x;
                    }
                }
                else
                { // 縦幅のほうが長い → 縦を1ずつ計算して進める
                    dm_size = (width_z - sq_size + 1);
                    var x = start_x;
                    var y = start_z;
                    var diff_x = end_x - start_x;
                    var diff_z = end_z - start_z;
                    for (int i = 0; i < (width_z - sq_size + 1); i++)
                    {
                        x = (int)((double)diff_x / diff_z * ((i + 0.5) * add_y) + start_x);
                        route_pos_list_xz.Add(new Point(x, y));
                        y += add_y;
                    }

                }

            }

            // 断面図の算出
            width_height_map = route_pos_list_xz.Count();
            var um = GameMain.GetInstance().unit_manager;
            height_map_route = new int[width_height_map * heigth_max];
            {
                var height_map_route_w = route_pos_list_xz.Count();
                for (var j = 0; j < height_map_route.Count(); j++) height_map_route[j] = -2; // 初期化

                // 地形は-1埋め、ユニットは各番号で埋める
                //for ( var i = 0; i< route_pos_list_xz.Count(); i++)
                var i = 0;
                foreach( var p in route_pos_list_xz)
                {
                    var map_x = p.X / 40;
                    var map_y = p.Y / 40;
                    var h = height_map_xz[p.X - offset_x + (p.Y-offset_z) * width_x];

                    //var x =
                    var u = um.GetUnit(map_x, map_y);

                    for ( var y=0; y<=h; y++)
                    {
                        height_map_route[i + y * width_height_map] = -1;
                    }

                    if (u != null)
                    {
                        var unit_height = (int)(40 * 1.5);
                        for (var y = h+1; y < h+ unit_height; y++)
                        {
                            height_map_route[i + y * width_height_map] = 0;
                        }

                    }

                    i++;
                }
            }

            // 断面図の放物線を計算
            // 2次方程式だが、頂点がわからない　→　減速速度(ag)と始点、希望着地点から近似値を算出
            // 角度を少しずつずらしながら近い値を使うので、処理が重い
            // ずらし方をいったん大雑把にして、その近似付近を細かくしらべる、を繰り返すのもありだけど保留
            {
                var size_xz = route_pos_list_xz.Count();
                var start_p = route_pos_list_xz[0];
                var end_p = route_pos_list_xz[size_xz - 1];

                var ag = 0.0025; // 減速速度
                var y1 = (double)height_map_xz[start_p.X - offset_x + (start_p.Y - offset_z) * width_x] + 40.0 * 0.75; // 
                var y2 = (double)height_map_xz[end_p.X - offset_x + (end_p.Y - offset_z) * width_x] + 40.0 * 0.75; //

                var rot_min = 60.0; // 最小角度、-45などにすると近距離で角度なしが選択されやすい
                var map_range_x = start_map_x - end_map_x;
                var map_range_y = start_map_y - end_map_y;
                var map_range = Math.Sqrt(map_range_x * map_range_x + map_range_y * map_range_y);


                var list = new List<Point>();
                var most_near = -1.0;
                var most_rot_r = 89.0;
                for (var rot_r1 = 89.999; rot_r1 > rot_min; rot_r1 -= 0.1)
                {
                    var res_y = GetShootRoute(ag, rot_r1, y1, size_xz, ref route_pos_list_route, ref direction_list_route, false);
                    var near = res_y - y2; if (near < 0) near = -near;

                    if ((most_near==-1.0)||(near<most_near))
                    {
                        most_near = near;
                        most_rot_r = rot_r1;
                    }
                }

                GetShootRoute(ag, most_rot_r, y1, size_xz, ref route_pos_list_route, ref direction_list_route, true);
            }

            // 方向を計算する
            if (end_x == start_x)
            {
                if (end_z - start_z > 0)
                {
                    direction = Math.PI * 0.5; //  90度
                }
                else
                {
                    direction = Math.PI * 1.5; // 270度
                }
            }
            else if (end_z == start_z)
            {
                if (end_x - start_x > 0)
                {
                    direction = 0; //  0度
                }
                else
                {
                    direction = Math.PI * 1.0; // 180度
                }

            }
            else
            {
                var dz = end_z - start_z;
                var dx = end_x - start_x;
                direction = Math.Atan((double)(dz) / (double)(end_x - start_x));
                if ( dx > 0 && dz > 0 )
                {
                    // そのまま
                }
                else if ( dx < 0 && dz > 0)
                {
                    direction += Math.PI;
                }
                else if (dx < 0 && dz < 0)
                {
                    direction += Math.PI;
                }
                else // if (dx > 0 && dz < 0)
                {
                    direction += Math.PI * 2.0;
                }
            }
        }

        // ag : 減速度、重力と初速に影響する
        // rot_r : 投射角 (単位:ラジアン 90度etc...)
        // y1 : 初期の高さ
        // size : 長さ
        // list : 結果のPointを入れるところ
        // is_list : listへの追加をしない(処理高速化)、いっそfor文を別に作ったほうがいいかも
        double GetShootRoute( double ag, double rot_r, double y1, int size, ref List<Point> list, ref List<ExDPoint> list_exdp, bool is_list)
        {
            var rot = Math.PI / 180.0 * rot_r;
            var Ax = Math.Cos(rot);
            var Ay = Math.Sin(rot);

            var speed = Ay;
            var tmp_y = y1;
            for (var i = 0; i * Ax < size; i++)
            {
                var x = (int)(i * Ax);
                var y = (int)(tmp_y+0.5);
                if (is_list)
                {
                    list.Add(new Point(x, y));
                    list_exdp.Add(new ExDPoint(Ax, speed));
                }
                tmp_y += speed;
                speed -= ag;
            }

            return tmp_y;
        }

        public void Check()
        {

        }
    }

}
