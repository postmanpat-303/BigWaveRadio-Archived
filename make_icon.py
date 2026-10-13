"""Build BigWave Radio icon assets from the master logo.

The master logo (src/BigWaveRadio.App/Assets/bigwave-logo-src.png, 512px)
is the only hand-authored asset. Run this script to regenerate:
  - bigwave.ico        multi-size icon for the exe and window
  - bigwave-32.png     title-bar / small-size logo
  - bigwave-logo-src.png  re-optimised master (when built from a larger source)

Usage:  python make_icon.py [path-to-larger-source.png]
"""
import sys
from PIL import Image

ASSETS = "src/BigWaveRadio.App/Assets"
MASTER = f"{ASSETS}/bigwave-logo-src.png"


def build(src: Image.Image) -> None:
    print("source:", src.size)
    src.resize((512, 512), Image.LANCZOS).save(MASTER, optimize=True)
    src.resize((32, 32), Image.LANCZOS).save(f"{ASSETS}/bigwave-32.png")
    src.resize((256, 256), Image.LANCZOS).save(
        f"{ASSETS}/bigwave.ico",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )
    print("assets written")


if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else MASTER
    build(Image.open(path).convert("RGBA"))
