# -*- coding: utf-8 -*-

target = param[0]
damage_value = param[1]

timer = 0
font             = draw.Font( draw.GetFontFamilyNameByKey("Bold"), 21, 2)
font.main_color  = draw.Color(255,255,255,255) # 主線カラー
font.frame_color = draw.Color(255,255,0,0)     # 枠カラー

add_y = 0
add_y_speed = -50

def Update():
    # timerが、何故かglobalの宣言がいる…他いけるのに？
    global timer, add_y, add_y_speed
    timer += 1

    add_y_speed += 3
    add_y += add_y_speed / 15
    if ( add_y > 0 ):
        div = 0.6
        add_y = (int)(-add_y * div)
        add_y_speed = (int)(-add_y_speed * div)


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

    text = str(damage_value)
    w = draw.GetTextWidth(text, font)
    x, y = draw.GetScreenPositionByUnitTop(target)
    draw.Text( x-w/2, y+add_y, text, font )
