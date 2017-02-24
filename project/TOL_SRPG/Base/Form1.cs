using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using DxLibDLL;
using DxlibGame;

namespace DxlibGameSimRPG
{
    public partial class Form1 : Form
    {
        GameBase game_base; // DxLibを駆動させる基礎部分をまとめたもの（まとめきれない部分はFromやProgramにこぼれてる）
        GameMain game_main; // ゲームごとにオリジナルで作成する、メインループの指定をgame_base側に設定する必要がある

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            game_base = new GameBase( this, 1024, 720, null, true );
            game_main = new GameMain(game_base);                      // DxLib初期化後にこちらを処理したいので
            game_base.dlg_main_loop_one_frame = game_main.MainLoop;   // 上記の理由につきやや強引（引数でやりたいところ）
        }

        public void MainLoop()
        {
            game_base.MainLoop(); // 回りくどいけど
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            game_base.Dispose(); // 解放処理など
        }

    }
}
