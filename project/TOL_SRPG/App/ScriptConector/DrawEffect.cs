using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;

namespace TOL_SRPG.App.ScriptConector
{
    //

    public class DrawEffectScriptConector
    {
        public class Font
        {
            public SFont sfont;
            public Font( int size, int frame_size )
            {
                //: base( font_name, size, thick, Antialiasing.Normal, edge_size )
                sfont = new SFont(GameMain.main_font_name_b, size, 0, SFont.Antialiasing.Normal, frame_size);
            }
        }

        public class Color
        {
            public uint color_uint = 0;
            public int color_a = 255;
            public Color( int a, int r, int g, int b )
            {
                color_a = a;
                color_uint = DX.GetColor(r,g,b);
            }
        }

        // 文字列の描画
        // DXライブラリの都合上、透明度はmain_colorのみ参照させる
        public void Text( int x, int y, string text, Font font, Color main_color, Color frame_color )
        {
            if (main_color.color_a == 255)
            {
                DX.DrawStringFToHandle(x, y, text, main_color.color_uint, font.sfont.GetHandle(), frame_color.color_uint);
            }
            else
            {
                DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, main_color.color_a);
                DX.DrawStringFToHandle(x, y, text, main_color.color_uint, font.sfont.GetHandle(), frame_color.color_uint);
                DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);
            }
        }
    }

    public class DrawEffect : Action
    {
        PythonScript python_script;
        //Action action;
        //Effect effect;
        //Thread thread;
        //bool is_hit = true;
        //int effect_value = 0;
        //Unit target_unit = null;
        DrawEffectScriptConector draw_effect_script_conector;


        public DrawEffect(string script_path)
        {
            draw_effect_script_conector = new DrawEffectScriptConector();
            python_script = new PythonScript(script_path,
                (s) => { s.SetVariable("draw", draw_effect_script_conector); }
                );
        }

        public override void Update()
        {
            if ( python_script.script.Update()==false )
            {
                is_end = true;
            }
        }

        public override void Draw()
        {
            python_script.script.Draw();
        }


    }
}
