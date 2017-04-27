# -*- coding: utf-8 -*-

target_unit = param[0]
heal_value = param[1]

timer = 0
font             = draw.Font( draw.GetFontFamilyNameByKey("Bold"), 21, 2)
font.main_color  = draw.Color(255,255,255,255) # 主線カラー
font.frame_color = draw.Color(255,0,0,255)     # 枠カラー

add_y = 0
add_y_speed = -50
# 戦闘不能によるtarget_unit.SetAlive(False)の後は
# target_unitのマップ座標が不定になるため、予め取得しておく
map_x = target_unit.map_x
map_y = target_unit.map_y

if target_unit.bt.status["HP"].max < target_unit.bt.status["HP"].now + heal_value:
    heal_value = target_unit.bt.status["HP"].max - target_unit.bt.status["HP"].now

def Update():
    # クラスでないもの？はglobalで宣言が必要?
    global timer, add_y, add_y_speed
    timer += 1

    add_y_speed += 3
    add_y += add_y_speed / 15
    if add_y > 0 :
        div = 0.6
        add_y = (int)(-add_y * div)
        add_y_speed = (int)(-add_y_speed * div)

    if timer == 1:
        # ダメージ反映処理
        hp = target_unit.bt.status["HP"]
        hp.now += heal_value
        status.ReleaseFreeze()     # 操作の停止を解除

    if timer > 160:
        return False

def Draw():
    global timer, add_y, add_y_speed

    a_blend = 255;
    if (timer < 50):
        a_blend = timer * 7
    elif (timer > 130):
        a_blend = 255 - (timer - 130) * 20;
    if (a_blend > 255) :
        a_blend = 255
    if (a_blend < 0) :
        a_blend = 0
    font.main_color.color_a = a_blend

    text = str(heal_value)
    w = draw.GetTextWidth(text, font)
    #x, y = draw.GetScreenPositionByUnitTop(target_unit)
    x, y = draw.GetScreenPositionByMapPos( map_x, map_y, 0, 20, 0)
    draw.Text( x-w/2, y+add_y, text, font )
