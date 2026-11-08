#!/usr/bin/env python3
"""
Generate sprite sheets for Space Distortion boss mod.
Creates:
- Boss sprite sheet (8 frames, 120x120 each)
- Glow effect texture
- Projectile textures
"""

from PIL import Image, ImageDraw, ImageFilter
import math
import os

OUTPUT_DIR = "/root/SpaceDistortionMod/sprites"

def create_boss_spritesheet():
    """Create the main boss sprite sheet with 8 animation frames"""
    frame_width = 120
    frame_height = 120
    frame_count = 8
    sheet_width = frame_width
    sheet_height = frame_height * frame_count

    # Create sprite sheet
    sheet = Image.new('RGBA', (sheet_width, sheet_height), (0, 0, 0, 0))

    for frame in range(frame_count):
        img = Image.new('RGBA', (frame_width, frame_height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)

        center = (frame_width // 2, frame_height // 2)
        time = frame * 0.5  # Animation phase

        # Base colors
        core_color = (155, 89, 182, 255)      # Purple
        outer_color = (100, 50, 140, 200)     # Darker purple
        glow_color = (180, 120, 220, 150)     # Light purple glow
        accent_color = (255, 100, 200, 255)   # Pink accent
        void_color = (20, 5, 40, 255)         # Near black void

        # Animation parameters
        pulse = 1 + 0.15 * math.sin(time * 2)
        rotation = time * 0.3

        # Draw multiple layers for depth
        # Layer 1: Outer distortion rings
        for ring in range(4):
            ring_radius = 45 * pulse - ring * 8
            ring_alpha = int(100 - ring * 20)
            ring_col = (*outer_color[:3], ring_alpha)

            # Draw ring segments
            for seg in range(8):
                angle_start = rotation + seg * math.pi / 4 + ring * 0.2
                angle_end = angle_start + math.pi / 6
                # Draw arc using polygon approximation
                points = [center]
                for i in range(6):
                    a = angle_start + (angle_end - angle_start) * i / 5
                    r = ring_radius + 5 * math.sin(time * 3 + seg + i)
                    points.append((
                        center[0] + r * math.cos(a),
                        center[1] + r * math.sin(a)
                    ))
                draw.polygon(points, fill=ring_col)

        # Layer 2: Main body - cosmic entity shape
        body_radius = 35 * pulse
        # Main ellipse
        draw.ellipse([
            center[0] - body_radius, center[1] - body_radius * 0.8,
            center[0] + body_radius, center[1] + body_radius * 0.8
        ], fill=core_color)

        # Inner void/core
        void_radius = int(body_radius * 0.4)
        draw.ellipse([
            center[0] - void_radius, center[1] - void_radius,
            center[0] + void_radius, center[1] + void_radius
        ], fill=void_color)

        # Layer 3: Rotating arms/tendrils
        for arm in range(6):
            arm_angle = rotation + arm * math.pi / 3
            arm_length = body_radius * 1.3
            arm_width = 8

            # Arm end position
            end_x = center[0] + arm_length * math.cos(arm_angle)
            end_y = center[1] + arm_length * math.sin(arm_angle)

            # Draw arm as thick line with taper
            points = []
            for i in range(10):
                t = i / 9
                r = body_radius * 0.5 + (arm_length - body_radius * 0.5) * t
                w = arm_width * (1 - t * 0.7)
                a = arm_angle + 0.1 * math.sin(time * 4 + arm + i)
                points.append((
                    center[0] + r * math.cos(a) - w/2 * math.sin(a),
                    center[1] + r * math.sin(a) + w/2 * math.cos(a)
                ))
            # Reverse for other side
            for i in range(9, -1, -1):
                t = i / 9
                r = body_radius * 0.5 + (arm_length - body_radius * 0.5) * t
                w = arm_width * (1 - t * 0.7)
                a = arm_angle + 0.1 * math.sin(time * 4 + arm + i)
                points.append((
                    center[0] + r * math.cos(a) + w/2 * math.sin(a),
                    center[1] + r * math.sin(a) - w/2 * math.cos(a)
                ))

            arm_color = (*accent_color[:3], 200 - arm * 20)
            draw.polygon(points, fill=arm_color)

        # Layer 4: Energy particles orbiting
        for p in range(12):
            p_angle = rotation * 2 + p * math.pi / 6 + time * 0.5
            p_radius = body_radius * 1.1 + 10 * math.sin(time * 2 + p)
            p_x = center[0] + p_radius * math.cos(p_angle)
            p_y = center[1] + p_radius * math.sin(p_angle)
            p_size = 3 + 2 * math.sin(time * 3 + p)
            p_col = (*glow_color[:3], 180)
            draw.ellipse([
                p_x - p_size, p_y - p_size,
                p_x + p_size, p_y + p_size
            ], fill=p_col)

        # Layer 5: Core glow
        for glow_layer in range(3):
            g_r = void_radius + glow_layer * 4
            g_alpha = 60 - glow_layer * 15
            draw.ellipse([
                center[0] - g_r, center[1] - g_r,
                center[0] + g_r, center[1] + g_r
            ], fill=(*glow_color[:3], g_alpha))

        # Layer 6: Central singularity
        draw.ellipse([
            center[0] - 4, center[1] - 4,
            center[0] + 4, center[1] + 4
        ], fill=(255, 200, 255, 255))

        # Paste frame into sheet
        sheet.paste(img, (0, frame * frame_height), img)

    # Save
    sheet.save(os.path.join(OUTPUT_DIR, "SpaceDistortionBoss.png"))
    print(f"Saved boss sprite sheet: {sheet_width}x{sheet_height} ({frame_count} frames)")

def create_glow_texture():
    """Create glow effect texture"""
    size = 256
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = (size // 2, size // 2)

    # Radial gradient glow
    for r in range(size // 2, 0, -2):
        alpha = int(200 * (1 - r / (size // 2)) ** 2)
        color = (155, 89, 182, alpha)
        draw.ellipse([
            center[0] - r, center[1] - r,
            center[0] + r, center[1] + r
        ], fill=color)

    # Add some noise/texture
    for _ in range(200):
        x = center[0] + (hash(str(_)) % (size // 2)) * (1 if _ % 2 == 0 else -1)
        y = center[1] + (hash(str(_ * 7)) % (size // 2)) * (1 if _ % 3 == 0 else -1)
        if 0 <= x < size and 0 <= y < size:
            draw.point((x, y), fill=(180, 120, 220, 100))

    img.save(os.path.join(OUTPUT_DIR, "SpaceDistortionGlow.png"))
    print(f"Saved glow texture: {size}x{size}")

def create_voidbolt_texture():
    """Create void bolt projectile texture"""
    size = 32
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = (size // 2, size // 2)

    # Bolt shape - elongated diamond
    points = [
        (center[0], 2),           # Top
        (center[0] + 6, center[1]), # Right
        (center[0], size - 2),    # Bottom
        (center[0] - 6, center[1])  # Left
    ]
    draw.polygon(points, fill=(155, 89, 182, 255))

    # Inner glow
    inner_points = [
        (center[0], 6),
        (center[0] + 3, center[1]),
        (center[0], size - 6),
        (center[0] - 3, center[1])
    ]
    draw.polygon(inner_points, fill=(180, 120, 220, 200))

    # Core
    draw.ellipse([center[0]-2, center[1]-2, center[0]+2, center[1]+2], fill=(255, 200, 255, 255))

    img.save(os.path.join(OUTPUT_DIR, "VoidBolt.png"))
    print(f"Saved void bolt texture: {size}x{size}")

def create_distortionwave_texture():
    """Create distortion wave projectile texture"""
    size = 64
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = (size // 2, size // 2)

    # Concentric rings
    for ring in range(5):
        r = 25 - ring * 4
        alpha = 150 - ring * 25
        draw.ellipse([
            center[0] - r, center[1] - r,
            center[0] + r, center[1] + r
        ], outline=(155, 89, 182, alpha), width=2)

    # Central distortion
    draw.ellipse([center[0]-8, center[1]-8, center[0]+8, center[1]+8], fill=(100, 50, 140, 200))
    draw.ellipse([center[0]-4, center[1]-4, center[0]+4, center[1]+4], fill=(20, 5, 40, 255))

    img.save(os.path.join(OUTPUT_DIR, "DistortionWave.png"))
    print(f"Saved distortion wave texture: {size}x{size}")

def create_starshard_texture():
    """Create star shard projectile texture"""
    size = 24
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = (size // 2, size // 2)

    # Star shape (4-pointed)
    points = []
    for i in range(8):
        angle = i * math.pi / 4
        r = 10 if i % 2 == 0 else 5
        points.append((
            center[0] + r * math.cos(angle),
            center[1] + r * math.sin(angle)
        ))
    draw.polygon(points, fill=(255, 220, 100, 255))

    # Inner star
    inner_points = []
    for i in range(8):
        angle = i * math.pi / 4 + math.pi / 8
        r = 6 if i % 2 == 0 else 3
        inner_points.append((
            center[0] + r * math.cos(angle),
            center[1] + r * math.sin(angle)
        ))
    draw.polygon(inner_points, fill=(255, 255, 200, 255))

    # Core
    draw.ellipse([center[0]-2, center[1]-2, center[0]+2, center[1]+2], fill=(255, 255, 255, 255))

    img.save(os.path.join(OUTPUT_DIR, "StarShard.png"))
    print(f"Saved star shard texture: {size}x{size}")

def create_blackhole_texture():
    """Create black hole projectile texture"""
    size = 80
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = (size // 2, size // 2)

    # Outer accretion disk rings
    for ring in range(8):
        r = 35 - ring * 3
        alpha = 80 - ring * 8
        hue_shift = ring * 15
        color = (
            min(155 + hue_shift, 180),
            max(89 - hue_shift // 2, 30),
            min(182 + hue_shift, 220),
            alpha
        )
        draw.ellipse([
            center[0] - r, center[1] - r,
            center[0] + r, center[1] + r
        ], outline=color, width=1)

    # Event horizon
    draw.ellipse([
        center[0] - 12, center[1] - 12,
        center[0] + 12, center[1] + 12
    ], fill=(5, 0, 15, 255))

    # Singularity
    draw.ellipse([
        center[0] - 4, center[1] - 4,
        center[0] + 4, center[1] + 4
    ], fill=(0, 0, 0, 255))

    # Bright center point
    draw.point(center, fill=(100, 50, 150, 255))

    img.save(os.path.join(OUTPUT_DIR, "BlackHole.png"))
    print(f"Saved black hole texture: {size}x{size}")

def create_spawnitem_texture():
    """Create spawn item (Cosmic Rift Key) texture"""
    size = 32
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    center = (size // 2, size // 2)

    # Key shape - ornate cosmic key
    # Handle
    draw.rectangle([center[0]-2, center[1]+2, center[0]+2, size-2], fill=(80, 40, 100, 255))

    # Key head - circular with void
    draw.ellipse([center[0]-8, center[1]-10, center[0]+8, center[1]+6], fill=(100, 50, 140, 255))
    draw.ellipse([center[0]-4, center[1]-6, center[0]+4, center[1]+2], fill=(20, 5, 40, 255))

    # Teeth
    for t in range(3):
        x = center[0] - 5 + t * 5
        draw.rectangle([x, center[1]+2, x+2, center[1]+8], fill=(155, 89, 182, 255))

    # Glow accents
    draw.ellipse([center[0]-6, center[1]-8, center[0]-2, center[1]-4], fill=(180, 120, 220, 200))
    draw.ellipse([center[0]+2, center[1]-8, center[0]+6, center[1]-4], fill=(180, 120, 220, 200))

    img.save(os.path.join(OUTPUT_DIR, "CosmicRiftKey.png"))
    print(f"Saved spawn item texture: {size}x{size}")

if __name__ == "__main__":
    os.makedirs(OUTPUT_DIR, exist_ok=True)

    print("Generating Space Distortion sprites...")
    create_boss_spritesheet()
    create_glow_texture()
    create_voidbolt_texture()
    create_distortionwave_texture()
    create_starshard_texture()
    create_blackhole_texture()
    create_spawnitem_texture()
    print("All sprites generated successfully!")