# -*- coding: utf-8 -*-

def Setup( action_data ):
	action_data.view_name   = "攻撃／ヒートボイル"
	action_data.system_name = "Basic／攻撃／ヒートボイル"
	action_data.type        = "攻撃"
	action_data.target_type = "敵"
	action_data.SetRange( 2, 3, "遠直", "範囲内" )

def EffectValue(action_unit, target_unit, range_ ):
	v = action_unit.status["MGK"] * 1.00 - target_unit.status["MGK"] * 0.05
	return v

def Action( is_hit, action_unit, target_unit, effect_value ):
	effect.PlaySound( "戦闘／攻撃／魔法・ヒートボイル", 0.5 )
	action.Wait(15)
	if is_hit:
		#effect.PlaySound( "戦闘／攻撃／弓・終了", 0.5 )
		effect.Action( "$CD$effect_damage.py", [target_unit, effect_value] )
	else:
		pass
		#effect.PlaySound( "戦闘／攻撃／弓・失敗", 0.5 )
