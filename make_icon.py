"""Generate the BigWave Radio app icon (multi-size .ico) and a 32px titlebar PNG."""
import math
from PIL import Image, ImageDraw

SIZE = 256
img = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
draw = ImageDraw.Draw(img)

# Deep ocean gradient background
top = (16, 42, 94)      # deep blue
bottom = (0, 120, 215)  # ocean azure
grad = Image.new("RGBA", (1, SIZE))
for y in range(SIZE):
    t = y / (SIZE - 1)
    grad.putpixel((0, y), tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (255,))
grad = grad.resize((SIZE, SIZE))
mask = Image.new("L", (SIZE, SIZE), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, SIZE, SIZE], radius=int(SIZE * 0.22), fill=255)
img.paste(grad, (0, 0), mask)

# Radio sound waves: three arcs radiating from a center point (broadcast motif)
cx, cy = SIZE * 0.34, SIZE * 0.5
wave = (140, 230, 255, 255)
for i, r in enumerate([SIZE * 0.16, SIZE * 0.30, SIZE * 0.44]):
    bbox = [cx - r, cy - r, cx + r, cy + r]
    start = -55
    end = 55
    draw.arc(bbox, start=start, end=end, fill=wave, width=int(SIZE * 0.055))

# Broadcast dot
dot_r = SIZE * 0.062
draw.ellipse([cx - dot_r, cy - dot_r, cx + dot_r, cy + dot_r], fill=(255, 255, 255, 255))

# Water wave line across the lower third (the "big wave")
pts = []
for x in range(0, SIZE + 1, 2):
    y = SIZE * 0.78 + math.sin(x / SIZE * math.pi * 2.2) * SIZE * 0.045
    pts.append((x, y))
draw.line(pts, fill=(255, 255, 255, 210), width=int(SIZE * 0.028))

img.save("src/BigWaveRadio.App/Assets/bigwave.ico", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
img.resize((32, 32), Image.LANCZOS).save("src/BigWaveRadio.App/Assets/bigwave-32.png")
print("icon + png written")
