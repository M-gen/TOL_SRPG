# -*- coding: utf-8 -*-

timer = 0
main_color = draw.Color(255,255,255,255)
frame_color = draw.Color(255,255,0,0)
font = draw.Font(21,2)

def Update():
    # timerが、何故かglobalの宣言がいる…他いけるのに？
    global timer
    timer += 1

    if timer > 100:
        return False

def Draw():
    draw.Text( 100, 100, "aaa", font, main_color, frame_color )
