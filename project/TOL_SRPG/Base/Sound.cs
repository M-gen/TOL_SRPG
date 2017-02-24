using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;

namespace DxlibGame
{
    // 音管理
    // staticを使うので複数存在できないが、導線を作るのが億劫なので

    class SoundManager
    {
        static SoundManager sound_manager = null;
        
        public class Sound
        {
            public string key;  // 直接サウンドを呼び出さない
            public string path; // 
            public int handle; 
        }
        Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();

        public SoundManager()
        {
            sound_manager = this;
        }

        public static void PlaySound(string sound_key, double volume = 1.0)
        {
            var v = (int)(255 * volume);
            if (v > 255) v = 255;
            else if (v < 0) return;

            if (!sound_manager.sounds.ContainsKey(sound_key))
            {
                // 存在しない ?
                return;
            }
            var s = sound_manager.sounds[sound_key];

            DX.ChangeNextPlayVolumeSoundMem(v, s.handle);
            DX.PlaySoundMem( s.handle, DX.DX_PLAYTYPE_BACK);
        }

        public static void ConectSound(string sound_key, string sound_path)
        {
            if (sound_manager.sounds.ContainsKey(sound_key))
            {
                return;
            }
            var s = new Sound();
            s.key = sound_key;
            s.path = sound_path;
            s.handle = DX.LoadSoundMem(sound_path);

            sound_manager.sounds.Add(s.key, s);
        }
    }
}
