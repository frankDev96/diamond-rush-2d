#!/usr/bin/env node
// Convert refined Map/World Select sources into Assets/Art/Map PNGs.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const sourceDir = path.join(root, 'ArtSource/Map');
const outDir = path.join(root, 'Assets/Art/Map');
const previewDir = path.join(root, 'BlenderStudio/renders/Map');

function cursorAssetDirs() {
  const dirs = [];
  if (process.env.CURSOR_ASSETS_DIR) dirs.push(process.env.CURSOR_ASSETS_DIR);
  const projects = path.join(os.homedir(), '.cursor/projects');
  if (!fs.existsSync(projects)) return dirs;
  for (const name of fs.readdirSync(projects)) {
    const assets = path.join(projects, name, 'assets');
    if (fs.existsSync(assets)) dirs.push(assets);
  }
  return dirs;
}

function resolveSource(name) {
  const local = path.join(sourceDir, name);
  if (fs.existsSync(local)) return local;
  for (const dir of cursorAssetDirs()) {
    const src = path.join(dir, name);
    if (!fs.existsSync(src)) continue;
    fs.copyFileSync(src, local);
    return local;
  }
  return null;
}

const jobs = [
  // Full-bleed jungle backdrop — no chroma key.
  { src: 'map_frame.jpg', out: 'map_frame.png', w: 1920, h: 1080, mode: 'full' },
  // Magenta-backed UI plates — Key + trim so pink never ships.
  { src: 'map_parchment.jpg', out: 'map_parchment.png', w: 1240, h: 860, mode: 'key' },
  { src: 'world_row_active.jpg', out: 'world_row_active.png', w: 1120, h: 220, mode: 'key' },
  { src: 'world_row_locked.jpg', out: 'world_row_locked.png', w: 1120, h: 220, mode: 'key' },
  { src: 'node_cleared.jpg', out: 'node_cleared.png', w: 260, h: 272, mode: 'key' },
  { src: 'node_current.jpg', out: 'node_current.png', w: 340, h: 354, mode: 'key' },
  { src: 'node_locked.jpg', out: 'node_locked.png', w: 262, h: 273, mode: 'key' },
  { src: 'preview_card.jpg', out: 'preview_card.png', w: 1120, h: 220, mode: 'key' },
  { src: 'play_button.jpg', out: 'play_button.png', w: 560, h: 160, mode: 'key' },
  { src: 'title_banner.jpg', out: 'title_banner.png', w: 900, h: 140, mode: 'key' },
  { src: 'courtyard_thumb.jpg', out: 'courtyard_thumb.png', w: 560, h: 420, mode: 'full' },
  { src: 'hud_chip.jpg', out: 'hud_chip.png', w: 320, h: 88, mode: 'key' },
  { src: 'hud_plaque_gold.jpg', out: 'hud_plaque_gold.png', w: 256, h: 128, mode: 'key' },
  { src: 'heart.jpg', out: 'heart.png', w: 96, h: 96, mode: 'key' },
];

function clamp01(v) {
  return Math.min(1, Math.max(0, v));
}

function readRaster(file) {
  const tmp = path.join(os.tmpdir(), `dr_map_${path.basename(file)}.bmp`);
  execFileSync('sips', ['-s', 'format', 'bmp', file, '--out', tmp], { stdio: 'ignore' });
  const buf = fs.readFileSync(tmp);
  fs.unlinkSync(tmp);
  const dataOffset = buf.readUInt32LE(10);
  const width = buf.readInt32LE(18);
  const rawHeight = buf.readInt32LE(22);
  const bpp = buf.readUInt16LE(28);
  const height = Math.abs(rawHeight);
  const bytes = bpp / 8;
  const stride = Math.ceil((width * bytes) / 4) * 4;
  const px = new Float32Array(width * height * 4);
  for (let y = 0; y < height; y++) {
    const srcRow = rawHeight > 0 ? height - 1 - y : y;
    for (let x = 0; x < width; x++) {
      const o = dataOffset + srcRow * stride + x * bytes;
      const i = (y * width + x) * 4;
      px[i] = buf[o + 2] / 255;
      px[i + 1] = buf[o + 1] / 255;
      px[i + 2] = buf[o] / 255;
      px[i + 3] = 1;
    }
  }
  return { width, height, px };
}

function keyImage(img) {
  const low = 35 / 255;
  const high = 95 / 255;
  const { px } = img;
  const kr = px[0];
  const kg = px[1];
  const kb = px[2];
  for (let i = 0; i < px.length; i += 4) {
    const dr = px[i] - kr;
    const dg = px[i + 1] - kg;
    const db = px[i + 2] - kb;
    const a = Math.min(1, Math.max(0, (Math.sqrt(dr * dr + dg * dg + db * db) - low) / (high - low)));
    if (a <= 0) {
      px[i] = px[i + 1] = px[i + 2] = px[i + 3] = 0;
      continue;
    }
    if (a < 1) {
      px[i] = clamp01((px[i] - kr * (1 - a)) / a);
      px[i + 1] = clamp01((px[i + 1] - kg * (1 - a)) / a);
      px[i + 2] = clamp01((px[i + 2] - kb * (1 - a)) / a);
    }
    px[i + 3] = a;
  }
}

function crop(img, x0, y0, w, h) {
  const out = new Float32Array(w * h * 4);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const s = ((y + y0) * img.width + x + x0) * 4;
      const d = (y * w + x) * 4;
      for (let c = 0; c < 4; c++) out[d + c] = img.px[s + c];
    }
  }
  return { width: w, height: h, px: out };
}

function trim(img) {
  let minX = img.width;
  let minY = img.height;
  let maxX = -1;
  let maxY = -1;
  for (let y = 0; y < img.height; y++) {
    for (let x = 0; x < img.width; x++) {
      if (img.px[(y * img.width + x) * 4 + 3] < 0.05) continue;
      minX = Math.min(minX, x);
      minY = Math.min(minY, y);
      maxX = Math.max(maxX, x);
      maxY = Math.max(maxY, y);
    }
  }
  if (maxX < 0) return img;
  minX = Math.max(0, minX - 2);
  minY = Math.max(0, minY - 2);
  maxX = Math.min(img.width - 1, maxX + 2);
  maxY = Math.min(img.height - 1, maxY + 2);
  return crop(img, minX, minY, maxX - minX + 1, maxY - minY + 1);
}

function bilinear(img, u, v, out) {
  const { width, height, px } = img;
  const x0 = Math.min(width - 1, Math.max(0, Math.floor(u)));
  const y0 = Math.min(height - 1, Math.max(0, Math.floor(v)));
  const x1 = Math.min(x0 + 1, width - 1);
  const y1 = Math.min(y0 + 1, height - 1);
  const tx = clamp01(u - x0);
  const ty = clamp01(v - y0);
  for (let c = 0; c < 4; c++) {
    const top = px[(y0 * width + x0) * 4 + c] * (1 - tx) + px[(y0 * width + x1) * 4 + c] * tx;
    const bottom = px[(y1 * width + x0) * 4 + c] * (1 - tx) + px[(y1 * width + x1) * 4 + c] * tx;
    out[c] = top * (1 - ty) + bottom * ty;
  }
}

function resample(img, ow, oh) {
  const out = new Float32Array(ow * oh * 4);
  const samples = Math.max(1, Math.ceil(Math.max(img.width / ow, img.height / oh)));
  const c = [0, 0, 0, 0];
  for (let y = 0; y < oh; y++) {
    for (let x = 0; x < ow; x++) {
      let r = 0;
      let g = 0;
      let b = 0;
      let a = 0;
      for (let sy = 0; sy < samples; sy++) {
        for (let sx = 0; sx < samples; sx++) {
          bilinear(
            img,
            ((x + (sx + 0.5) / samples) / ow) * img.width - 0.5,
            ((y + (sy + 0.5) / samples) / oh) * img.height - 0.5,
            c,
          );
          r += c[0] * c[3];
          g += c[1] * c[3];
          b += c[2] * c[3];
          a += c[3];
        }
      }
      const i = (y * ow + x) * 4;
      if (a > 0) {
        out[i] = r / a;
        out[i + 1] = g / a;
        out[i + 2] = b / a;
        out[i + 3] = a / (samples * samples);
      }
    }
  }
  return { width: ow, height: oh, px: out };
}

function fitContain(img, ow, oh) {
  const scale = Math.min(ow / img.width, oh / img.height);
  const tw = Math.max(1, Math.round(img.width * scale));
  const th = Math.max(1, Math.round(img.height * scale));
  const sized = resample(img, tw, th);
  const canvas = { width: ow, height: oh, px: new Float32Array(ow * oh * 4) };
  const ox = Math.floor((ow - tw) / 2);
  const oy = Math.floor((oh - th) / 2);
  for (let y = 0; y < th; y++) {
    for (let x = 0; x < tw; x++) {
      const s = (y * tw + x) * 4;
      const d = ((y + oy) * ow + x + ox) * 4;
      for (let c = 0; c < 4; c++) canvas.px[d + c] = sized.px[s + c];
    }
  }
  return canvas;
}

function crc32(buf) {
  let c = ~0;
  for (let n = 0; n < buf.length; n++) {
    c ^= buf[n];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }
  return ~c >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type), data]);
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

function writePng(file, img) {
  const { width, height, px } = img;
  const raw = Buffer.alloc((width * 4 + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (width * 4 + 1)] = 0;
    for (let x = 0; x < width * 4; x++) {
      raw[y * (width * 4 + 1) + 1 + x] = Math.round(clamp01(px[y * width * 4 + x]) * 255);
    }
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0);
  ihdr.writeUInt32BE(height, 4);
  ihdr[8] = 8;
  ihdr[9] = 6;
  fs.writeFileSync(
    file,
    Buffer.concat([
      Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
      chunk('IHDR', ihdr),
      chunk('IDAT', zlib.deflateSync(raw)),
      chunk('IEND', Buffer.alloc(0)),
    ]),
  );
}

fs.mkdirSync(sourceDir, { recursive: true });
fs.mkdirSync(outDir, { recursive: true });
fs.mkdirSync(previewDir, { recursive: true });

for (const job of jobs) {
  const local = resolveSource(job.src);
  if (!local) {
    console.error(
      `Missing ${job.src}. Place it at ${path.join(sourceDir, job.src)}` +
        ' (or set CURSOR_ASSETS_DIR).',
    );
    continue;
  }
  let img = readRaster(local);
  if (job.mode === 'key') {
    keyImage(img);
    img = trim(img);
    img = fitContain(img, job.w, job.h);
  } else {
    img = resample(img, job.w, job.h);
  }
  const dest = path.join(outDir, job.out);
  writePng(dest, img);
  fs.copyFileSync(dest, path.join(previewDir, job.out));
  console.log(job.out, `${job.w}x${job.h}`);
}

console.log('Installed polished World Select art into Assets/Art/Map');
