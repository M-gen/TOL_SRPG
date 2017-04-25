# -*- coding: utf-8 -*-

def Setup( action_data ):
	action_data.view_name   = "攻撃／弓"
	action_data.system_name = "Basic／攻撃／弓"
	action_data.type        = "攻撃"
	action_data.target_type = "敵"
	action_data.SetRange( 1, 4, "投射", "延長可" )

def EffectValue(action_unit, target_unit, range_ ):
	v = action_unit.status["ATK"] * 1.00 - range_ * 0.6 - target_unit.status["DEF"] * 0.50
	return v

def Action( is_hit, action_unit, target_unit, effect_value ):
	effect.ActionShoot(
			action_unit.map_x, action_unit.map_y, 1.0,
			target_unit.map_x, target_unit.map_y, 1.0
		)
	effect.PlaySound( "戦闘／攻撃／弓・開始", 0.5 )
	action.Wait(99)
	if is_hit:
		effect.PlaySound( "戦闘／攻撃／弓・終了", 0.5 )
		effect.Action( "$CD$effect_damage.py", [target_unit, effect_value] )
	else:
		effect.PlaySound( "戦闘／攻撃／弓・失敗", 0.5 )
