using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading.Tasks;

using DxLibDLL;

namespace DxlibGame
{
    public class SIPoint
    {
        public int x;
        public int y;
        public SIPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class SDPoint
    {
        public double x;
        public double y;
        public SDPoint( double x, double y )
        {
            this.x = x;
            this.y = y;
        }
    }

    public class SDrawImage : IDisposable
    {
        protected int image_handle = -1;
        protected int width = 0;
        protected int height = 0;

        protected SDrawImage() {}

        public SDrawImage( int image_handle )
        {
            this.image_handle = image_handle;
            DX.GetGraphSize(image_handle, out width, out height);
        }

        public void Draw(Point src_pos, double alpha)
        {
        }

        public void Draw( SDPoint src_pos, Rectangle dst_rc, double alpha, bool is_turn )
        {
            if (alpha > 1) alpha = 1;
            if (alpha <= 0) return;

            DX.SetDrawBlendMode(DX.DX_BLENDMODE_ALPHA, (int)(alpha * 255));
            int is_turn_flag = DX.FALSE;
            if (is_turn) is_turn_flag = DX.TRUE;
            DX.DrawRectGraphF((float)src_pos.x, (float)src_pos.y, dst_rc.X, dst_rc.Y, dst_rc.Width, dst_rc.Height, image_handle, DX.TRUE, is_turn_flag);
            DX.SetDrawBlendMode(DX.DX_BLENDMODE_NOBLEND, 0);
        }

        public void Dispose()
        {
            if (image_handle!=-1)
            {
                DX.DeleteGraph(image_handle);
            }
        }

        public void FilterGauss( int p1, int p2 )
        {
            DX.GraphFilter(image_handle, DX.DX_GRAPH_FILTER_GAUSS, p1, p2);
        }
    }

    public class SDrawImageByScreen : SDrawImage
    {
        public SDrawImageByScreen()
        {
            int color_bit_depth;
            DX.GetScreenState(out width, out height, out color_bit_depth);
            image_handle = DX.MakeScreen(width, height, DX.FALSE);
            DX.GetDrawScreenGraph(0, 0, width, height, image_handle);
        }

    }

}
