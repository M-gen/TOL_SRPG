using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;

namespace TOL_SRPG.App
{
    public class G3DModel : IDisposable
    {
        public class MaterialChangeData
        {
            public int color_no = 0;
            public int material_index = 0;
            public string image_path = "";
            public int image_handle = -1;
        }

        public int model_handle;
        public string model_root_dir;

        public DX.VECTOR pos;
        public DX.VECTOR rot;
        public DX.VECTOR scale;
        public float alpha = 1;
        public List<MaterialChangeData> material_change_datas = new List<MaterialChangeData>();

        public enum MotionType
        {
            DirectFrame, // フレームを直値指定
            Step,   // 徐々に進行する
        };

        class Motion
        {
            // モーションの合成はちょっとあとまわし
            public int motion_no;           // モーション番号 DxLib で自動にも見込まれる [モデル名]000.vmd など、後続する番号3桁のもの
            public int motion_handle;
            public float motion_timer;
            public float motion_timer_add;
            public float motion_time_max;
            //public bool is_loop;
            public MotionType motion_type;
        }
        List<Motion> motions = new List<Motion>();


        class ConectMotionData
        {
            public int motion_no;
            public string key;
        }
        Dictionary<string, ConectMotionData> conect_motion_datas = new Dictionary<string, ConectMotionData>();


        public G3DModel( string path )
        {
            var ss = path.Split('/');
            var sl = ss[ss.Count() - 1].Length;
            model_root_dir = path.Substring(0, path.Length - sl);
            model_handle = DX.MV1LoadModel(path);


        }

        public void AddMotion(int no, float motion_timer_add)
        {
            // 同じモーションがある場合キャンセル
            // このあたりはとりあえず適当。。。合成もろくに考えてないので
            foreach (Motion m2 in motions)
            {
                if (m2.motion_no == no)
                {
                    return;
                }
            }

            var m = new Motion();
            m.motion_no = no;
            m.motion_handle = DX.MV1AttachAnim(model_handle, no);
            m.motion_timer = 0.0f;
            m.motion_timer_add = motion_timer_add;
            m.motion_time_max = DX.MV1GetAnimTotalTime(model_handle, m.motion_handle);
            m.motion_type = MotionType.Step;

            if (m.motion_handle > -1)
            {
                // err
            }

            motions.Add(m);
        }

        public void AddMotion( string key, float motion_timer_add )
        {
            if (!conect_motion_datas.ContainsKey(key)) return;
            var cmd = conect_motion_datas[key];
            AddMotion(cmd.motion_no, motion_timer_add);
        }

        // モーションを指定するが、フレーム(時刻)を直接指定する
        public void SetMotionDirectFrame(int no, float frame)
        {
            // 同じモーションがある場合はそれを削除
            DeleteMotion(no);

            var m = new Motion();
            m.motion_no = no;
            m.motion_handle = DX.MV1AttachAnim(model_handle, no);
            m.motion_timer = frame;
            m.motion_timer_add = 0.0f;
            m.motion_time_max = DX.MV1GetAnimTotalTime(model_handle, m.motion_handle);
            m.motion_type = MotionType.DirectFrame;

            if (m.motion_handle > -1)
            {
                // err
            }

            motions.Add(m);

        }

        public void SetMotionDirectFrame(string key, float frame)
        {
            if (!conect_motion_datas.ContainsKey(key)) return;
            var cmd = conect_motion_datas[key];
            SetMotionDirectFrame(cmd.motion_no, frame);
        }

        public void DeleteMotion(int no)
        {
            foreach (Motion m in motions)
            {
                if (m.motion_no == no)
                {
                    DX.MV1DetachAnim(model_handle, m.motion_handle);
                    motions.Remove(m);
                    break;
                }
            }
        }

        public void DeleteMotion(string key)
        {
            if (!conect_motion_datas.ContainsKey(key)) return;
            var cmd = conect_motion_datas[key];
            DeleteMotion(cmd.motion_no);
        }

        public void Update()
        {
            foreach (Motion m in motions)
            {
                switch (m.motion_type) {
                    case MotionType.Step:
                        DX.MV1SetAttachAnimTime(model_handle, m.motion_handle, m.motion_timer);
                        m.motion_timer += m.motion_timer_add;//0.16f * 2.50f;
                        if (m.motion_timer > m.motion_time_max) m.motion_timer = 0.0f;
                        break;
                    case MotionType.DirectFrame:
                        DX.MV1SetAttachAnimTime(model_handle, m.motion_handle, m.motion_timer);
                        break;
                }
            }
        }

        public void Draw()
        {

            //foreach (SubModel sub_model in sub_models)
            //{
            //    // 行列合成
            //    DX.MATRIX RightHandMatrix;
            //    RightHandMatrix = DX.MV1GetFrameLocalWorldMatrix(model_handle, sub_model.conect_frame_no);
            //    RightHandMatrix = DX.MMult(DX.MGetScale(DX.VGet(0.05f, 0.05f, 0.05f)), RightHandMatrix);
            //    RightHandMatrix = DX.MMult(DX.MGetRotAxis(DX.VGet(-1.0f, -0.3f, 0.5f), 1.2f), RightHandMatrix);

            //    DX.MV1SetMatrix(sub_model.model.model_handle, RightHandMatrix); // sub_model.model.Draw()でやれたほうがよさ気
            //    DX.MV1DrawModel(sub_model.model.model_handle);

            //}
            DX.MV1DrawModel(model_handle);
            

        }

        public void Pos(float x, float y, float z)
        {
            pos = DX.VGet(x, y, z);
            DX.MV1SetPosition(model_handle, pos);
        }

        public void Rot(float x, float y, float z)
        {
            rot = DX.VGet(x, y, z);
            DX.MV1SetRotationXYZ(model_handle, rot);
        }

        public void RotAdd(float x, float y, float z)
        {
            rot = DX.VGet(rot.x + x, rot.y + y, rot.z + z);
            DX.MV1SetRotationXYZ(model_handle, rot);
        }

        public void Scale(float x, float y, float z)
        {
            scale = DX.VGet(x, y, z);
            DX.MV1SetScale(model_handle, scale);
        }

        // 透明度
        public void Alpha( float a)
        {
            alpha = a;
            DX.MV1SetOpacityRate(model_handle, a);
        }

        public void SetDifColorScale(float r, float g, float b, float a)
        {
            DX.MV1SetDifColorScale(model_handle, DX.GetColorF(r, g, b, a));
        }

        public void ChangeTexture( int no )
        {
            foreach (var mcd in material_change_datas)
            {
                if (mcd.color_no != no) continue;

                if (mcd.image_handle == -1)
                {
                    mcd.image_handle = DX.LoadGraph(model_root_dir + mcd.image_path); ;
                }
                DX.MV1SetTextureGraphHandle(model_handle, mcd.material_index, mcd.image_handle, DX.FALSE);
            }
        }

        public void AddChangeTextureData( int no, int material_index, string texture_path )
        {
            var mcd = new MaterialChangeData();
            mcd.color_no       = no;
            mcd.material_index = material_index;
            mcd.image_path     = texture_path;
            material_change_datas.Add(mcd);
        }

        public void AddConectMotionData( string key, int no )
        {
            var cmd = new ConectMotionData();
            cmd.key = key;
            cmd.motion_no = no;
            conect_motion_datas.Add(key, cmd);
        }

        public virtual void Dispose()
        {
            foreach( var mcd in material_change_datas )
            {
                if(mcd.image_handle>0)
                {
                    DX.DeleteGraph(mcd.image_handle);
                }
            }
            DX.MV1DeleteModel(model_handle);
        }
    }

    // 再利用を考慮して運用される3Dモデル
    // 利用が終わったら、明示的にReleaseを呼ぶこと
    public class OnePointModel : G3DModel
    {
        //public int handle;
        public bool is_rent = false;

        public OnePointModel( string path ) : base(path)
        {
        }

        public void Release()
        {
            is_rent = false;
        }

    }

    public class ModelManager
    {
        public class OnePointModelData
        {
            public string key;  // 直接呼び出さない
            public string path; // 
            public List<OnePointModel> models = new List<OnePointModel>();
            public int stock_max; // 蓄積最大数、Release時にチェックさせる予定
        }

        static ModelManager model_manager = null;
        Dictionary<string, OnePointModelData> one_point_models = new Dictionary<string, OnePointModelData>();


        public ModelManager()
        {
            model_manager = this;
        }

        public static OnePointModel RentModel(string model_key)
        {
            if (!model_manager.one_point_models.ContainsKey(model_key))
            {
                // 存在しない ?
                return null;
            }
            var md = model_manager.one_point_models[model_key];

            foreach( var m in md.models)
            {
                if ( m.is_rent == false)
                {
                    m.is_rent = true;
                    return m;
                }
            }

            {
                var m = new OnePointModel(md.path);
                m.is_rent = true;
                md.models.Add(m);
                return m;
            }
        }

        public static void ConectOnePointModel(string model_key, string model_path, int stock_max = 1)
        {
            foreach( var opm in model_manager.one_point_models)
            {
                if(opm.Key==model_key)
                {
                    // todo: Warningログ出しときたい
                    // 重複しているのでここでもどす
                    opm.Value.path = model_path;
                    opm.Value.stock_max = stock_max;
                    return;
                }
            }

            var md = new OnePointModelData();
            md.key = model_key;
            md.path = model_path;
            md.stock_max = stock_max;

            model_manager.one_point_models.Add(md.key, md);
        }

    }
}
