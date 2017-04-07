# -*- coding: utf-8 -*-

def Setup( action_data ):
	action_data.view_name   = "攻撃／剣"
	action_data.system_name = "Basic／攻撃／剣"
	action_data.type        = "攻撃"
	action_data.SetRange( 1, 1, "近接", "範囲内" )

def EffectValue(action_unit, target_unit, range):
	v = action_unit.status["ATK"] * 1.00 - target_unit.status["DEF"] * 0.50
	return v

def ActionEffect( is_hit, target_unit, effect_value ):
	Effect.PlaySound( "戦闘／攻撃／剣・開始", 0.5 )
	Action.Wait(20)
	if is_hit:
		Effect.PlaySound( "戦闘／攻撃／剣・終了", 0.5 )
		Effect.Damage( target_unit, effect_value )
		Effect.Action( "$CD$effect_damage.py" )
	else:
		Effect.PlaySound( "戦闘／攻撃／剣・失敗", 0.5 )
