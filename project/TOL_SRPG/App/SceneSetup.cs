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
                                var script_path_pre  = t.GetString(2);
                                var script_path_main = t.GetString(3);
                                var scene = new ScenePreBattle(script_path_pre, script_path_main);
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
                    break;
                case "ConectOnePointModel":
                    {
                        var key_name = t.GetString(1);
                        var path = t.GetString(2);
                        ModelManager.ConectOnePointModel(key_name, path);
                    }
                    break;
                case "SetupActionData":
                    {
                        var path = t.GetString(1);
                        ActionDataManager.SetupActionData( path);
                    }
                    break;
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
                    break;
                //case "MapSetup":
                //    {
                //        setup_script_data.map_h = setup_script_data.map_data.Count() / setup_script_data.map_w;
                //        g3d_map.map_w = setup_script_data.map_w;
                //        g3d_map.map_h = setup_script_data.map_h;

                //        g3d_map.Setup(setup_script_data.map_data.ToArray());

                //    }
                //    break;
                case "Unit":
                    {
                        var x = t.GetInt(1);
                        var y = t.GetInt(2);
                        var model_path = "data/model/" + t.GetString(3) + "/_.pmd";
                        var image_face_path = "data/image/face/" + t.GetString(4);
                        var name = t.GetString(5);
                        var group = t.GetString(6);
                        var color_no = t.GetInt(7);
                        var direction = t.GetInt(8);
                        var unit = new Unit(model_path, image_face_path, name, x, y, color_no, direction);
                        unit_manager.Join(unit, group);

                    }
                    //unit_manager.Join(new Unit(path, 3, 5, 1, 0), "敵");
                    return true;
                    //case "DebugMode":
                    //    if (t.GetString(1) == "True")
                    //    {
                    //        is_debug_mode = true;
                    //    }
                    //    else
                    //    {
                    //        is_debug_mode = false;
                    //    }
                    //    break;
            }
            return false;
        }
    }
}
