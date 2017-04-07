# -*- coding: utf-8 -*-

def Setup():
	Map.Load( "$CD$stage_0_map.nst" )

	Map.AddUnit( 1, 0, "ピープル",   "ドーナッツ", "味方", 0, 2 )
	Map.AddUnit( 2, 0, "ファイター", "コミニ",     "味方", 0, 2 )
	Map.AddUnit( 3, 0, "アーチャー", "ハーゲン",   "味方", 0, 2 )
	Map.AddUnit( 2, 2, "ウィザード", "フランツォ", "味方", 0, 2 )
	Map.AddUnit( 4, 8, "ピープル",   "ダン",       "敵1", 1, 3 )
	Map.AddUnit( 5, 8, "ファイター", "アルナハト",  "敵1", 1, 0 )
	Map.AddUnit( 4, 7, "アーチャー", "ポイニーチェ","敵1", 1, 3 )
	Map.AddUnit( 5, 7, "ウィザード", "フレデック",  "敵1", 1, 0 )
