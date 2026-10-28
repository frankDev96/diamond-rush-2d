// Procedural gameplay tiles not yet in photo sources. Run: node ArtSource/Gameplay/generate_extra_art.mjs
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';

const root = path.resolve(path.dirname(new URL(import.meta.url).pathname), '../..');
const out = path.join(root, 'Assets/Art/Gameplay/Angkor');
const mapOut = path.join(root, 'Assets/Art/Map');

function writePng(file, width, height, rgba) {
  const stride = width * 4;
  const raw = Buffer.alloc((stride + 1) * height);
  for (let y = 0; y < height; y++) {
    raw[y * (stride + 1)] = 0;
    rgba.copy(raw, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  }
  const compressed = zlib.deflateSync(raw);
  const sig = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]);
  const ihdr = Buffer.alloc(25);
  ihdr.writeUInt32BE(13, 0);
  ihdr.write('IHDR', 4);
  ihdr.writeUInt32BE(width, 8);
  ihdr.writeUInt32BE(height, 12);
  ihdr[16] = 8;
  ihdr[17] = 6;
  const crc1 = crc32(Buffer.concat([Buffer.from('IHDR'), ihdr.slice(8, 21)]));
  ihdr.writeUInt32BE(crc1, 21);
  const idat = Buffer.alloc(compressed.length + 12);
  idat.writeUInt32BE(compressed.length, 0);
  idat.write('IDAT', 4);
  compressed.copy(idat, 8);
  idat.writeUInt32BE(crc32(Buffer.concat([Buffer.from('IDAT'), compressed])), 8 + compressed.length);
  const iend = Buffer.from([0, 0, 0, 0, 73, 69, 78, 68, 174, 66, 96, 130]);
  fs.mkdirSync(out, { recursive: true });
  fs.mkdirSync(mapOut, { recursive: true });
  fs.writeFileSync(file, Buffer.concat([sig, ihdr, idat, iend]));
}

function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = (c >>> 1) ^ (0xedb88320 & -(c & 1));
  }
  return ~c >>> 0;
}

function fill(w, h, fn) {
  const b = Buffer.alloc(w * h * 4);
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const i = (y * w + x) * 4;
      const [r, g, bl, a] = fn(x, y, w, h);
      b[i] = r;
      b[i + 1] = g;
      b[i + 2] = bl;
      b[i + 3] = a;
    }
  }
  return b;
}

function stoneTile(w, h, crack = false) {
  return fill(w, h, (x, y) => {
    const n = ((x * 17 + y * 31) % 97) / 97;
    const base = 140 + Math.floor(n * 35);
    let r = base, g = base - 5, b = base - 12;
    if (crack && Math.abs(x - w * 0.55) < 2 && y > h * 0.2 && y < h * 0.75) {
      r -= 40;
      g -= 40;
      b -= 40;
    }
    return [r, g, b, 255];
  });
}

function entity(w, h, draw) {
  return fill(w, h, (x, y) => {
    const cx = w / 2,
      cy = h / 2;
    const inside = draw(x, y, cx, cy, w, h);
    return inside ? inside : [0, 0, 0, 0];
  });
}

const size = 128;
// floor_cracked comes from angkor_floor_tile via process_art.mjs — do not overwrite with procedural grey.
writePng(path.join(out, 'boulder_face.png'), size, size, entity(size, size, (x, y, cx, cy) => {
  const dx = (x - cx) / (size * 0.38);
  const dy = (y - cy) / (size * 0.38);
  if (dx * dx + dy * dy > 1) return null;
  return [210, 185, 140, 255];
}));
writePng(path.join(out, 'snake_green.png'), size, size, entity(size, size, (x, y, cx, cy) => {
  const dx = (x - cx) / (size * 0.45);
  const dy = (y - cy) / (size * 0.2);
  if (dx * dx + dy * dy > 1) return null;
  return [60, 180, 70, 255];
}));
writePng(path.join(out, 'chest_wood.png'), size, size, entity(size, size, (x, y, cx, cy, w, h) => {
  if (Math.abs(x - cx) > w * 0.35 || Math.abs(y - cy) > h * 0.28) return null;
  return [120, 70, 35, 255];
}));
writePng(path.join(out, 'chest_secret.png'), size, size, entity(size, size, (x, y, cx, cy, w, h) => {
  if (Math.abs(x - cx) > w * 0.35 || Math.abs(y - cy) > h * 0.28) return null;
  return [90, 55, 120, 255];
}));
writePng(path.join(out, 'trap_fire.png'), size, size, entity(size, size, (x, y, cx, cy) => {
  const dx = (x - cx) / (size * 0.25);
  const dy = (y - cy) / (size * 0.35);
  if (dx * dx + dy * dy > 1) return null;
  return [255, 140, 30, 255];
}));
writePng(path.join(out, 'trap_spear.png'), size, size, fill(size, size, (x, y, w, h) => {
  if (Math.abs(x - w / 2) < 4 && y > h * 0.15 && y < h * 0.85) return [180, 180, 190, 255];
  return [0, 0, 0, 0];
}));
writePng(path.join(out, 'checkpoint_pad.png'), size, size, entity(size, size, (x, y, cx, cy) => {
  const dx = (x - cx) / (size * 0.42);
  const dy = (y - cy) / (size * 0.42);
  if (dx * dx + dy * dy > 1) return null;
  return [240, 210, 60, 255];
}));

writePng(path.join(mapOut, 'hud_plaque_gold.png'), 256, 128, fill(256, 128, (x, y, w, h) => {
  const border = x < 8 || y < 8 || x > w - 9 || y > h - 9;
  if (border) return [180, 130, 40, 255];
  return [70, 52, 28, 240];
}));

console.log('Wrote procedural gameplay + HUD art to', out);
