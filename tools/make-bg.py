"""把图标插画生成嵌入用的原始 BGRA 位图（预乘 alpha，供 AlphaBlend 使用）。"""
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "assets", "icon-source.png")
SIZE = 320


def main() -> None:
    im = Image.open(SRC).convert("RGBA")

    bbox = im.getchannel("A").getbbox()
    im = im.crop(bbox)

    # 保持比例，贴到正方形画布中央
    side = max(im.size)
    sq = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2), im)
    sq = sq.resize((SIZE, SIZE), Image.LANCZOS)

    px = sq.load()
    buf = bytearray(SIZE * SIZE * 4)
    i = 0
    for y in range(SIZE):
        for x in range(SIZE):
            r, g, b, a = px[x, y]
            # AlphaBlend 要求预乘
            buf[i + 0] = (b * a) // 255
            buf[i + 1] = (g * a) // 255
            buf[i + 2] = (r * a) // 255
            buf[i + 3] = a
            i += 4

    out = os.path.join(ROOT, "src", "c2d", "bg.bin")
    with open(out, "wb") as f:
        f.write(buf)
    print("bg.bin", len(buf), "bytes")

    # 预览：模拟 1000x150 的横幅，插画贴右边
    band = Image.new("RGB", (1000, 150), (255, 255, 255))
    art = sq.resize((140, 140), Image.LANCZOS)
    band.paste(art, (1000 - 24 - 140, 5), art)
    band.save(os.path.join(ROOT, "assets", "bg-preview.png"))
    print("preview saved")


if __name__ == "__main__":
    main()
