using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DxLibDLL;
using TOL_SRPG.Base;

namespace TOL_SRPG.App.Map
{
    // 画像をつかう素材データ
    public class MapImageMaterial : IDisposable
    {
        public string key;
        public string image_path;
        public int image_handle = -1;

        public MapImageMaterial(string key, string image_path)
        {
            this.key = key;
            this.image_path = image_path;

            image_handle = DX.LoadGraph(image_path);
        }

        public virtual void Dispose()
        {
            if (image_handle != -1)
            {
                DX.DeleteGraph(image_handle);
            }
        }
    }

    // 壁面データ
    public class MapWallMaterial : MapImageMaterial
    {
        public MapWallMaterial(string key, string image_path) : base(key, image_path)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    // 地形データ
    public class MapGroundMaterial : MapImageMaterial
    {
        public MapGroundMaterial(string key, string image_path) : base(key, image_path)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }

    // 素材管理
    public class MapMaterialManager : IDisposable
    {

        public Dictionary<string, MapGroundMaterial> ground_materials = new Dictionary<string, MapGroundMaterial>();
        public Dictionary<string, MapWallMaterial> wall_materials = new Dictionary<string, MapWallMaterial>();

        public MapMaterialManager()
        {

        }

        public MapGroundMaterial AddGroundMaterial(string key, string image_path)
        {
            var gm = new MapGroundMaterial(key, image_path);
            ground_materials.Add(key, gm);
            return gm;
        }

        public MapWallMaterial AddWallMaterial(string key, string image_path)
        {
            var wm = new MapWallMaterial(key, image_path);
            wall_materials.Add(key, wm);
            return wm;
        }

        public void Dispose()
        {
            foreach (var v in ground_materials) v.Value.Dispose();
            foreach (var v in wall_materials) v.Value.Dispose();
        }
    }
}
