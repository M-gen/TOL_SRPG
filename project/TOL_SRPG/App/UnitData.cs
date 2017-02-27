using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOL_SRPG.App
{
    public class UnitClassData
    {
        public class ClassStatus
        {
            public string key;
            public int default_value; //
            public int levelup_add;    // LvUpでの増加量、これを÷10して適応する予定

            public ClassStatus(string key, int default_value, int levelup_add)
            {
                this.key = key;
                this.default_value = default_value;
                this.levelup_add = levelup_add;
            }
        }

        public class ActionStatus
        {
            public string system_name = "";
            public int reflect_point = 100; // 行動の実力発揮%(100=100%)

            public ActionStatus( string system_name, int reflect_point)
            {
                this.system_name = system_name;
                this.reflect_point = reflect_point;
            }
        }


        public string class_name = "";
        public string image_default_path = "";
        public string model_default_path = "";
        public int exp_bonus = 0;
        public List<ActionStatus> actions = new List<ActionStatus>();
        public Dictionary<string, ClassStatus> status = new Dictionary<string, ClassStatus>();

        Script script;

        public UnitClassData( string script_path )
        {
            script = new Script(script_path, _ScriptLineAnalyze);
            script.Run("Setup");
        }



        bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            switch (t.command[0])
            {
                case "$":
                    {
                        switch (t.command[1])
                        {
                            case "ClassName":
                                class_name = t.GetString(2);
                                return true;
                            case "DefaultImage":
                                image_default_path = t.GetString(2);
                                return true;
                            case "DefaultModel":
                                model_default_path = t.GetString(2);
                                return true;
                            case "Action":
                                {
                                    var a = new ActionStatus(t.GetString(2), t.GetInt(3));
                                    actions.Add(a);
                                }
                                return true;
                            case "ExpBonus":
                                exp_bonus = t.GetInt(2);
                                return true;
                            default:
                                {
                                    var cs = new ClassStatus(t.GetString(1), t.GetInt(2), t.GetInt(3));
                                    status.Add(t.command[1], cs);
                                }
                                return true;
                        }
                    }
            }
            return false;
        }

    }

    public class UnitDataManager
    {
        static UnitDataManager unit_data_manager = null;

        List<UnitClassData> unit_class_datas = new List<UnitClassData>();

        public UnitDataManager()
        {
            unit_data_manager = this;
        }

        static public void AddClassData( string script_path )
        {
            var ucd = new UnitClassData(script_path);
            unit_data_manager.unit_class_datas.Add(ucd);
        }

        static public UnitClassData GetUnitClassData( string class_name)
        {
            foreach( var ucd in unit_data_manager.unit_class_datas)
            {
                if (ucd.class_name == class_name) return ucd;
            }
            return null;
        }


    }

}
