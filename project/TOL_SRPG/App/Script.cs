using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxlibGameSimRPG
{
    // オリジナルスクリプトエンジン
    public class Script
    {
        public class ScriptLineToken
        {
            public int line_index = 0; // 行数
            public string[] command;
            //public string param;

            public List<ScriptLineToken> token_code_tree = null; // 全部にインスタンスはいらない？= new List<ScriptLineToken>();
            public ScriptLineToken parent = null; // 親
            public Script owner_script = null;

            public string GetString( int index)
            {
                return owner_script.GetCommandString( this, index);
            }

            public int GetInt( int index)
            {
                return owner_script.GetCommandInt(this, index);
            }
        }

        public delegate bool ScriptLineAnlyze(ScriptLineToken t); // 戻り値がtrueだと「処理済み」という扱い

        public class Value
        {
            public int _int = 0;
            public string _string = "";
        }

        string script_path;
        List<ScriptLineToken> tokens = new List<ScriptLineToken>();          // スクリプトデータの本体
        List<ScriptLineToken> token_code_tree = new List<ScriptLineToken>(); // コードを解析してツリー状に体系化したもの、実行時に利用する
        Dictionary<string, ScriptLineToken> token_def_function_top = new Dictionary<string, ScriptLineToken>(); // 関数の呼び出し開始トークン
        Dictionary<string, Value> values = new Dictionary<string, Value>();
        ScriptLineAnlyze sub_line_analyze = null;

        public Script( string script_path, ScriptLineAnlyze sub_line_analyze=null )
        {
            this.script_path = script_path;
            this.sub_line_analyze = sub_line_analyze;

            // スクリプトの単純な読み込み（トークン分けはおおなう）
            var file = new System.IO.StreamReader(script_path);

            string line;
            var i = 0;
            while ((line = file.ReadLine()) != null)
            {
                var t = new ScriptLineToken();
                t.command = SplitScriptLine(line);
                t.line_index = i;
                t.owner_script = this;
                tokens.Add(t);

                i++;
            }
            file.Close();

            // 構造を分析してツリー構造へ（処理順を実行時に解釈・処理しやすくする）
            var parent_token_stack = new List<ScriptLineToken>();
            ScriptLineToken parent_token = null;
            foreach( var t in tokens )
            {
                if (t.command.Count() == 0) continue;


                switch (t.command[0])
                {
                    case "def": // 関数の定義開始宣言
                        token_def_function_top.Add(t.command[1], t);
                        if (parent_token!=null)
                        {
                            parent_token_stack.Clear();
                        }
                        parent_token = t;
                        parent_token.token_code_tree = new List<ScriptLineToken>();
                        break;
                    default:
                        if (parent_token != null)
                        {
                            t.parent = parent_token;
                            parent_token.token_code_tree.Add(t);
                        }
                        else
                        {
                            // err
                        }
                        break;
                        //case "$":
                        //    break;
                }
            }
        }

        string[] SplitScriptLine( string line )
        {
            var s = new List<string>();

            if ( line.Count() == 0 )
            {
                return s.ToArray();
            }

            // 前方のタブと半角スペースを削除する
            {
                if ( line[0]==' ' || line[0]=='\t' ) {
                    var i = 0;
                    foreach (var c in line)
                    {
                        if (c == ' ' || c == '\t')
                        {

                        }
                        else 
                        {
                            line = line.Substring(i);
                            break;
                        }
                        i++;
                    }
                }
            }

            // 半角スペースで分離しつつ"での文字列としてまとめる
            {
                var i = 0;
                var cut_start = 0;
                var cut_end = 0;
                var is_cut = true;       // カット中=終端を探している
                var is_cut_text = false; // "の終端をさがしてカット中
                foreach (var c in line)
                {
                    //Console.WriteLine(c + " " + i);
                    if (is_cut)
                    {
                        if (c == ' ' || c == '\t')
                        {
                            cut_end = i;
                            is_cut = false;
                            var tmp = line.Substring(cut_start, cut_end - cut_start);
                            //Console.WriteLine("cut " + tmp);
                            s.Add(tmp);
                        }

                    }
                    else if (is_cut_text)
                    {
                        if (c == '"')
                        {
                            cut_end = i+1;
                            is_cut_text = false;
                            var tmp = line.Substring(cut_start, cut_end- cut_start);
                            //Console.WriteLine("cut_text " + tmp);
                            s.Add(tmp);
                        }
                    }
                    else
                    {
                        if (c == ' ' || c == '\t')
                        {
                            // 半角スペースとタブが続くのでカットできない
                        }
                        else if ( c=='"' )
                        {
                            // "による文字扱いなので、文字カット開始
                            is_cut_text = true;
                            cut_start = i;

                        }
                        else
                        {
                            // 通常文字なのでカット開始
                            is_cut = true;
                            cut_start = i;
                        }
                    }
                    i++;
                }
                if ( is_cut || is_cut_text)
                {
                    cut_end = i;
                    var tmp = line.Substring(cut_start, cut_end - cut_start);
                    //Console.WriteLine("cut " + tmp);
                    s.Add(tmp);
                }
            }

            // コメント部分を削除（残しておくと処理に困るので）
            {
                var i = 0;
                foreach (var str in s)
                {
                    if (str.IndexOf("//") == 0)
                    {
                        var size = s.Count();
                        for ( var j=i; j< size; j++)
                        {
                            s.RemoveAt(i);
                        }
                        break;
                    }
                    i++;
                }
            }

            return s.ToArray();
        }

        // 両端のゴミを削除、（チェックしてないけど
        public string TrimString( string str )
        {
            var len = str.Length;
            str = str.Substring(1, len - 2);
            return str;
        }

        public void Run( string function_name )
        {
            if (!token_def_function_top.ContainsKey(function_name)) return; // 存在しない

            ScriptLineToken t_head = token_def_function_top[function_name];
            if (t_head == null) return;

            var counter_stack = new List<int>();
            int counter = 0;
            ScriptLineToken t_pos = t_head.token_code_tree[counter];
            counter++;

            while (t_pos != null )
            {
                switch(t_pos.command[0])
                {
                    //case "$":
                    //    Console.WriteLine(ExpansionString(t_pos.command));
                    //    break;
                    default:
                        Run_OneLine(t_pos);
                        break;
                }

                if (counter >= t_head.token_code_tree.Count())
                {
                    if (t_pos.parent.parent==null)
                    {
                        t_head = null;
                        t_pos = null;
                    }
                    else
                    {
                        counter = counter_stack[0]; counter_stack.RemoveAt(0);
                        t_pos = t_head.token_code_tree[counter];
                        counter++;
                    }
                }
                else
                {
                    t_pos = t_head.token_code_tree[counter];
                    counter++;
                }
            }
        }

        void Run_OneLine(ScriptLineToken t )
        {
            if (sub_line_analyze != null)
            {
                if (sub_line_analyze(t)) return; // true が帰ってきてるので、処理済みとして扱う
            }
        }

        // ログ表示用に配列の文字列をくっつける
        string ExpansionString( string[] strs )
        {
            var s = "";

            var i = 0;
            foreach( var ss in strs )
            {
                if (i > 0) s += " ";
                s += ss;
                i++;
            }

            return s;
        }

        public string GetCommandString(ScriptLineToken t, int index)
        {
            string v = t.command[index];
            v = TrimString(v);
            return v;
        }

        public int GetCommandInt(ScriptLineToken t, int index)
        {
            return int.Parse(t.command[index]);
        }

    }
}
