from PIL import Image, ImageDraw
from pathlib import Path
out = Path("assets")
out.mkdir(exist_ok=True)
im = Image.new("RGBA", (256, 256), (5, 11, 22, 255))
d = ImageDraw.Draw(im)
# glowing rounded blue tile
d.rounded_rectangle((10, 10, 246, 246), radius=48, fill=(7, 28, 54, 255), outline=(0, 190, 255, 255), width=9)
# phone outline
d.rounded_rectangle((65, 35, 164, 198), radius=19, outline=(240, 248, 255, 255), width=12)
d.line((94, 53, 135, 53), fill=(240, 248, 255, 255), width=7)
d.line((101, 177, 130, 177), fill=(240, 248, 255, 255), width=7)
# magnifier
d.ellipse((112, 101, 195, 184), outline=(0, 220, 255, 255), width=13)
d.line((176, 170, 216, 210), fill=(0, 220, 255, 255), width=16)
# produce a transparent reusable logo and windows icon
im.save(out / "gsm-check-tool-logo.png")
im.save(out / "gsm-check-tool.ico", format="ICO", sizes=[(256,256),(128,128),(64,64),(48,48),(32,32),(16,16)])
