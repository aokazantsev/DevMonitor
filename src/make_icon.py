from PIL import Image, ImageDraw

SIZE = 1024
TILE = (15, 23, 42, 255)
GLASS = (241, 245, 249, 255)
MERCURY = (239, 68, 68, 255)
TICK = (148, 163, 184, 255)

image = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(image)
draw.rounded_rectangle((32, 32, SIZE - 32, SIZE - 32), radius=220, fill=TILE)

center_x = SIZE // 2 - 40
tube_width = 190
tube_top = 170
bulb_center_y = 740
bulb_radius = 170

draw.rounded_rectangle(
    (center_x - tube_width // 2, tube_top, center_x + tube_width // 2, bulb_center_y),
    radius=tube_width // 2, fill=GLASS)
draw.ellipse(
    (center_x - bulb_radius, bulb_center_y - bulb_radius, center_x + bulb_radius, bulb_center_y + bulb_radius),
    fill=GLASS)

inner_width = 90
inner_radius = 110
draw.rounded_rectangle(
    (center_x - inner_width // 2, 380, center_x + inner_width // 2, bulb_center_y),
    radius=inner_width // 2, fill=MERCURY)
draw.ellipse(
    (center_x - inner_radius, bulb_center_y - inner_radius, center_x + inner_radius, bulb_center_y + inner_radius),
    fill=MERCURY)

tick_left = center_x + tube_width // 2 + 50
for index, y in enumerate(range(230, 620, 78)):
    length = 150 if index % 2 == 0 else 95
    draw.rounded_rectangle((tick_left, y - 18, tick_left + length, y + 18), radius=18, fill=TICK)

sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
image.resize((256, 256), Image.LANCZOS).save("D:/DevMonitor/src/app.ico", sizes=sizes)
image.resize((256, 256), Image.LANCZOS).save("D:/DevMonitor/src/app_preview.png")
