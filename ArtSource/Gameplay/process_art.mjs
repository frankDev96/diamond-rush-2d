// Mirror of Assets/Editor/GameplayArtImporter.cs for use while the Unity editor holds the project lock.
// Usage: node ArtSource/Gameplay/process_art.mjs   (macOS, needs `sips`)
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import zlib from 'node:zlib';

const root = path.resolve(path.dirname(new URL(import.meta.url).pathname), '../..');
const sourceDir = path.join(root, 'ArtSource/Gameplay/Angkor');
const outputDir = path.join(root, 'Assets/Art/Gameplay/Angkor');

const entries = [
  ['floor_stone_single.jpg', 'floor_stone.png', 'full', 128],
  ['floor_stone_single.jpg', 'floor_stone_alt.png', 'full', 128, { rotate90: true }],
  ['dirt_soft.jpg', 'dirt_soft.png', 'full', 128],
  ['wall_block_full.jpg', 'wall_block.png', 'full', 128],
  ['red_diamond.jpg', 'red_diamond.png', 'key', 128],
  ['exit_door_closed.jpg', 'exit_door_closed.png', 'key', 384],
  ['exit_door_open.jpg', 'exit_door_open.png', 'key', 384],
  ['explorer_down.jpg', 'explorer_down.png', 'key', 256],
  ['explorer_up.jpg', 'explorer_up.png', 'key', 256],
  ['explorer_side.jpg', 'explorer_side.png', 'key', 256],
  ['jungle_border_16x9.jpg', 'jungle_border_16x9.png', 'full', 1920],
  ['joystick_base.jpg', 'joystick_base.png', 'key', 320, { clearCenter: 0.165 }],
  ['joystick_knob.jpg', 'joystick_knob.png', 'key', 160],
  ['btn_restart.jpg', 'btn_restart.png', 'key', 192],
  ['btn_inventory.jpg', 'btn_inventory.png', 'key', 192],
  ['btn_pause.jpg', 'btn_pause.png', 'key', 192],
  ['red_diamond.jpg', 'icon_red.png', 'key', 96],
  ['icon_purple.jpg', 'icon_purple.png', 'key', 96],
  ['icon_lives_head.jpg', 'icon_lives_head.png', 'key', 96],
  ['fx_dust_dirt.jpg', 'dust_puff.png', 'left', 64],
  ['fx_dust_dirt.jpg', 'dirt_chunk.png', 'right', 64],
];

function readJpeg(file) {
  const tmp = path.join(os.tmpdir(), `dr_${path.basename(file)}.bmp`);
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
  const low = 50 / 255, high = 110 / 255;
  const { px } = img;
  const kr = px[0], kg = px[1], kb = px[2];
  for (let i = 0; i < px.length; i += 4) {
    const dr = px[i] - kr, dg = px[i + 1] - kg, db = px[i + 2] - kb;
    const a = Math.min(1, Math.max(0, (Math.sqrt(dr * dr + dg * dg + db * db) - low) / (high - low)));
    if (a <= 0) { px[i] = px[i + 1] = px[i + 2] = px[i + 3] = 0; continue; }
    if (a < 1) {
      px[i] = clamp01((px[i] - kr * (1 - a)) / a);
      px[i + 1] = clamp01((px[i + 1] - kg * (1 - a)) / a);
      px[i + 2] = clamp01((px[i + 2] - kb * (1 - a)) / a);
    }
    px[i + 3] = a;
    despill(px, i, kr, kg, kb);
  }
}

function despill(px, i, kr, kg, kb) {
  const magentaKey = kr > 0.5 && kb > 0.4 && kg < 0.3;
  const greenKey = kg > 0.5 && kr < 0.5 && kb < 0.3;
  let spill = 0;
  if (magentaKey) {
    spill = Math.max(0, Math.min(px[i], px[i + 2]) - px[i + 1]);
    px[i] -= spill; px[i + 2] -= spill;
  } else if (greenKey) {
    spill = Math.max(0, px[i + 1] - Math.max(px[i], px[i + 2]));
    px[i + 1] -= spill;
  }
  px[i + 3] *= clamp01(1 - spill * 1.6);
}

function clearCenter(img, fraction) {
  const { width, height, px } = img;
  const cx = width / 2, cy = height / 2, r = width * fraction, feather = width * 0.01;
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    const d = Math.hypot(x - cx, y - cy);
    if (d > r + feather) continue;
    px[(y * width + x) * 4 + 3] *= clamp01((d - r) / feather);
  }
}

function crop(img, x0, y0, w, h) {
  const out = new Float32Array(w * h * 4);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const s = ((y + y0) * img.width + x + x0) * 4, d = (y * w + x) * 4;
    for (let c = 0; c < 4; c++) out[d + c] = img.px[s + c];
  }
  return { width: w, height: h, px: out };
}

function trim(img) {
  let minX = img.width, minY = img.height, maxX = -1, maxY = -1;
  for (let y = 0; y < img.height; y++) for (let x = 0; x < img.width; x++) {
    if (img.px[(y * img.width + x) * 4 + 3] < 0.05) continue;
    minX = Math.min(minX, x); minY = Math.min(minY, y); maxX = Math.max(maxX, x); maxY = Math.max(maxY, y);
  }
  if (maxX < 0) return img;
  minX = Math.max(0, minX - 2); minY = Math.max(0, minY - 2);
  maxX = Math.min(img.width - 1, maxX + 2); maxY = Math.min(img.height - 1, maxY + 2);
  return crop(img, minX, minY, maxX - minX + 1, maxY - minY + 1);
}

function rotate90(img) {
  const { width, height, px } = img;
  const out = new Float32Array(px.length);
  for (let y = 0; y < height; y++) for (let x = 0; x < width; x++) {
    const s = (y * width + x) * 4, d = (x * height + (height - 1 - y)) * 4;
    for (let c = 0; c < 4; c++) out[d + c] = px[s + c];
  }
  return { width: height, height: width, px: out };
}

function bilinear(img, u, v, out) {
  const { width, height, px } = img;
  const x0 = Math.min(width - 1, Math.max(0, Math.floor(u))), y0 = Math.min(height - 1, Math.max(0, Math.floor(v)));
  const x1 = Math.min(x0 + 1, width - 1), y1 = Math.min(y0 + 1, height - 1);
  const tx = clamp01(u - x0), ty = clamp01(v - y0);
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
  for (let y = 0; y < oh; y++) for (let x = 0; x < ow; x++) {
    let r = 0, g = 0, b = 0, a = 0;
    for (let sy = 0; sy < samples; sy++) for (let sx = 0; sx < samples; sx++) {
      bilinear(img, ((x + (sx + 0.5) / samples) / ow) * img.width - 0.5, ((y + (sy + 0.5) / samples) / oh) * img.height - 0.5, c);
      r += c[0] * c[3]; g += c[1] * c[3]; b += c[2] * c[3]; a += c[3];
    }
    const i = (y * ow + x) * 4;
    if (a > 0) { out[i] = r / a; out[i + 1] = g / a; out[i + 2] = b / a; out[i + 3] = a / (samples * samples); }
  }
  return { width: ow, height: oh, px: out };
}

function crc32(buf) {
  let c = ~0;
  for (let n = 0; n < buf.length; n++) { c ^= buf[n]; for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1)); }
  return ~c >>> 0;
}

function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const body = Buffer.concat([Buffer.from(type), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(body));
  return Buffer.concat([len, body, crc]);
}

function writePng(file, img) {
  const { width, height, px } = img;
  const raw = Buffer.alloc((width * 4 + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (width * 4 + 1)] = 0;
    for (let x = 0; x < width * 4; x++) raw[y * (width * 4 + 1) + 1 + x] = Math.round(clamp01(px[y * width * 4 + x]) * 255);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(width, 0); ihdr.writeUInt32BE(height, 4); ihdr[8] = 8; ihdr[9] = 6;
  fs.writeFileSync(file, Buffer.concat([
    Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]),
    chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw)), chunk('IEND', Buffer.alloc(0)),
  ]));
}

function clamp01(v) { return v < 0 ? 0 : v > 1 ? 1 : v; }

fs.mkdirSync(outputDir, { recursive: true });
for (const [source, output, mode, size, opts = {}] of entries) {
  let img = readJpeg(path.join(sourceDir, source));
  if (mode === 'left') img = crop(img, 0, 0, Math.floor(img.width / 2), img.height);
  if (mode === 'right') img = crop(img, Math.floor(img.width / 2), 0, Math.floor(img.width / 2), img.height);
  if (mode !== 'full') {
    keyImage(img);
    if (opts.clearCenter) clearCenter(img, opts.clearCenter);
    img = trim(img);
  }
  if (opts.rotate90) img = rotate90(img);
  let ow, oh;
  if (mode === 'full' && img.width !== img.height) { ow = size; oh = Math.round((size * img.height) / img.width); }
  else { const s = size / Math.max(img.width, img.height); ow = Math.max(1, Math.round(img.width * s)); oh = Math.max(1, Math.round(img.height * s)); }
  writePng(path.join(outputDir, output), resample(img, ow, oh));
  console.log(`${output} ${ow}x${oh}`);
}
