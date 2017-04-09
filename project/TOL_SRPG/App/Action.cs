using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;
using System.Drawing;

namespace TOL_SRPG.App
{
    public class Action : IDisposable
    {
        protected bool is_end = false;
        public bool is_frease = true;

        public virtual void Update() { }
        public virtual void Draw() { }
        public bool IsEnd() { return is_end; }

        public virtual void Dispose()
        {
        }
    }


    public class ActionManager
    {
        static ActionManager action_manager = null;
        List<Action> actions = new List<Action>();

        static object lock_action_foreach = new object(); // AIが別スレッドなので、タイミングによってアクション追加とアクションの更新や描画が交錯する

        public ActionManager()
        {
            action_manager = this;
        }

        static public void Add(Action a)
        {
            lock (lock_action_foreach)
            {
                action_manager.actions.Add(a);
            }
        }

        public void Update()
        {
            if (actions.Count() == 0) return;

            lock (lock_action_foreach)
            {
                List<Action> delete_actions = new List<Action>();
                foreach (var a in actions)
                {
                    a.Update();
                    if (a.IsEnd())
                    {
                        delete_actions.Add(a);
                    }
                }

                foreach (var a in delete_actions)
                {
                    a.Dispose();
                    actions.Remove(a);
                }
            }
        }

        public void Draw()
        {
            if (actions.Count() == 0) return;

            lock (lock_action_foreach)
            {
                foreach (var a in actions)
                {
                    a.Draw();
                }
            }
        }

        // 操作を停止させる必要のあるアクション中かどうか
        public bool IsControlFreese()
        {
            lock (lock_action_foreach)
            {
                foreach (var a in actions)
                {
                    //var ad = a as ActionDamage;
                    if ( a.is_frease ) 
                    {
                        //if (ad.is_unable_to_fight)
                        //{
                        //    return true; // 戦闘不能が内部的にも確定してないので待機する必要がある
                        //}
                        //else if (ad.timer > 30)
                        //{
                        //    // ダメージを与えるだけであれば、早めに切り上げ無いとテンポが悪いのと、支障がない
                        //}
                        //else
                        //{
                        //    return true;
                        //}
                        return true;
                    }
                    else
                    {
                        return false; 
                    }
                }
                //if (actions.Count() > 0) return true;
                return false;
            }
        }

    }

    public class ActionDamage : Action
    {
        public int timer = 0;
        int damage_value = 50;

        SFont font;

        //int add_y = -100;
        //int add_y_speed = 100;
        int add_y = 0;
        int add_y_speed = -50;

        int ox = 0;
        int oy = 0;
        int timer_start_wait = 0;
        Unit target_unit;
        public bool is_unable_to_fight = false; // 戦闘不能


        public ActionDamage(int start_wait, int x, int y, int damage_value, Unit target_unit)
        {
            ox = x; oy = y;
            this.timer_start_wait = start_wait;
            this.damage_value = damage_value;
            this.target_unit = target_unit;

            font = new SFont(GameMain.main_font_name_b, 21, 0, SFont.Antialiasing.Normal, 2);
        }

        public override void Update()
        {
            if (timer_start_wait > 0)
            {
                timer_start_wait--;
                return;
            }

            timer++;

            //add_y_speed--;
            add_y_speed += 3;
            add_y += add_y_speed / 15;
            //add_y += 3; // 重力
            if (add_y > 0)
            {
                var div = 0.7;
                add_y = (int)(-add_y * div);
                add_y_speed = (int)(-add_y_speed * div);
            }

            if (timer == 1)
            {
                var hp = target_unit.bt.status["HP"];
                var now_hp = hp.now;
                hp.now -= damage_value;
                if (hp.now <= 0)
                {

                    if (now_hp != 0)
                    {
                        is_unable_to_fight = true;
                    }
                    hp.now = 0;
                }
            }
            else if (timer == 20)
            {
                if (is_unable_to_fight)
                {
                    target_unit.SetMotion("戦闘不能");
                    SoundManager.PlaySound("戦闘／倒れる音", 0.5);
                }
            }

            if (is_unable_to_fight) { // 戦闘不能時のフェードアウト（透明化）と、退場処理
                var start = 40;
                var wait = 60;
                var end = start + wait;
                if (start < timer && timer < end)
                {
                    var a = (double)(wait - (timer - start)) / (double)wait;
                    target_unit.SetAlpha(a);

                }
                if (timer == end)
                {
                    target_unit.SetAlive(false);
                }
            }

            if (timer >= 160) is_end = true;

        }


        public override void Draw()
        {
            if (timer_start_wait > 0) return;

            var x = ox;
            var y = oy;
            var text = damage_value.ToString();
            var color_text = DX.GetColor(255, 255, 255);
            var color_text_frame = DX.GetColor(255, 0, 0);

            y += add_y;

            var a_blend = 255;
            if (timer < 50)
            {
                a_blend = timer * 7;
            }
            else if (timer > 130)
            {
                a_blend = 255 - (timer - 130) * 20;
            }
            if (a_blend > 255) a_blend = 255;
            if (a_blend < 0) a_blend = 0;

            var w = DX.GetDrawStringWidthToHandle(text, text.Count(), font.GetHandle());

            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, a_blend);
            DX.DrawStringFToHandle(x - w / 2, y, text, color_text, font.GetHandle(), color_text_frame);
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);

        }
    }

    public class ActionShootArrow : Action
    {
        int timer = 0;

        //G3DModel model;
        DX.VECTOR start_pos;
        List<Point> route_xz_list;
        List<Point> route_y_list;
        List<RouteAsShoot.ExDPoint> direction_list;
        RouteAsShoot route_as_shoot;

        int timer_start_wait = 0;
        float correct = 10.0f / 40.0f; // 軌道では1マス40で計算し、3D座標では1マス10で計算しているための補正値
        double shoot_time = 100;
        double correct_time_to_xz;
        double correct_time_to_y;

        float rot_y;
        bool is_success;
        OnePointModel model_arrow;

        public ActionShootArrow(int start_wait, RouteAsShoot route_as_shoot, bool is_success)
        {
            this.timer_start_wait = start_wait;
            this.is_success = is_success;
            this.route_as_shoot = route_as_shoot;

            model_arrow = ModelManager.RentModel("戦闘／矢");
            
            var start_x = route_as_shoot.start_x * correct;
            var start_y = route_as_shoot.start_y * correct + 20.0f;
            var start_z = route_as_shoot.start_z * correct;

            var end_x = route_as_shoot.end_x * correct;
            var end_y = route_as_shoot.end_y * correct;
            var end_z = route_as_shoot.end_z * correct;


            route_xz_list = new List<Point>( route_as_shoot.route_pos_list_xz);
            route_y_list = new List<Point>( route_as_shoot.route_pos_list_route);
            direction_list = new List<RouteAsShoot.ExDPoint>( route_as_shoot.direction_list_route);
            correct_time_to_xz = (double)route_xz_list.Count() / shoot_time;
            correct_time_to_y = (double)route_y_list.Count() / shoot_time;

            // 弓の向きを計算
            // もともとのモデルが -x方向に向いているのでこのような計算になる
            rot_y = (float)(Math.PI - route_as_shoot.direction);
            model_arrow.Rot(0, rot_y, 0);

            start_pos = DX.VGet(start_x, start_y, start_z);
        }

        public override void Update()
        {
            if (timer_start_wait > 0)
            {
                timer_start_wait--;
                return;
            }

            var g3d_map = GameMain.GetInstance().g3d_map;
            var t = timer; if (t >= shoot_time) t = (int)shoot_time - 1;
            var xz_time = (int)(t * correct_time_to_xz);
            var y_time = (int)(t * correct_time_to_y);
            var now_x = route_xz_list[xz_time].X * correct;
            var now_y = route_y_list[y_time].Y * correct;
            var now_z = route_xz_list[xz_time].Y * correct;

            // 回転で対応するのが難しかったので
            // 矢にモーションを付けて、角度をつけるようにした
            // 時刻0f:90度 → 10f:0度 →20f:-90度
            // と、移り変わるので、角度から目標の時刻を算出する
            var motion_frame_rot = direction_list[y_time].rot * 180.0 / Math.PI;
            var motion_frame = 0.0f;
            if (motion_frame_rot > 0) // 0度 <  < 90度
            {
                motion_frame = (float)((90.0 - motion_frame_rot) / 9.0);
            }
            else if (motion_frame_rot == 0.0) //  0度
            {
                motion_frame = (float)(10.0);
            }
            else if (motion_frame_rot == -90.0) // -90度
            {
                motion_frame = (float)(20.0);
            }
            else // -0度 <  < -90度
            {
                motion_frame = (float)(10.0 + (-motion_frame_rot) / 9.0);
            }

            model_arrow.Pos(now_x, now_y, now_z);
            model_arrow.Rot(0, rot_y, 0);
            model_arrow.SetMotionDirectFrame(0, motion_frame);
            model_arrow.Update();


            timer++;

            if (timer >= 100) is_end = true;
        }


        public override void Draw()
        {
            if (timer_start_wait > 0) return;

            model_arrow.Draw();

        }

    }

    //public class ActionSwing : Action
    //{
    //    int timer = 0;
    //    int timer_start_wait = 0;

    //    public ActionSwing(int start_wait = 0)
    //    {
    //        this.timer_start_wait = start_wait;
    //    }

    //    public override void Update()
    //    {
    //        if (timer_start_wait > 0)
    //        {
    //            timer_start_wait--;
    //            return;
    //        }


    //        timer++;

    //        if (timer == 1)
    //        {
    //            SoundManager.PlaySound("戦闘／攻撃／剣・開始", 0.5);
    //        }
    //        else if (timer == 20)
    //        {
    //            SoundManager.PlaySound("戦闘／攻撃／剣・終了", 0.5);
    //        }

    //        if (timer >= 100) is_end = true;
    //    }


    //    public override void Draw()
    //    {
    //        if (timer_start_wait > 0) return;

    //    }
    //}

    //public class ActionSimmple : Action
    //{
    //    int timer = 0;
    //    int timer_start_wait = 0;
    //    string sound_key;

    //    public ActionSimmple(int start_wait, string sound_key)
    //    {
    //        this.timer_start_wait = start_wait;
    //        this.sound_key = sound_key;
    //    }

    //    public override void Update()
    //    {
    //        if (timer_start_wait > 0)
    //        {
    //            timer_start_wait--;
    //            return;
    //        }


    //        timer++;

    //        if (timer == 1)
    //        {
    //            SoundManager.PlaySound( sound_key, 0.5);
    //        }
    //        if (timer >= 1) is_end = true;
    //    }


    //    public override void Draw()
    //    {
    //        if (timer_start_wait > 0) return;

    //    }
    //}

    //// 戦闘不能になる
    //public class ActionUnableToFight : Action
    //{
    //    int timer = 0;
    //    int timer_start_wait = 0;

    //    public ActionUnableToFight(int start_wait = 0)
    //    {
    //        this.timer_start_wait = start_wait;
    //    }

    //    public override void Update()
    //    {
    //        if (timer_start_wait > 0)
    //        {
    //            timer_start_wait--;
    //            return;
    //        }


    //        timer++;

    //        if (timer == 1)
    //        {
    //            SoundManager.PlaySound("戦闘／倒れる音", 0.5);
    //        }

    //        if (timer >= 100) is_end = true;
    //    }


    //    public override void Draw()
    //    {
    //        if (timer_start_wait > 0) return;

    //    }

    //}

}
