"""把源 PNG 加工成多尺寸 .ico，并生成预览图。"""
import io
import os
import struct

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "assets", "icon-source.png")
SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]


def resize_rgba(img: Image.Image, s: int) -> Image.Image:
    # 用预乘 alpha 缩小，避免半透明边缘出现白边/黑边
    return img.convert("RGBa").resize((s, s), Image.LANCZOS).convert("RGBA")


def bmp_frame(img: Image.Image, s: int) -> bytes:
    hdr = struct.pack("<IiiHHIIiiII", 40, s, s * 2, 1, 32, 0, s * s * 4, 0, 0, 0, 0)
    px = img.load()
    rows = []
    for y in range(s - 1, -1, -1):
        row = bytearray()
        for x in range(s):
            r, g, b, a = px[x, y]
            row += bytes((b, g, r, a))
        rows.append(bytes(row))
    xor = b"".join(rows)
    mask_stride = ((s + 31) // 32) * 4
    and_mask = b"\x00" * (mask_stride * s)
    return hdr + xor + and_mask


def write_ico(path: str, frames: dict[int, Image.Image]) -> None:
    blobs = []
    for s in sorted(frames):
        img = frames[s]
        data = io.BytesIO()
        img.save(data, format="PNG", optimize=True)
        blobs.append((s, data.getvalue()))

    header = struct.pack("<HHH", 0, 1, len(blobs))
    offset = 6 + 16 * len(blobs)
    entries, payload = b"", b""
    for s, data in blobs:
        dim = 0 if s >= 256 else s
        entries += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(data), offset)
        payload += data
        offset += len(data)
    with open(path, "wb") as f:
        f.write(header + entries + payload)


def write_ico_bmp_small(path: str, frames: dict[int, Image.Image]) -> None:
    """小尺寸用未压缩 BMP 帧（兼容性最好），大尺寸用 PNG 帧。"""
    blobs = []
    for s in sorted(frames):
        img = frames[s]
        if s <= 48:
            blobs.append((s, bmp_frame(img, s)))
        else:
            data = io.BytesIO()
            img.save(data, format="PNG", optimize=True)
            blobs.append((s, data.getvalue()))

    header = struct.pack("<HHH", 0, 1, len(blobs))
    offset = 6 + 16 * len(blobs)
    entries, payload = b"", b""
    for s, data in blobs:
        dim = 0 if s >= 256 else s
        entries += struct.pack("<BBBBHHII", dim, dim, 0, 0, 1, 32, len(data), offset)
        payload += data
        offset += len(data)
    with open(path, "wb") as f:
        f.write(header + entries + payload)


def main() -> None:
    im = Image.open(SRC).convert("RGBA")
    bbox = im.getchannel("A").getbbox()
    print("source", im.size, "content bbox", bbox)

    pad = 8
    l, t, r, b = bbox
    l, t = max(0, l - pad), max(0, t - pad)
    r, b = min(im.width, r + pad), min(im.height, b + pad)
    cropped = im.crop((l, t, r, b))

    side = max(cropped.size)
    square = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    square.paste(cropped, ((side - cropped.width) // 2, (side - cropped.height) // 2), cropped)
    square.save(os.path.join(ROOT, "assets", "icon-square.png"))
    print("square", square.size)

    frames = {s: resize_rgba(square, s) for s in SIZES}
    write_ico_bmp_small(os.path.join(ROOT, "src", "c2d", "app.ico"), frames)

    # 预览：深色 / 浅色两种背景，实际尺寸 + 8 倍放大
    pad_x, gap = 24, 14
    strip_h = 300
    canvas = Image.new("RGBA", (1000, strip_h), (255, 255, 255, 255))
    dark = Image.new("RGBA", (1000, 140), (32, 32, 32, 255))
    light = Image.new("RGBA", (1000, 140), (243, 243, 243, 255))
    canvas.paste(dark, (0, 0))
    canvas.paste(light, (0, 150))

    def place(bg_y: int, bg: Image.Image) -> None:
        x = pad_x
        for s in [256, 128, 64, 48, 32, 24, 16]:
            canvas.alpha_composite(frames[s], (x, bg_y + 80 - s // 2))
            x += s + gap
        # 右侧放大 4 倍，只看 16/24/32/48
        zx = 700
        for s in [16, 24, 32]:
            big = frames[s].resize((s * 4, s * 4), Image.NEAREST)
            canvas.alpha_composite(big, (zx, bg_y + 70 - s * 2))
            zx += s * 4 + 12

    place(0, dark)
    place(150, light)
    canvas.convert("RGB").save(os.path.join(ROOT, "assets", "icon-preview.png"))
    print("ico bytes", os.path.getsize(os.path.join(ROOT, "src", "c2d", "app.ico")))


if __name__ == "__main__":
    main()
