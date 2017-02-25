using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using DxLibDLL;

namespace TOL_SRPG.Base
{
    public class SFont : IDisposable
    {
        //-----------------------------------------------------------------------------------------
        // Static
        //-----------------------------------------------------------------------------------------
        
        class LoadFontData
        {
            public string path;
        }
        static List<LoadFontData> load_fonts = null;
        
        [DllImport("gdi32.dll")]
        static extern int AddFontResourceEx(StringBuilder font_path, uint fl, IntPtr pdv);
        [DllImport("gdi32.dll")]
        static extern int RemoveFontResourceEx(StringBuilder font_path, uint fl, IntPtr pdv);
        const int FR_PRIVATE = 0x10;
        const int STRING_MAX = 512;
        

        // 追加読み込みをさせる
        static public void Load( string font_path )
        {
            if (load_fonts == null) load_fonts = new List<LoadFontData>();

            var path = new StringBuilder(STRING_MAX);
            path.AppendFormat(font_path);
            AddFontResourceEx(path, FR_PRIVATE, (IntPtr)0);
            var lfd = new LoadFontData();
            lfd.path = font_path;
            load_fonts.Add(lfd);
        }

        // 追加読み込みしたフォントを開放する
        static public void Release()
        {
            if(load_fonts!=null)
            {
                foreach (  var lfd in load_fonts )
                {
                    var path = new StringBuilder(STRING_MAX);
                    path.AppendFormat(lfd.path);
                    RemoveFontResourceEx(path, FR_PRIVATE, (IntPtr)0);
                }
                load_fonts.Clear();
            }

        }

        //-----------------------------------------------------------------------------------------
        // Instace
        //-----------------------------------------------------------------------------------------

        int font_handle = -1;
        public enum Antialiasing
        {
            None,
            Normal,
        }

        public SFont( string font_name, int size, int thick, Antialiasing a_type, int edge_size )
        {
            int a_type_int = DX.DX_FONTTYPE_NORMAL;
            switch(a_type)
            {
                case Antialiasing.Normal: a_type_int = DX.DX_FONTTYPE_ANTIALIASING; break;

            }
            font_handle = DX.CreateFontToHandle( font_name, size, thick, a_type_int, DX.DX_CHARSET_DEFAULT, edge_size);
        }

        public void Dispose()
        {
            DX.DeleteFontToHandle(font_handle);
        }

        public int GetHandle()
        {
            return font_handle;
        }

    }
}
