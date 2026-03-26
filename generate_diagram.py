from PIL import Image, ImageDraw, ImageFont
import os

WIDTH = 1400
HEIGHT = 1800
BG_COLOR = (255, 255, 255)
BOX_COLOR = (52, 73, 94)
BOX_TEXT = (255, 255, 255)
DECISION_COLOR = (241, 196, 15)
DECISION_TEXT = (44, 62, 80)
ACTION_COLOR = (46, 204, 113)
ACTION_TEXT = (255, 255, 255)
BLOCK_COLOR = (231, 76, 60)
BLOCK_TEXT = (255, 255, 255)
DB_COLOR = (52, 152, 219)
DB_TEXT = (255, 255, 255)
ARROW_COLOR = (44, 62, 80)
LABEL_COLOR = (44, 62, 80)
TITLE_COLOR = (44, 62, 80)
SUBTITLE_COLOR = (127, 140, 141)
NOTE_BG = (245, 245, 245)
NOTE_BORDER = (189, 195, 199)

img = Image.new("RGB", (WIDTH, HEIGHT), BG_COLOR)
draw = ImageDraw.Draw(img)

try:
    font_title = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 36)
    font_subtitle = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 20)
    font_box = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 18)
    font_small = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 15)
    font_label = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 16)
    font_note = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 14)
except:
    font_title = ImageFont.load_default()
    font_subtitle = font_title
    font_box = font_title
    font_small = font_title
    font_label = font_title
    font_note = font_title


def draw_rounded_rect(d, xy, radius, fill, outline=None):
    x1, y1, x2, y2 = xy
    d.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline)


def draw_box(x, y, w, h, text, fill, text_color, radius=12):
    draw_rounded_rect(draw, (x, y, x + w, y + h), radius, fill)
    lines = text.split("\n")
    total_h = len(lines) * 24
    start_y = y + (h - total_h) // 2
    for i, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_box)
        tw = bbox[2] - bbox[0]
        draw.text((x + (w - tw) // 2, start_y + i * 24), line, fill=text_color, font=font_box)


def draw_diamond(cx, cy, w, h, text, fill, text_color):
    points = [(cx, cy - h // 2), (cx + w // 2, cy), (cx, cy + h // 2), (cx - w // 2, cy)]
    draw.polygon(points, fill=fill, outline=(200, 170, 0))
    lines = text.split("\n")
    total_h = len(lines) * 20
    start_y = cy - total_h // 2
    for i, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_small)
        tw = bbox[2] - bbox[0]
        draw.text((cx - tw // 2, start_y + i * 20), line, fill=text_color, font=font_small)


def draw_db_shape(x, y, w, h, text, fill, text_color):
    ew = 12
    draw.rectangle((x, y + ew, x + w, y + h - ew), fill=fill)
    draw.ellipse((x, y, x + w, y + ew * 2), fill=fill)
    draw.ellipse((x, y + h - ew * 2, x + w, y + h), fill=fill)
    # Top ellipse border
    draw.arc((x, y, x + w, y + ew * 2), 0, 360, fill=(41, 128, 185), width=2)
    lines = text.split("\n")
    total_h = len(lines) * 20
    start_y = y + (h - total_h) // 2 + 4
    for i, line in enumerate(lines):
        bbox = draw.textbbox((0, 0), line, font=font_small)
        tw = bbox[2] - bbox[0]
        draw.text((x + (w - tw) // 2, start_y + i * 20), line, fill=text_color, font=font_small)


def arrow_down(x, y1, y2):
    draw.line((x, y1, x, y2), fill=ARROW_COLOR, width=2)
    draw.polygon([(x - 6, y2 - 8), (x + 6, y2 - 8), (x, y2)], fill=ARROW_COLOR)


def arrow_right(x1, y, x2):
    draw.line((x1, y, x2, y), fill=ARROW_COLOR, width=2)
    draw.polygon([(x2 - 8, y - 6), (x2 - 8, y + 6), (x2, y)], fill=ARROW_COLOR)


def arrow_left(x1, y, x2):
    draw.line((x1, y, x2, y), fill=ARROW_COLOR, width=2)
    draw.polygon([(x2 + 8, y - 6), (x2 + 8, y + 6), (x2, y)], fill=ARROW_COLOR)


def label_on_arrow(x, y, text, align="center"):
    bbox = draw.textbbox((0, 0), text, font=font_label)
    tw = bbox[2] - bbox[0]
    if align == "center":
        draw.text((x - tw // 2, y), text, fill=LABEL_COLOR, font=font_label)
    elif align == "left":
        draw.text((x + 5, y), text, fill=LABEL_COLOR, font=font_label)


# === TITLE ===
draw.text((WIDTH // 2 - 250, 25), "Alur Kerja Limit Hutang", fill=TITLE_COLOR, font=font_title)
draw.text((WIDTH // 2 - 180, 70), "POS System - Validasi Kredit Anggota", fill=SUBTITLE_COLOR, font=font_subtitle)

# Separator
draw.line((100, 105, WIDTH - 100, 105), fill=(220, 220, 220), width=2)

# === STEP 1: Kasir pilih pelanggan ===
cx = WIDTH // 2
bw, bh = 320, 50

draw_box(cx - bw // 2, 130, bw, bh, "Kasir Memilih Pelanggan", BOX_COLOR, BOX_TEXT)
arrow_down(cx, 180, 220)

# === STEP 2: Load data from FIN_ANGGOTA ===
dbw, dbh = 280, 65
draw_db_shape(cx - dbw // 2, 220, dbw, dbh, "FIN_ANGGOTA\n(NIK, STATUS, LIMIT_HUTANG)", DB_COLOR, DB_TEXT)
arrow_down(cx, 285, 320)

# === STEP 3: Decision - Tunai or Kredit? ===
dw, dh = 300, 100
draw_diamond(cx, 370, dw, dh, "NIK = 00.00004\n(Umum/Tunai)?", DECISION_COLOR, DECISION_TEXT)

# YES branch - right
arrow_right(cx + dw // 2, 370, cx + dw // 2 + 120)
label_on_arrow(cx + dw // 2 + 30, 348, "YA", "left")
draw_box(cx + dw // 2 + 120, 345, 240, 50, "TUNAI - Bayar Cash\nTidak perlu cek limit", ACTION_COLOR, ACTION_TEXT)

# NO branch - down
arrow_down(cx, 420, 470)
label_on_arrow(cx + 8, 430, "TIDAK", "left")

# === STEP 4: Decision - Has limit? ===
draw_diamond(cx, 520, 300, 100, "LIMIT_HUTANG\n!= 0 ?", DECISION_COLOR, DECISION_TEXT)

# NO branch - right (no limit set)
arrow_right(cx + 150, 520, cx + 270)
label_on_arrow(cx + 170, 498, "TIDAK", "left")
draw_box(cx + 270, 495, 240, 50, "KREDIT Tanpa Batas\nBoleh simpan transaksi", ACTION_COLOR, ACTION_TEXT)

# YES branch - down
arrow_down(cx, 570, 620)
label_on_arrow(cx + 8, 578, "YA (ada limit)", "left")

# === STEP 5: Get periode dates ===
draw_box(cx - bw // 2, 620, bw, 50, "Ambil Rentang Tanggal Periode", BOX_COLOR, BOX_TEXT)

# DB shape for POS_PERIODE on the left
dbw2 = 260
draw_db_shape(cx - bw // 2 - dbw2 - 40, 610, dbw2, 70, "POS_PERIODE\n(R1DARI, R2SAMPAI,\nBDARI, BSAMPAI)", DB_COLOR, DB_TEXT)
arrow_right(cx - bw // 2 - 40, 645, cx - bw // 2)

arrow_down(cx, 670, 710)

# === STEP 6: Decision by STATUS ===
draw_diamond(cx, 760, 300, 100, "STATUS\nAnggota?", DECISION_COLOR, DECISION_TEXT)

# BULANAN branch - left
arrow_left(cx - 150, 760, cx - 270)
label_on_arrow(cx - 260, 738, "BULANAN", "left")
draw_box(cx - 510, 735, 240, 50, "Dari: BDARI\nSampai: BSAMPAI", BOX_COLOR, BOX_TEXT)

# REMISE branch - right
arrow_right(cx + 150, 760, cx + 270)
label_on_arrow(cx + 170, 738, "REMISE", "left")
draw_box(cx + 270, 735, 240, 50, "Dari: R1DARI\nSampai: R2SAMPAI", BOX_COLOR, BOX_TEXT)

# Continue down
arrow_down(cx, 810, 870)

# === STEP 7: Query total hutang ===
draw_box(cx - bw // 2, 870, bw, 50, "Hitung Total Hutang Berjalan", BOX_COLOR, BOX_TEXT)

# DB shape for POS_PENJUALAN on the right
draw_db_shape(cx + bw // 2 + 40, 855, 280, 80, "POS_PENJUALAN\nSUM(TOTAL)\nWHERE NIK & TANGGAL", DB_COLOR, DB_TEXT)
arrow_left(cx + bw // 2 + 40, 895, cx + bw // 2)

arrow_down(cx, 920, 970)

# === STEP 8: Calculation box ===
calc_h = 90
draw_rounded_rect(draw, (cx - 200, 970, cx + 200, 970 + calc_h), 12, NOTE_BG, NOTE_BORDER)
calc_lines = [
    "totalHutang = SUM hutang periode ini",
    "JUMLAHFAKTUR = nilai faktur baru",
    "totalSetelahFaktur = totalHutang + JUMLAHFAKTUR"
]
for i, line in enumerate(calc_lines):
    draw.text((cx - 185, 980 + i * 26), line, fill=LABEL_COLOR, font=font_small)

arrow_down(cx, 1060, 1110)

# === STEP 9: Final decision ===
draw_diamond(cx, 1160, 360, 110, "totalSetelahFaktur\n> LIMIT_HUTANG ?", DECISION_COLOR, DECISION_TEXT)

# YES - BLOCKED (left)
arrow_left(cx - 180, 1160, cx - 310)
label_on_arrow(cx - 300, 1138, "YA", "left")

block_x = cx - 560
block_w = 250
block_h = 140
draw_rounded_rect(draw, (block_x, 1090, block_x + block_w, 1090 + block_h), 12, BLOCK_COLOR)
block_lines = [
    "TRANSAKSI DITOLAK",
    "",
    "Tombol Simpan",
    "dinonaktifkan",
    "",
    "Tampilkan pesan error",
]
for i, line in enumerate(block_lines):
    bbox = draw.textbbox((0, 0), line, font=font_small)
    tw = bbox[2] - bbox[0]
    draw.text((block_x + (block_w - tw) // 2, 1098 + i * 22), line, fill=BLOCK_TEXT, font=font_small)

# NO - ALLOWED (right)
arrow_right(cx + 180, 1160, cx + 310)
label_on_arrow(cx + 195, 1138, "TIDAK", "left")

allow_x = cx + 310
allow_w = 250
allow_h = 140
draw_rounded_rect(draw, (allow_x, 1090, allow_x + allow_w, 1090 + allow_h), 12, ACTION_COLOR)
allow_lines = [
    "TRANSAKSI DIIZINKAN",
    "",
    "Tombol Simpan aktif",
    "Kasir dapat menyimpan",
    "dan mencetak faktur",
]
for i, line in enumerate(allow_lines):
    bbox = draw.textbbox((0, 0), line, font=font_small)
    tw = bbox[2] - bbox[0]
    draw.text((allow_x + (allow_w - tw) // 2, 1098 + i * 22), line, fill=ACTION_TEXT, font=font_small)

# === EXAMPLE BOX ===
ex_y = 1280
ex_x = 100
ex_w = WIDTH - 200
ex_h = 210
draw.line((100, ex_y - 20, WIDTH - 100, ex_y - 20), fill=(220, 220, 220), width=2)
draw.text((ex_x, ex_y, ), "Contoh Kasus:", fill=TITLE_COLOR, font=ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 22))

# Example: BLOCKED
ex1_x = ex_x
ex1_y = ex_y + 40
draw_rounded_rect(draw, (ex1_x, ex1_y, ex1_x + 560, ex1_y + 160), 10, (253, 237, 236), BLOCK_COLOR)
ex1_lines = [
    "DITOLAK:",
    "Limit Hutang Anggota   : Rp. 2.000.000",
    "Hutang Saat Ini        : Rp. 1.800.000",
    "Faktur Baru            : Rp.    500.000",
    "Total Setelah Faktur   : Rp. 2.300.000  (> Limit)",
    "Kelebihan              : Rp.    300.000",
]
for i, line in enumerate(ex1_lines):
    color = BLOCK_COLOR if i == 0 else (44, 62, 80)
    f = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 16) if i == 0 else font_small
    draw.text((ex1_x + 20, ex1_y + 10 + i * 24), line, fill=color, font=f)

# Example: ALLOWED
ex2_x = ex1_x + 600
draw_rounded_rect(draw, (ex2_x, ex1_y, ex2_x + 560, ex1_y + 160), 10, (234, 250, 241), ACTION_COLOR)
ex2_lines = [
    "DIIZINKAN:",
    "Limit Hutang Anggota   : Rp. 2.000.000",
    "Hutang Saat Ini        : Rp.    800.000",
    "Faktur Baru            : Rp.    500.000",
    "Total Setelah Faktur   : Rp. 1.300.000  (< Limit)",
    "Sisa Limit             : Rp.    700.000",
]
for i, line in enumerate(ex2_lines):
    color = (39, 174, 96) if i == 0 else (44, 62, 80)
    f = ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 16) if i == 0 else font_small
    draw.text((ex2_x + 20, ex1_y + 10 + i * 24), line, fill=color, font=f)

# === LEGEND ===
leg_y = 1520
draw.line((100, leg_y - 10, WIDTH - 100, leg_y - 10), fill=(220, 220, 220), width=2)
draw.text((ex_x, leg_y), "Keterangan:", fill=TITLE_COLOR, font=ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 18))

legend_items = [
    ((52, 73, 94), "Proses / Langkah"),
    ((241, 196, 15), "Keputusan (Decision)"),
    ((52, 152, 219), "Database / Tabel"),
    ((46, 204, 113), "Transaksi Diizinkan"),
    ((231, 76, 60), "Transaksi Ditolak"),
]

lx = ex_x
for i, (color, label) in enumerate(legend_items):
    col = i % 3
    row = i // 3
    px = lx + col * 400
    py = leg_y + 35 + row * 35
    draw.rounded_rectangle((px, py, px + 24, py + 20), radius=4, fill=color)
    draw.text((px + 32, py), label, fill=LABEL_COLOR, font=font_small)

# === TABLE INFO ===
tbl_y = 1630
draw.line((100, tbl_y - 10, WIDTH - 100, tbl_y - 10), fill=(220, 220, 220), width=2)
draw.text((ex_x, tbl_y), "Tabel Database yang Terlibat:", fill=TITLE_COLOR, font=ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 18))

tables = [
    ("FIN_ANGGOTA", "Data master anggota: NIK, NAMA, STATUS, LIMIT_HUTANG, AKTIF"),
    ("POS_PERIODE", "Pengaturan periode: R1DARI, R1SAMPAI, R2DARI, R2SAMPAI, BDARI, BSAMPAI"),
    ("POS_PENJUALAN", "Transaksi penjualan: NO_TRANSAKSI, NIK, TANGGAL, TOTAL, JENIS_BAYAR"),
]

for i, (name, desc) in enumerate(tables):
    ty = tbl_y + 35 + i * 30
    draw.text((ex_x + 20, ty), name, fill=DB_COLOR, font=ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", 15))
    draw.text((ex_x + 220, ty), desc, fill=(100, 100, 100), font=font_small)

# Save
output = os.path.join(os.path.dirname(os.path.abspath(__file__)), "limit_hutang_flowchart.png")
img.save(output, "PNG", dpi=(150, 150))
print(f"Saved to: {output}")
