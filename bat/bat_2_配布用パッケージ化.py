import os
import shutil
import os.path

target_dir = "release/"
new_dir_name = "TOL_RPG_0x00xXX"
main_target_dir = target_dir+new_dir_name+"/"

if (os.path.exists(target_dir)==False):
	pass #os.mkdir(target_dir)

if (os.path.exists(target_dir+new_dir_name)==False):
    pass #os.mkdir(target_dir+new_dir_name)
else:
    shutil.rmtree(target_dir+new_dir_name)
    # os.rmdir(target_dir+new_dir_name)

# Releaseのデータをひとまず丸ごとコピーする
# ディレクトリがすでにあるとうまく動かないので、コレを最初に持ってくる
shutil.copytree("../project/TOL_SRPG/bin/Release", target_dir+new_dir_name)

# ReadMeのコピー
shutil.copyfile("../document/ReadMe.txt", main_target_dir+"ReadMe.txt")

# 実行ファイルのリネーム
os.rename(main_target_dir+"TOL_SRPG.exe", main_target_dir+"TOLなSRPG.exe")

# 不要なファイルの削除
delete_items = [
    "Log.txt",
    "TOL_SRPG.exe.config",
    "TOL_SRPG.pdb",
    "TOL_SRPG.vshost.exe",
    "TOL_SRPG.vshost.exe.config",
    "TOL_SRPG.vshost.exe.manifest",
]

for i in delete_items:
    os.remove(main_target_dir+i)
