#!/usr/bin/env node
// Process refined Home hero images into BlenderStudio/renders/Home PNGs.
// Pass --install to copy approved layers into Assets/Art/Home/.
// Uses the same sips + PNG writer approach as ArtSource/Gameplay/process_art.mjs.
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../..');
const artSource = path.join(root, 'ArtSource/Home');
const outDir = path.join(root, 'BlenderStudio/renders/Home');
const installDir = path.join(root, 'Assets/Art/Home');
const install = process.argv.includes('--install');

/** Optional inbox for freshly generated Cursor assets. Prefer ArtSource/Home. */
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

function clamp01(v) {
  return Math.min(1, Math.max(0, v));
}

function readRaster(file) {
  const tmp = path.join(os.tmpdir(), `dr_home_${path.basename(file)}.bmp`);
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

function keyImage(img, keyIndex = 0, low = 50 / 255, high = 110 / 255) {
  const { px } = img;
  const kr = px[keyIndex];
  const kg = px[keyIndex + 1];
  const kb = px[keyIndex + 2];
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
    const magentaKey = kr > 0.5 && kb > 0.4 && kg < 0.3;
    if (magentaKey) {
      const spill = Math.max(0, Math.min(px[i], px[i + 2]) - px[i + 1]);
      px[i] -= spill;
      px[i + 2] -= spill;
      px[i + 3] *= clamp01(1 - spill * 1.6);
    }
  }
}

/** Drop soft fringe pixels so opaque plates don't keep parchment halos. */
function dropFringe(img, minAlpha = 0.55) {
  const { width, height, px } = img;
  const next = new Float32Array(px);
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const i = (y * width + x) * 4;
      if (px[i + 3] >= minAlpha) continue;
      next[i] = next[i + 1] = next[i + 2] = next[i + 3] = 0;
    }
  }
  // Also clear any opaque pixel that has mostly transparent neighbors (halo islands).
  for (let y = 1; y < height - 1; y++) {
    for (let x = 1; x < width - 1; x++) {
      const i = (y * width + x) * 4;
      if (next[i + 3] < 0.5) continue;
      let empty = 0;
      for (let dy = -1; dy <= 1; dy++) {
        for (let dx = -1; dx <= 1; dx++) {
          if (dx === 0 && dy === 0) continue;
          if (next[((y + dy) * width + x + dx) * 4 + 3] < 0.2) empty++;
        }
      }
      if (empty >= 6) next[i] = next[i + 1] = next[i + 2] = next[i + 3] = 0;
    }
  }
  img.px = next;
  return img;
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

function opaqueResize(img, ow, oh) {
  return resample(img, ow, oh);
}

/** Harden soft alpha so stone plates don't look ghosted over the temple. */
function hardenAlpha(img, threshold = 0.22) {
  const { px } = img;
  for (let i = 0; i < px.length; i += 4) {
    if (px[i + 3] < threshold) {
      px[i] = px[i + 1] = px[i + 2] = px[i + 3] = 0;
    } else {
      px[i + 3] = 1;
    }
  }
  return img;
}

/**
 * Stamp keyed art onto a solid rounded plate (full opacity).
 * Removes holey chroma-key transparency that reads as white/ghost UI in Unity.
 */
function opaquePlate(img, ow, oh, rgb, pad = 0.04) {
  hardenAlpha(img);
  const plate = { width: ow, height: oh, px: new Float32Array(ow * oh * 4) };
  const [pr, pg, pb] = rgb;
  const rx = Math.max(8, Math.floor(ow * 0.045));
  const ry = Math.max(8, Math.floor(oh * 0.18));
  for (let y = 0; y < oh; y++) {
    for (let x = 0; x < ow; x++) {
      const i = (y * ow + x) * 4;
      // rounded-rect coverage
      const dx = x < rx ? rx - x : x >= ow - rx ? x - (ow - rx - 1) : 0;
      const dy = y < ry ? ry - y : y >= oh - ry ? y - (oh - ry - 1) : 0;
      const inside = dx === 0 || dy === 0 || (dx * dx) / (rx * rx) + (dy * dy) / (ry * ry) <= 1;
      if (!inside) continue;
      plate.px[i] = pr;
      plate.px[i + 1] = pg;
      plate.px[i + 2] = pb;
      plate.px[i + 3] = 1;
    }
  }
  const innerW = Math.max(1, Math.floor(ow * (1 - pad * 2)));
  const innerH = Math.max(1, Math.floor(oh * (1 - pad * 2)));
  const fitted = fitContain(img, innerW, innerH);
  const ox = Math.floor((ow - fitted.width) / 2);
  const oy = Math.floor((oh - fitted.height) / 2);
  for (let y = 0; y < fitted.height; y++) {
    for (let x = 0; x < fitted.width; x++) {
      const si = (y * fitted.width + x) * 4;
      const a = fitted.px[si + 3];
      if (a <= 0.01) continue;
      const di = ((y + oy) * ow + x + ox) * 4;
      const ia = 1 - a;
      plate.px[di] = fitted.px[si] * a + plate.px[di] * ia;
      plate.px[di + 1] = fitted.px[si + 1] * a + plate.px[di + 1] * ia;
      plate.px[di + 2] = fitted.px[si + 2] * a + plate.px[di + 2] * ia;
      plate.px[di + 3] = 1;
    }
  }
  return plate;
}

function ensureSource(name) {
  fs.mkdirSync(artSource, { recursive: true });
  const dst = path.join(artSource, name);
  if (fs.existsSync(dst)) return dst;

  for (const dir of cursorAssetDirs()) {
    const src = path.join(dir, name);
    if (!fs.existsSync(src)) continue;
    fs.copyFileSync(src, dst);
    return dst;
  }

  throw new Error(
    `Missing refined source "${name}". Place it at ${dst}` +
      ' (or set CURSOR_ASSETS_DIR to a folder that contains it).',
  );
}

fs.mkdirSync(outDir, { recursive: true });

const bgSrc = ensureSource('bg_temple_refined.jpg');
const heroSrc = ensureSource('hero_explorer_refined.jpg');
const logoSrc = ensureSource('logo_refined.jpg');
const gemSrc = ensureSource('gem_cluster_refined.jpg');

const bg = opaqueResize(readRaster(bgSrc), 1920, 1080);
writePng(path.join(outDir, 'bg_temple.png'), bg);
console.log('Wrote bg_temple.png');

const hero = readRaster(heroSrc);
keyImage(hero, 0);
const heroTrim = trim(hero);
hardenAlpha(heroTrim, 0.18);
const heroOut = fitContain(heroTrim, 620, 1028);
hardenAlpha(heroOut, 0.12);
writePng(path.join(outDir, 'hero_explorer.png'), heroOut);
console.log('Wrote hero_explorer.png');

// Logo plate: solid dark stone backing so DIAMOND RUSH 2D stays readable on the misty temple.
const logo = readRaster(logoSrc);
keyImage(logo, 0);
const logoTrim = trim(logo);
const logoOut = opaquePlate(logoTrim, 840, 280, [0.14, 0.1, 0.07], 0.03);
writePng(path.join(outDir, 'logo.png'), logoOut);
console.log('Wrote logo.png');

const gems = readRaster(gemSrc);
keyImage(gems, 0);
const gemTrim = trim(gems);
hardenAlpha(gemTrim, 0.18);
const gemOut = fitContain(gemTrim, 540, 632);
hardenAlpha(gemOut, 0.12);
writePng(path.join(outDir, 'gem_cluster.png'), gemOut);
console.log('Wrote gem_cluster.png');

// Composite preview: background + explorer left + gems right (no Unity UI).
const preview = opaqueResize(readRaster(bgSrc), 1920, 1080);
function stamp(dest, src, x, y) {
  for (let sy = 0; sy < src.height; sy++) {
    const dy = y + sy;
    if (dy < 0 || dy >= dest.height) continue;
    for (let sx = 0; sx < src.width; sx++) {
      const dx = x + sx;
      if (dx < 0 || dx >= dest.width) continue;
      const si = (sy * src.width + sx) * 4;
      const di = (dy * dest.width + dx) * 4;
      const a = src.px[si + 3];
      if (a <= 0.01) continue;
      const ia = 1 - a;
      dest.px[di] = src.px[si] * a + dest.px[di] * ia;
      dest.px[di + 1] = src.px[si + 1] * a + dest.px[di + 1] * ia;
      dest.px[di + 2] = src.px[si + 2] * a + dest.px[di + 2] * ia;
      dest.px[di + 3] = 1;
    }
  }
}
stamp(preview, heroOut, 40, 40);
stamp(preview, gemOut, 1920 - gemOut.width - 20, 1080 - gemOut.height - 20);
stamp(preview, logoOut, Math.floor((1920 - logoOut.width) / 2), 20);
writePng(path.join(outDir, 'home_preview.png'), preview);
console.log('Wrote home_preview.png');

// Buttons: painted labels on opaque warm-stone plates (no holey transparency).
const buttonJobs = [
  ['btn_continue_refined.jpg', 'button_continue.png', 720, 168, [0.22, 0.16, 0.1]],
  ['btn_newgame_refined.jpg', 'button_newgame.png', 720, 148, [0.2, 0.14, 0.09]],
  ['btn_settings_refined.jpg', 'button_settings.png', 720, 148, [0.18, 0.13, 0.09]],
];
for (const [srcName, outName, w, h, rgb] of buttonJobs) {
  const src = ensureSource(srcName);
  const img = readRaster(src);
  // Aggressive key: parchment/map and black drop out; stone plaque stays.
  keyImage(img, 0, 70 / 255, 140 / 255);
  dropFringe(img, 0.6);
  const trimmed = trim(img);
  const out = opaquePlate(trimmed, w, h, rgb, 0.035);
  writePng(path.join(outDir, outName), out);
  console.log('Wrote ' + outName);
}

const installFiles = [
  'bg_temple.png',
  'hero_explorer.png',
  'logo.png',
  'gem_cluster.png',
  'button_continue.png',
  'button_newgame.png',
  'button_settings.png',
];

if (install) {
  fs.mkdirSync(installDir, { recursive: true });
  for (const name of installFiles) {
    fs.copyFileSync(path.join(outDir, name), path.join(installDir, name));
    console.log('Installed ' + name);
  }
  console.log('Installed Home art into Assets/Art/Home/');
} else {
  console.log('Preview only — re-run with --install to copy into Assets/Art/Home.');
}
