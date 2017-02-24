rem Debugの確認環境にあるデータを、Releaseにコピー(ミラー)します
robocopy "../project/TOL_SRPG\bin\Debug\data" "../project/TOL_SRPG\bin\Release\data" /MIR /R:0 /W:0 /NP /XJD /XJF

rem 不足している可能性のある DXライブラリのDLLをコピーしておく
copy "../project/TOL_SRPG\bin\Debug\DxLib.dll" "../project/TOL_SRPG\bin\Release\DxLib.dll" /Y
copy "../project/TOL_SRPG\bin\Debug\DxLib_x64.dll" "../project/TOL_SRPG\bin\Release\DxLib_x64.dll" /Y
copy  "../project/TOL_SRPG\bin\Debug\DxLibDotNet.dll" "../project/TOL_SRPG\bin\Release\DxLibDotNet.dll" /Y