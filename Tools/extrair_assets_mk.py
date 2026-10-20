"""
Extrai dos repositórios de referência os cenários, efeitos, interface, fontes e
sons usados pela base estilo Mortal Kombat do Dev Kombat.

Uso (na raiz do projeto):

    python -m pip install pillow
    python Tools/extrair_assets_mk.py

Os repositórios são clonados em ../_referencias (ao lado do projeto) quando
ainda não existem. O resultado vai para Assets/Resources/Kombat, pasta ignorada
pelo Git: os sprites e sons pertencem à Warner Bros./NetherRealm e servem apenas
para estudo e testes locais. Não publique esses arquivos.
"""

import colorsys
import json
import os
import re
import shutil
import subprocess
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
REFS = os.path.join(os.path.dirname(ROOT), "_referencias")
OUT = os.path.join(ROOT, "Assets", "Resources", "Kombat")

REPOS = {
    "mk2-remake": "https://github.com/perninio/Mortal-KombatII-remake.git",
    "mk-reimagined": "https://github.com/uzeernabeel/Mortal-Kombat-Reimagined.git",
    "CatfishStudio_Mortal-Kombat-QC": "https://github.com/CatfishStudio/Mortal-Kombat-QC.git",
    "nazimboudeffa_phaser2-mk1": "https://github.com/nazimboudeffa/phaser2-mk1.git",
    "NeMoDDD_Mortal-Kombat": "https://github.com/NeMoDDD/Mortal-Kombat.git",
    "vladimirplyukhin89_MortalKombat": "https://github.com/vladimirplyukhin89/MortalKombat.git",
}

MK2 = "mk2-remake/Assets"
REIM = "mk-reimagined/Assets"
SCORPION = f"{MK2}/characters/scorpion/SNES_MK2_scorp.png"
SUBZERO = f"{MK2}/characters/sub-zero/sprites/SNES_MK2_subz.png"
LIUKANG = f"{MK2}/characters/liu_kang/SNES_MK2_liuKang.png"
BLOOD = f"{MK2}/sprites/effect/SNES_MK2_blood.png"
MENU = f"{MK2}/sprites/menu/58949.png"

# ---------------------------------------------------------------------------
# Corpo base: ninja do MK2 (Scorpion) separado em corpo + roupa tingível.
# Os números são os índices dos recortes do .meta original (SNES_MK2_scorp_N).
# ---------------------------------------------------------------------------
NINJA_ANIMATIONS = {
    "idle": [1, 2, 3, 4, 5, 6],
    "walk": list(range(16, 25)),
    "crouch": [11, 12],
    "crouch_block": [15],
    "block": [9, 10],
    "jump_start": [25],
    "jump_up": [26],
    "jump_land": [27],
    "flip": list(range(28, 35)),
    "low_punch": [37, 38, 39],
    "high_punch": [40, 41, 42, 43],
    "throw": [52, 53],
    "thrown": list(range(54, 62)),
    "high_kick": list(range(62, 68)),
    "low_kick": list(range(68, 73)),
    "knee": [73, 74],
    "roundhouse": list(range(75, 81)),
    "sweep": list(range(81, 86)),
    "uppercut": list(range(86, 92)),
    "crouch_punch": [92, 93],
    "crouch_kick": list(range(98, 103)),
    "jump_punch": [103, 104],
    "jump_kick": [105, 106],
    "flying_kick": [107, 108],
    "hit_high": [109, 110],
    "hit_low": [113, 114, 115],
    "crouch_hit": [118, 119],
    "knockdown": list(range(127, 132)),
    "launched": list(range(132, 138)),
    "get_up": list(range(138, 142)),
    "dizzy": list(range(144, 149)),
    "special_lunge": list(range(149, 155)),
    "special_throw": [193, 194, 195],
    "victory": [270],
    "victory_dance": [267, 268, 269],
    "defeat": list(range(227, 233)),
    "fatality_unmask": list(range(167, 173)),
    "baby": [271],
}
AIRBORNE = {"jump_start", "jump_up", "jump_land", "flip", "thrown", "launched",
            "jump_punch", "jump_kick", "flying_kick"}

# ---------------------------------------------------------------------------
# Efeitos. Tipos de origem:
#   ("meta", folha, [índices])          -> um quadro por recorte do .meta
#   ("box", folha, (x0, y0, x1, y1))    -> recorta a área e separa quadros pelas colunas vazias
#   ("grid", arquivo, colunas, linhas)   -> folha em grade regular
#   ("files", [arquivos], tamanho_max)  -> um quadro por arquivo
# ---------------------------------------------------------------------------
EFFECTS = {
    "spear_head": ("meta", SCORPION, [155]),
    "spear_rope": ("meta", SCORPION, [163]),
    "spear_rope_wave": ("meta", SCORPION, [166]),
    "fire_ball": ("meta", SCORPION, [185, 186, 187, 181]),
    "fire_explosion": ("meta", SCORPION, [188, 182, 173, 174, 175, 176]),
    "ice_blast": ("box", SUBZERO, (182, 1274, 773, 1346)),
    "ice_ground": ("meta", SUBZERO, list(range(213, 222))),
    "liu_fireball": ("meta", LIUKANG, [160, 161, 163, 164]),
    "liu_fireball_impact": ("meta", LIUKANG, [146, 147, 148, 149, 150]),
    "shadow_ball": ("box", f"{MK2}/characters/johnny_cage/SNES_MK2_johnny.png", (272, 1505, 432, 1545)),
    "shadow_impact": ("box", f"{MK2}/characters/johnny_cage/SNES_MK2_johnny.png", (432, 1470, 600, 1550)),
    "energy_wave": ("box", f"{MK2}/characters/jax/SNES_MK2_jax.png", (678, 1555, 880, 1590)),
    "sai": ("box", f"{MK2}/characters/mileena/SNES_MK2_mileena.png", (176, 1465, 616, 1505)),
    "fan_spin": ("box", f"{MK2}/characters/jade/SNES_MK2_jade.png", (125, 1425, 495, 1482)),
    "fan_arc": ("box", f"{MK2}/characters/jade/SNES_MK2_jade.png", (500, 1410, 1012, 1482)),
    "green_spear": ("box", f"{MK2}/characters/shao-kahn/SNES_MK2_Kahn.png", (238, 540, 830, 612)),
    "blood_spurt": ("box", BLOOD, (5, 5, 197, 44)),
    "blood_spray": ("box", BLOOD, (5, 54, 115, 77)),
    "blood_drops": ("box", BLOOD, (5, 87, 274, 106)),
    "blood_burst": ("box", BLOOD, (5, 121, 490, 190)),
    "blood_fountain": ("box", BLOOD, (5, 196, 410, 288)),
    "blood_drip": ("box", BLOOD, (5, 298, 159, 322)),
    "blood_pool": ("box", BLOOD, (5, 335, 389, 346)),
    "gibs": ("meta", BLOOD, list(range(151, 217))),
    "burning_body": ("box", BLOOD, (5, 484, 316, 589)),
    "blood_splat": ("meta", BLOOD, [230, 233]),
    "explosion": ("grid", f"{REIM}/otherStuff/blastAnim.png", 4, 4),
    "hell_fire": ("files", None, 128),
    "ice_break": ("files", None, 128),
    "portal": ("files", None, 160),
}
EFFECT_FOLDERS = {
    "hell_fire": f"{REIM}/otherStuff/LevelObsticles/HellFire",
    "ice_break": f"{REIM}/otherStuff/LevelObsticles/IceBreak",
    "portal": f"{REIM}/otherStuff/final2",
}

# ---------------------------------------------------------------------------
# Interface (folha de menu do MK2 SNES). Áreas em pixels da imagem original.
# ---------------------------------------------------------------------------
UI = {
    "fight_red": (840, 462, 938, 494),
    "fight_yellow": (840, 494, 938, 527),
    "finish_him_red": (841, 527, 1015, 558),
    "finish_him_yellow": (841, 559, 1015, 591),
    "finish_her_red": (841, 591, 1015, 623),
    "finish_her_yellow": (841, 623, 1015, 655),
    "round": (841, 671, 889, 685),
    "toasty_face": (1052, 627, 1096, 685),
    "fatality": (645, 566, 752, 594),
    "babality": (660, 595, 738, 615),
    "game_over": (840, 692, 920, 708),
    "time_is_up": (840, 746, 920, 758),
    "flawless_victory": (988, 692, 1096, 703),
    "toasty": (840, 768, 915, 784),
    "win_medal": (692, 696, 710, 718),
    "dragon_logo": (16, 748, 272, 972),
    "dragon_disc": (334, 752, 512, 938),
    "title_stone": (292, 262, 549, 484),
    "panel_stone": (16, 505, 273, 728),
    "versus_stone": (292, 16, 549, 241),
    "button_start": (292, 547, 549, 634),
    "button_options": (292, 640, 549, 728),
    "select_frame": (954, 124, 988, 166),
}

# Cenários: (arquivo, altura do chão medida a partir do topo da imagem, nome exibido)
CATFISH = "CatfishStudio_Mortal-Kombat-QC/GameCards-close/src/mkcards/assets/media/textures"
STAGES = {
    "kahn_arena": (f"{MK2}/sprites/arena/arena1.png", 0.88, "Arena do Kahn"),
    "shaolin_temple": (f"{REIM}/otherStuff/StartScreenStuff/StartScreenBackground.png", 0.86, "Templo Shaolin"),
    "throne_room": ("nazimboudeffa_phaser2-mk1/assets/arena/01.png", 0.92, "Sala do Trono"),
    "armory": ("NeMoDDD_Mortal-Kombat/images/background.png", 0.92, "Arsenal"),
    "scorpion_lair": ("vladimirplyukhin89_MortalKombat/assets/scorpions-lair-arenas.png", 0.95, "Covil do Scorpion"),
    "subway": (f"{CATFISH}/level_bg_1.png", 0.90, "Metrô"),
    "street": (f"{CATFISH}/level_bg_2.png", 0.90, "Rua"),
    "bank": (f"{CATFISH}/level_bg_3.png", 0.90, "Banco"),
    "bell_tower": (f"{CATFISH}/level_bg_4.png", 0.90, "Torre do Sino"),
    "dungeon": (f"{CATFISH}/level_bg_5.png", 0.90, "Masmorra"),
    "soul_chamber": (f"{CATFISH}/level_bg_6.png", 0.90, "Câmara das Almas"),
    "kombat_temple": (f"{CATFISH}/level_bg_7.png", 0.92, "Templo do Kombate"),
    "graveyard": (f"{CATFISH}/level_bg_8.png", 0.92, "Cemitério"),
    "green_chamber": (f"{CATFISH}/level_bg_9.png", 0.90, "Câmara Verde"),
    "lava_pit": (f"{CATFISH}/level_bg_10.png", 0.92, "Poço de Lava"),
    "dark_throne": (f"{CATFISH}/level_bg_11.png", 0.92, "Trono Sombrio"),
    "rooftop": (f"{CATFISH}/level_bg_12.png", 0.90, "Terraço"),
}

# Sons: destino -> lista de (origem, nome final)
AUDIO = {
    "Announcer": [
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Round1.mp3", "round1.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Round2.mp3", "round2.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Round3.mp3", "round3.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Round4.mp3", "round4.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Fight.mp3", "fight.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Finish Him.mp3", "finish_him.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Flawless Victory.mp3", "flawless_victory.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Excelent.mp3", "excellent.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Outstanding.mp3", "outstanding.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Superb.mp3", "superb.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Well Done.mp3", "well_done.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Brutality.mp3", "brutality.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/laugh.mp3", "laugh.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Laugh2.mp3", "laugh2.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/You Suck.mp3", "you_suck.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/That was Pathatic.mp3", "pathetic.mp3"),
        (f"{REIM}/Sounds/OtherSounds/ShawKahn/Don't make me laugh.mp3", "dont_make_me_laugh.mp3"),
        (f"{REIM}/Sounds/OtherSounds/funSounds/Toasty.mp3", "toasty.mp3"),
        (f"{REIM}/Sounds/OtherSounds/funSounds/Frosty.mp3", "frosty.mp3"),
        (f"{MK2}/sound/gong.mp3", "gong.mp3"),
        (f"{MK2}/sound/finish him scorpion.mp3", "finish_him_mk2.mp3"),
        (f"{MK2}/sound/round one fight.mp3", "round_one_fight_mk2.mp3"),
    ],
    "Music": [
        (f"{MK2}/sound/Mortal Kombat 2 Theme Remix - Character Select Theme-[AudioTrimmer.com].mp3", "select.mp3"),
        (f"{REIM}/Sounds/BackgroundSound/mortalkombat (1).mp3", "title.mp3"),
        (f"{REIM}/Sounds/BackgroundSound/BackgroundMusicNoobSaibotLevel.mp3", "fight_1.mp3"),
    ],
    "Specials": [
        (f"{REIM}/Sounds/OtherSounds/scorpion/mk3-21135.mp3", "get_over_here.mp3"),
        (f"{REIM}/Sounds/OtherSounds/scorpion/mk3-21140.mp3", "come_here.mp3"),
        (f"{REIM}/Sounds/PlayerSounds/SubZeroPowerSound.mp3", "ice_cast.mp3"),
        (f"{REIM}/Sounds/PlayerSounds/SubZeroPowerGot.mp3", "ice_hit.mp3"),
        (f"{REIM}/Sounds/OtherSounds/funSounds/PlayerPowerHit.mp3", "power_hit.mp3"),
        (f"{REIM}/Sounds/OtherSounds/funSounds/crisspy.mp3", "crispy.mp3"),
        (f"{REIM}/Sounds/OtherSounds/funSounds/MenBrutlity.mp3", "male_brutality.mp3"),
        (f"{REIM}/Sounds/OtherSounds/funSounds/WomenBrutality.mp3", "female_brutality.mp3"),
        (f"{MK2}/sound/warrior_punch_scream.mp3", "warrior_scream.mp3"),
    ],
}
# Pastas copiadas inteiras: destino -> origem
AUDIO_FOLDERS = {
    "Hits": f"{REIM}/Sounds/OtherSounds/hitsounds",
    "VoiceMale": f"{REIM}/Sounds/OtherSounds/male",
    "VoiceFemale": f"{REIM}/Sounds/OtherSounds/female",
    "MusicStages": f"{REIM}/Sounds/OtherSounds/LevelBackground",
    "Unsorted": f"{REIM}/Sounds/OtherSounds/specialfx",
}

FONTS = [
    (f"{MK2}/font/mortalkombat2.ttf", "mk2.ttf"),
]


def ref(path):
    return os.path.join(REFS, path)


def ensure_repos():
    os.makedirs(REFS, exist_ok=True)
    for folder, url in REPOS.items():
        target = os.path.join(REFS, folder)
        if not os.path.isdir(target):
            print(f"Clonando {url} ...")
            subprocess.check_call(["git", "clone", "--depth", "1", "-q", url, target])


def meta_rects(png):
    """Lê os recortes de um .meta do Unity e devolve {índice: (x, y_topo, w, h)}."""
    height = Image.open(png).size[1]
    text = open(png + ".meta", encoding="utf-8").read()
    pattern = (r"name: \S+?_(\d+)\s+rect:\s+serializedVersion: 2\s+x: ([\d.]+)\s+"
               r"y: ([\d.]+)\s+width: ([\d.]+)\s+height: ([\d.]+)")
    rects = {}
    for match in re.finditer(pattern, text):
        index, x, y, w, h = match.groups()
        x, y, w, h = int(float(x)), int(float(y)), int(float(w)), int(float(h))
        rects[int(index)] = (x, height - y - h, w, h)
    return rects


_sheet_cache = {}


def sheet(path):
    if path not in _sheet_cache:
        _sheet_cache[path] = Image.open(ref(path)).convert("RGBA")
    return _sheet_cache[path]


def is_background(pixel):
    r, g, b, a = pixel
    return a < 16 or (g > 200 and r < 70 and b < 70)


def clean(image):
    """Remove o fundo verde de algumas folhas e corta as bordas vazias."""
    pixels = image.load()
    for y in range(image.size[1]):
        for x in range(image.size[0]):
            if is_background(pixels[x, y]):
                pixels[x, y] = (0, 0, 0, 0)
    box = image.getbbox()
    return image.crop(box) if box else None


def split_columns(image, min_gap=2):
    """Separa quadros lado a lado usando colunas totalmente transparentes."""
    alpha = image.split()[3]
    width, height = image.size
    columns = [any(alpha.getpixel((x, y)) > 0 for y in range(height)) for x in range(width)]
    frames, start, gap = [], None, 0
    for x, filled in enumerate(columns + [False] * (min_gap + 1)):
        if filled:
            if start is None:
                start = x
            gap = 0
        elif start is not None:
            gap += 1
            if gap > min_gap:
                frame = clean(image.crop((start, 0, x - gap + 1, height)))
                # Ignora as linhas pretas que separam quadros em algumas folhas.
                if frame and frame.size[0] > 4 and frame.size[0] * frame.size[1] > 12:
                    frames.append(frame)
                start, gap = None, 0
    return frames


def save_frames(folder, frames):
    if os.path.isdir(folder):
        shutil.rmtree(folder)
    os.makedirs(folder)
    for i, frame in enumerate(frames):
        frame.save(os.path.join(folder, f"{i:02d}.png"))
    return len(frames)


def is_costume(pixel):
    r, g, b, a = pixel
    if a == 0:
        return False
    h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return 0.07 <= h <= 0.18 and s >= 0.35 and v >= 0.22


def anchor_x(image, airborne):
    """Ponto de apoio horizontal: centro dos pés no chão ou centro de massa no ar."""
    width, height = image.size
    alpha = image.split()[3]
    top = 0 if airborne else int(height * 0.85)
    total = weight = 0
    for y in range(top, height):
        for x in range(width):
            if alpha.getpixel((x, y)) > 0:
                total += x
                weight += 1
    return total / weight if weight else width / 2


def export_ninja():
    rects = meta_rects(ref(SCORPION))
    base_root = os.path.join(OUT, "Fighters", "Ninja")
    costume_root = os.path.join(OUT, "Fighters", "NinjaCostume")
    summary = {}
    for anim, indices in NINJA_ANIMATIONS.items():
        crops = []
        for index in indices:
            x, y, w, h = rects[index]
            frame = sheet(SCORPION).crop((x, y, x + w, y + h))
            crops.append((frame, y + h, anchor_x(frame, anim in AIRBORNE)))
        baseline = max(bottom for _, bottom, _ in crops)
        half = max(max(ax, f.size[0] - ax) for f, _, ax in crops)
        tallest = max(f.size[1] + (baseline - bottom) for f, bottom, _ in crops)
        base_frames, costume_frames = [], []
        for frame, bottom, ax in crops:
            width = int(half * 2) + 2
            canvas_base = Image.new("RGBA", (width, tallest), (0, 0, 0, 0))
            canvas_costume = Image.new("RGBA", (width, tallest), (0, 0, 0, 0))
            px = int(round(half - ax)) + 1
            py = tallest - frame.size[1] - (baseline - bottom)
            body = frame.copy()
            costume = Image.new("RGBA", frame.size, (0, 0, 0, 0))
            body_px, costume_px = body.load(), costume.load()
            for yy in range(frame.size[1]):
                for xx in range(frame.size[0]):
                    pixel = body_px[xx, yy]
                    if is_costume(pixel):
                        value = max(pixel[:3])
                        gray = min(255, int(value * 1.15))
                        costume_px[xx, yy] = (gray, gray, gray, pixel[3])
                        body_px[xx, yy] = (0, 0, 0, 0)
            canvas_base.alpha_composite(body, (px, py))
            canvas_costume.alpha_composite(costume, (px, py))
            base_frames.append(canvas_base)
            costume_frames.append(canvas_costume)
        save_frames(os.path.join(base_root, anim), base_frames)
        save_frames(os.path.join(costume_root, anim), costume_frames)
        summary[anim] = len(base_frames)
    return summary


def export_effects():
    summary = {}
    for name, spec in EFFECTS.items():
        kind = spec[0]
        frames = []
        if kind == "meta":
            rects = meta_rects(ref(spec[1]))
            for index in spec[2]:
                x, y, w, h = rects[index]
                frame = clean(sheet(spec[1]).crop((x, y, x + w, y + h)))
                if frame:
                    frames.append(frame)
        elif kind == "box":
            area = sheet(spec[1]).crop(spec[2]).copy()
            pixels = area.load()
            for yy in range(area.size[1]):
                for xx in range(area.size[0]):
                    if is_background(pixels[xx, yy]):
                        pixels[xx, yy] = (0, 0, 0, 0)
            frames = split_columns(area)
        elif kind == "grid":
            image = Image.open(ref(spec[1])).convert("RGBA")
            cols, rows = spec[2], spec[3]
            cw, ch = image.size[0] // cols, image.size[1] // rows
            for r in range(rows):
                for c in range(cols):
                    frame = clean(image.crop((c * cw, r * ch, (c + 1) * cw, (r + 1) * ch)))
                    if frame:
                        frames.append(frame)
        elif kind == "files":
            folder = ref(EFFECT_FOLDERS[name])
            files = sorted(f for f in os.listdir(folder) if f.lower().endswith(".png"))
            for file in files:
                frame = Image.open(os.path.join(folder, file)).convert("RGBA")
                frame.thumbnail((spec[2], spec[2]), Image.LANCZOS)
                frame = clean(frame)
                if frame:
                    frames.append(frame)
        summary[name] = save_frames(os.path.join(OUT, "FX", name), frames)
    return summary


def export_ui():
    menu = sheet(MENU)
    folder = os.path.join(OUT, "UI")
    os.makedirs(folder, exist_ok=True)
    for name, box in UI.items():
        image = clean(menu.crop(box).copy()) or menu.crop(box)
        image.save(os.path.join(folder, f"{name}.png"))
    for extra, name in [(f"{REIM}/otherStuff/UI Spirtes/fatality.png", "fatality_big.png"),
                        (f"{REIM}/otherStuff/StartScreenStuff/mortal-kombat.jpg", "logo_wide.png")]:
        Image.open(ref(extra)).convert("RGBA").save(os.path.join(folder, name))
    return sorted(os.listdir(folder))


def export_stages():
    folder = os.path.join(OUT, "Stages")
    if os.path.isdir(folder):
        shutil.rmtree(folder)
    os.makedirs(folder)
    catalog = []
    for key, (path, floor, title) in STAGES.items():
        image = Image.open(ref(path)).convert("RGB")
        if image.size[0] > 1600:
            image.thumbnail((1600, 1600), Image.LANCZOS)
        image.save(os.path.join(folder, f"{key}.png"))
        catalog.append({"id": key, "name": title, "floor": floor})
    with open(os.path.join(folder, "stages.json"), "w", encoding="utf-8") as handle:
        json.dump({"stages": catalog}, handle, ensure_ascii=False, indent=2)
    return [s["id"] for s in catalog]


def export_audio():
    summary = {}
    root = os.path.join(OUT, "Audio")
    if os.path.isdir(root):
        shutil.rmtree(root)
    for folder, files in AUDIO.items():
        target = os.path.join(root, folder)
        os.makedirs(target, exist_ok=True)
        for source, name in files:
            shutil.copyfile(ref(source), os.path.join(target, name))
        summary[folder] = len(files)
    for folder, source in AUDIO_FOLDERS.items():
        target = os.path.join(root, folder)
        os.makedirs(target, exist_ok=True)
        count = 0
        for file in sorted(os.listdir(ref(source))):
            if file.lower().endswith(".mp3"):
                shutil.copyfile(os.path.join(ref(source), file), os.path.join(target, file))
                count += 1
        summary[folder] = count
    return summary


def export_fonts():
    folder = os.path.join(OUT, "Fonts")
    os.makedirs(folder, exist_ok=True)
    for source, name in FONTS:
        shutil.copyfile(ref(source), os.path.join(folder, name))
    return [name for _, name in FONTS]


def main():
    ensure_repos()
    os.makedirs(OUT, exist_ok=True)
    report = {
        "lutador_base": export_ninja(),
        "efeitos": export_effects(),
        "interface": export_ui(),
        "cenarios": export_stages(),
        "sons": export_audio(),
        "fontes": export_fonts(),
    }
    with open(os.path.join(OUT, "inventario.json"), "w", encoding="utf-8") as handle:
        json.dump(report, handle, ensure_ascii=False, indent=2)
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    sys.exit(main())
