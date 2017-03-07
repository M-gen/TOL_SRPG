using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TOL_SRPG.Base;

namespace TOL_SRPG.App
{
    // 初期化用のスクリプトをうまく走らせるためのシーン
    public class SceneSetup : Scene
    {
        Script setup_script;
        bool is_setup_script_run = false;

        public SceneSetup( string path )
        {
            setup_script = new Script(path, _ScriptLineAnalyze);
            
        }

        public override void Update()
        {
            if (!is_setup_script_run)
            {
                setup_script.Run("Setup");
                is_setup_script_run = true;
            }
        }

        public override void Draw(bool is_shadowmap)
        {
        }

        public bool _ScriptLineAnalyze(Script.ScriptLineToken t)
        {
            var game_main = GameMain.GetInstance();
            //var user_interface = game_main.user_interface;
            //var game_base = game_main.game_base;
            //var g3d_camera = game_main.g3d_camera;
            var g3d_map = game_main.g3d_map;
            var unit_manager = game_main.unit_manager;
            //var action_manager = game_main.action_manager;

            switch (t.command[0])
            {
                case "SetupScene":
                    switch (t.command[1])
                    {
                        case "Battle":
                            {
                                var script_path = t.GetString(2);
                                var scene = new SceneBattle(script_path, false);
                                game_main.NextScene(scene);
                            }
                            return true;
                        case "PreBattle":
                            {
                                var script_path_pre = t.GetString(2);
                                var script_path_main = t.GetString(3);
                                var scene = new ScenePreBattle(script_path_pre, script_path_main);
                                game_main.NextScene(scene);
                            }
                            return true;
                        case "CreateMap":
                            {
                                //var script_path_pre = t.GetString(2);
                                //var script_path_main = t.GetString(3);
                                var scene = new SceneCreateMap();
                                game_main.NextScene(scene);
                            }
                            return true;
                    }
                    break;
                case "ConectSound":
                    {
                        var key_name = t.GetString(1);
                        var path = t.GetString(2);
                        SoundManager.ConectSound(key_name, path);
                    }
                    return true;
                case "ConectOnePointModel":
                    {
                        var key_name = t.GetString(1);
                        var path = t.GetString(2);
                        ModelManager.ConectOnePointModel(key_name, path);
                    }
                    return true;
                case "SetupActionData":
                    {
                        var path = t.GetString(1);
                        ActionDataManager.SetupActionData( path);
                    }
                    return true;
                case "SetupClassData":
                    {
                        var path = t.GetString(1);
                        UnitDataManager.AddClassData(path);
                    }
                    return true;
                case "DebugMode":
                    switch (t.GetString(1))
                    {
                        case "UI":
                            game_main.debug_status.is_battle_ui_debug_mode = true;
                            return true;
                        case "Auto":
                            game_main.debug_status.is_auto_battle = true;
                            game_main.user_interface.SetMode(UserInterface.Mode.NonePlayerTurn, game_main.game_base.input.mouse_sutatus.position);
                            return true;
                    }
                    return true;
            }
            return false;
        }
    }
}
