using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOL_SRPG.App
{
    public class ActionData
    {
        public string view_name = "";
        public string system_name = "";
        public string type = "";
        public int range_min = 1;
        public int range_max = 1;
        public string range_type    = "近接";
        public string range_ok_type = "範囲内";

        // ダメージや効果量
        public string effect_src_main = "";            // 行動側の参照する能力値
        public int    effect_src_main_correction =  0; // 補正 %
        public int    effect_src_direct = 0;           // 直接値指定
        public int    effect_src_random = 0;           // 乱数指定(最大値を指定)
        public int    effect_src_range_correction = 0; // 距離(マス)補正 %
        public string effect_dst_main = "";            // 受け側の参照する能力値
        public int    effect_dst_main_correction = 0;  // 補正 

        Script script;

        public ActionData( string script_path )
        {
            script = new Script(script_path, _ScriptLineAnalyze);
            script.Run("Setup");

        }

        public bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            switch(t.command[0])
            {
                case "$":
                    switch(t.command[1])
                    {
                        case "SystemName":  system_name  = t.GetString(2); break;
                        case "ViewName":    view_name    = t.GetString(2); break;
                        case "Type":        type         = t.GetString(2); break;
                        case "Range":
                            range_min = t.GetInt(2);
                            range_max = t.GetInt(3);
                            range_type = t.GetString(4);
                            range_ok_type = t.GetString(5);
                            break;
                        case "EffectValue":
                            effect_src_main             = t.GetString(2);
                            effect_src_main_correction  = t.GetInt(3);
                            effect_src_direct           = t.GetInt(4);
                            effect_src_random           = t.GetInt(5);
                            effect_src_range_correction = t.GetInt(6);
                            effect_dst_main             = t.GetString(7);
                            effect_dst_main_correction  = t.GetInt(8);
                            break;
                    }
                    break;

            }

            return false;
        }
    }

    public class ActionDataManager
    {
        static ActionDataManager action_data_manager;
        List<ActionData> action_datas = new List<ActionData>();

        public ActionDataManager()
        {
            action_data_manager = this;
        }

        static public void SetupActionData( string script_path)
        {
            // 一旦、重複がないかチェックした方がいい気もするが…nameは読み込まないと判明しないか

            var ad = new ActionData(script_path);
            action_data_manager.action_datas.Add(ad);
        }

        static public ActionData GetActionData( string system_name)
        {
            foreach ( var ad in action_data_manager.action_datas)
            {
                if (ad.system_name == system_name)
                {
                    return ad;
                }
            }
            return null;
        }

    }
}
