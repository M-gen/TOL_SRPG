using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


using DxLibDLL;
using TOL_SRPG.Base;
using TOL_SRPG.App.Map;

namespace TOL_SRPG.App
{
    public class GameMain : GameMainBase
    {
        static GameMain static_main = null;

        public GameBase game_base;
        public const int WindowSizeW = 1024; // 別箇所でも定義してるから微妙なのだが
        public const int WindowSizeH = 720;
        public const string main_font_name_r = "MigMix 2P"; // 主に使うレギュラー（通常の細線）フォント名
        public const string main_font_name_b = "MigMix 1P"; // 主に使うボールドのフォント

        SoundManager sound_manager; // 
        ModelManager model_manager;
        ActionDataManager action_data_manager;

        public BattleMap g3d_map;
        public G3DCamera g3d_camera;
        public UserInterface user_interface;
        int ShadowMapHandle = 0;
        public UnitManager unit_manager = new UnitManager();
        public ActionManager action_manager = new ActionManager();
        public System.Random random = new System.Random();
        public STaskManager stask_manager = new STaskManager();
        public UnitDataManager unit_data_manager = new UnitDataManager();

        public ScriptConector.BattleMapEffectScriptConectorManager battle_map_effect_script_conector_manager = new ScriptConector.BattleMapEffectScriptConectorManager();

        bool is_use_shadowmap = true;
        public bool is_shadowmap_draw = false; // シャドウマップ中の描画かどうか

        public enum KeyCode{
            DebugView,
        };
        public bool debug_is_view = false;

        public Scene scene = null;

        public class DebugStatus
        {
            public bool is_battle_ui_debug_mode = false;    // WT無視、コマンドにデバック用を付与
            public bool is_auto_battle = false;             // 味方ユニットもAIで動く
        }
        public DebugStatus debug_status = new DebugStatus();

        public GameMain(GameBase game_base)
        {
            // 根本的な初期化(ここを先にしておかないと差し支えるもの)
            static_main = this;
            game_base.game_main = this;
            this.game_base = game_base;
            
            SFont.Load("data/font/migmix-1p-20150712/migmix-1p-bold.ttf");
            SFont.Load("data/font/migmix-2p-20150712/migmix-2p-regular.ttf");
            //

            // その他の初期化、基本部分
            sound_manager = new SoundManager();
            model_manager = new ModelManager();
            action_data_manager = new ActionDataManager();

            // シーン生成
            scene = new SceneSetup("data/script/start.nst");

            // キー入力初期化
            game_base.input.SetupKeyInput((int)KeyCode.DebugView, DX.KEY_INPUT_F3);
            
            user_interface = new UserInterface();
            

            DX.SetUseZBufferFlag(DX.TRUE);   // Zバッファを有効にする
            DX.SetWriteZBufferFlag(DX.TRUE);
            //DX.SetUseLighting(DX.FALSE); // ライトを無効にする ライトの調整が難しいので・・・
            DX.SetUseLighting(DX.TRUE);

            //BGM テスト
            //var sound = DX.LoadSoundMem("data/sound/Battle_Boss_01.ogg");
            //DX.PlaySoundMem(sound, DX.DX_PLAYTYPE_LOOP);

            if (is_use_shadowmap)
            {
                // シャドウマップ初期化
                int shadow_map_size = 1024;
                ShadowMapHandle = DX.MakeShadowMap(shadow_map_size, shadow_map_size);
            }
        }

        static public GameMain GetInstance()
        {
            return static_main;
        }

        public void MainLoop()
        {
            stask_manager.Update();

            if ( scene!=null)
            {
                scene.Update();
            }

            // ライト
            //DX.ChangeLightTypePoint(DX.VGet(0.0f, 300.0f, 0.0f), 2000.0f, 0.040f, 0.0025f, 0.000f);
            DX.ChangeLightTypePoint(DX.VGet(50.0f, 300.0f, 50.0f), 2000.0f, 0.040f, 0.0025f, 0.000f);

            // 影
            if (is_use_shadowmap)
            {
                //DX.SetShadowMapLightDirection(ShadowMapHandle, DX.VGet(20.0f, -50.0f, 20.0f));
                DX.SetShadowMapLightDirection(ShadowMapHandle, DX.VGet(-20.0f, -50.0f, 20.0f));
                DX.SetShadowMapDrawArea(ShadowMapHandle, DX.VGet(-10.0f, 0.0f, -10.0f), DX.VGet(150.0f, 10.0f, 150.0f));
                //DX.SetShadowMapAdjustDepth(ShadowMapHandle, 0.1000f); // 初期値 0.002f 
                DX.SetShadowMapAdjustDepth(ShadowMapHandle, 0.002f); // 初期値 0.002f 
            }

            if (is_use_shadowmap)
            {
                DX.ShadowMap_DrawSetup(ShadowMapHandle);
                MainLoop_Draw3DObjects( true);
                DX.ShadowMap_DrawEnd();
            }

            DX.ClearDrawScreen();
            DX.SetUseShadowMap(0, ShadowMapHandle);

            //
            if (debug_is_view){
                uint color = DX.GetColor(255, 255, 255);
                uint colorR = DX.GetColor(255, 0, 0);
                uint colorG = DX.GetColor(0, 255, 0);
                uint colorB = DX.GetColor(0, 0, 255);
                var w = 10;
                var num = 10;
                for (int x = 0; x < num; x++)
                {
                    DX.DrawLine3D(DX.VGet(0, 0, 0 + x * w), DX.VGet(w * num, 0, 0 + x * w), color);
                    DX.DrawLine3D(DX.VGet(0 + x * w, 0, 0), DX.VGet(0 + x * w, 0, w * num), color);
                }
                DX.DrawLine3D(DX.VGet(0, 0, 0), DX.VGet(300, 0, 0), colorR);
                DX.DrawLine3D(DX.VGet(0, 0, 0), DX.VGet(0, 300, 0), colorG);
                DX.DrawLine3D(DX.VGet(0, 0, 0), DX.VGet(0, 0, 300), colorB);
            }

            MainLoop_Draw3DObjects( false );


            // 確認ようにシャドウマップの描画
            if (is_use_shadowmap)
            {
                DX.SetUseShadowMap(0, -1);

                var screen = new SDrawImageByScreen();

                DX.ClearDrawScreen();
                MainLoop_Draw3DObjects(false);

                var alpha = 128;
                screen.Draw(new SDPoint(0, 0), new Rectangle(0, 0, GameMain.WindowSizeW, GameMain.WindowSizeH), (double)alpha / 255.0, false);
                screen.Dispose();

                //DX.TestDrawShadowMap(ShadowMapHandle, 5, 300, 5 + 100, 300 + 100);
            }

            DrawDebug();
        }

        void MainLoop_Draw3DObjects(bool is_shadowmap)
        {
            if (is_shadowmap) is_shadowmap_draw = true;
            else is_shadowmap_draw = false;

            if ( scene!=null)
            {
                scene.Draw(is_shadowmap);
            }
        }

        public void NextScene( Scene scene )
        {
            this.scene = scene;
        }

        public override void Release()
        {
            stask_manager.Dispose();
        }

        public void DrawDebug()
        {
            if (!debug_is_view) return;

            game_base.DrawGameBaseUI();

            // マップ系のデバック
            if ( g3d_map!=null )
            {
                if ( g3d_map.route_as_shoot!=null)
                {
                    var ox = 8;
                    var oy = 40;

                    for( var i=0; i< g3d_map.route_as_shoot.height_map_xz.Count(); i++ )
                    {
                        var x1 = ox + i % g3d_map.route_as_shoot.width_x;
                        var y1 = oy + i / g3d_map.route_as_shoot.width_x;
                        var x2 = x1 + 1;
                        var y2 = y1 + 1;
                        var h = g3d_map.route_as_shoot.height_map_xz[i] * 1 + 40;
                        var cr = h;
                        if (cr < 0) cr = 0;
                        if (cr > 255) cr = 255;
                        var cg = h - 255;
                        if (cg < 0) cg = 0;
                        if (cg > 255) cg = 255;
                        var cb = h - 255 * 2;
                        if (cb < 0) cb = 0;
                        if (cb > 255) cb = 255;

                        var color = DX.GetColor(cr, cg, cb);
                        DX.DrawBox(x1, y1, x2, y2, color, DX.TRUE);
                    }

                    // 開始位置-終端位置
                    {
                        foreach(var p in g3d_map.route_as_shoot.route_pos_list_xz)
                        {
                            var x1 = ox + p.X - g3d_map.route_as_shoot.offset_x;
                            var y1 = oy + p.Y - g3d_map.route_as_shoot.offset_z;
                            var x2 = x1 + 1;
                            var y2 = y1 + 1;
                            var color = DX.GetColor(128, 128, 128);
                            DX.DrawBox(x1, y1, x2, y2, color, DX.TRUE);
                        }
                    }
                    {
                        var x1 = ox + g3d_map.route_as_shoot.start_x - g3d_map.route_as_shoot.offset_x;
                        var y1 = oy + g3d_map.route_as_shoot.start_z - g3d_map.route_as_shoot.offset_z;
                        var x2 = x1 + 1;
                        var y2 = y1 + 1;
                        var color = DX.GetColor(0, 255, 0);
                        DX.DrawBox(x1, y1, x2, y2, color, DX.TRUE);
                    }
                    {
                        var x1 = ox + g3d_map.route_as_shoot.end_x - g3d_map.route_as_shoot.offset_x;
                        var y1 = oy + g3d_map.route_as_shoot.end_z - g3d_map.route_as_shoot.offset_z;
                        var x2 = x1 + 1;
                        var y2 = y1 + 1;
                        var color = DX.GetColor(255, 255, 255);
                        DX.DrawBox(x1, y1, x2, y2, color, DX.TRUE);
                    }

                    ox += g3d_map.route_as_shoot.width_x + 4;
                    try
                    {
                        // 断面図
                        {
                            for (var i = 0; i < g3d_map.route_as_shoot.height_map_route.Count(); i++)
                            {
                                var x1 = ox + i % g3d_map.route_as_shoot.width_height_map;
                                var y1 = oy + i / g3d_map.route_as_shoot.width_height_map;
                                var x2 = x1 + 1;
                                var y2 = y1 + 1;
                                var color = DX.GetColor(32, 0, 0);
                                var h = g3d_map.route_as_shoot.height_map_route[i];
                                if (h == -1) color = DX.GetColor(128, 0, 0);
                                if (h == 0) color = DX.GetColor(128, 128, 0);
                                DX.DrawBox(x1, y1, x2, y2, color, DX.TRUE);
                            }

                            for (var i = 0; i < g3d_map.route_as_shoot.route_pos_list_route.Count(); i++)
                            {
                                var p = g3d_map.route_as_shoot.route_pos_list_route[i];
                                var x1 = ox + p.X;
                                var y1 = oy + p.Y;
                                var x2 = x1 + 1;
                                var y2 = y1 + 1;
                                var color = DX.GetColor(255, 255, 255);
                                DX.DrawBox(x1, y1, x2, y2, color, DX.TRUE);

                            }
                        }
                    }
                    catch
                    {

                    }

                    {
                        var d = g3d_map.route_as_shoot.direction;
                        var dr = d *  180.0 / Math.PI;
                        var color = DX.GetColor(255, 255, 0);
                        DX.DrawString(8, 200, d.ToString("0.000"), color);
                        DX.DrawString(8, 200 + 20, dr.ToString("0.000"), color);
                    }

                }

            }

            // ユニット系のデバック
            var scene = this.scene as SceneBattle;
            if ( scene!=null )
            {
                //unit_manager
                var i = 0;
                foreach( var u in unit_manager.units )
                {
                    var x = 850;
                    var y = i * 22 + 8;
                    var color = DX.GetColor(255, 255, 255);
                    DX.DrawString(x, y, u.unit.name, color);

                    DX.DrawString(x+120, y, u.unit.bt.status["WT"].now.ToString(), color);
                    i++;
                }
            }
        }
    }
}
