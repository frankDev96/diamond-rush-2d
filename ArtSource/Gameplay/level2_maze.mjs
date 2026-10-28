// Angkor Level 2 from 1408×896 / 36px-tile mockup coordinates.
// PRD: Push Boulders! — dig, face boulders, reds/chests/purple. No snakes/potions.
// Usage: /opt/homebrew/opt/node@22/bin/node ArtSource/Gameplay/level2_maze.mjs
const TILE = 36;
const W = 40,
  H = 26;
const REQUIRED = 3,
  TOTAL = 5;
const g = Array.from({ length: H }, () => Array(W).fill('#'));

const cx = (px) => Math.floor(px / TILE);
const cy = (py) => Math.floor(py / TILE);

function clampOpen(x, y) {
  return x > 0 && x < W - 1 && y > 0 && y < H - 1;
}

function openBox(x0, y0, x1, y1) {
  const xa = Math.min(x0, x1),
    xb = Math.max(x0, x1);
  const ya = Math.min(y0, y1),
    yb = Math.max(y0, y1);
  for (let y = ya; y <= yb; y++)
    for (let x = xa; x <= xb; x++) {
      if (clampOpen(x, y)) g[y][x] = '.';
    }
}

function fillBox(x0, y0, x1, y1, c) {
  const xa = Math.min(x0, x1),
    xb = Math.max(x0, x1);
  const ya = Math.min(y0, y1),
    yb = Math.max(y0, y1);
  for (let y = ya; y <= yb; y++)
    for (let x = xa; x <= xb; x++) {
      if (clampOpen(x, y)) g[y][x] = c;
    }
}

function set(x, y, c) {
  if (x < 0 || y < 0 || x >= W || y >= H) return;
  if ((x === 0 || x === W - 1 || y === H - 1) && c !== '#') return;
  if (y === 0 && c !== '#' && c !== 'E') return;
  g[y][x] = c;
}

function place(px, py, c) {
  set(cx(px), cy(py), c);
}

function tunnel(x0, y0, x1, y1) {
  let x = x0,
    y = y0;
  while (x !== x1) {
    if (clampOpen(x, y)) g[y][x] = g[y][x] === '#' ? '.' : g[y][x];
    x += x < x1 ? 1 : -1;
  }
  while (y !== y1) {
    if (clampOpen(x, y)) g[y][x] = g[y][x] === '#' ? '.' : g[y][x];
    y += y < y1 ? 1 : -1;
  }
  if (clampOpen(x1, y1)) g[y1][x1] = g[y1][x1] === '#' ? '.' : g[y1][x1];
}

// --- Rooms from playable floor boxes ---
openBox(cx(198), cy(495), cx(588), cy(760)); // SW Excavation
openBox(cx(278), cy(120), cx(530), cy(368)); // Hall of Idols
openBox(cx(618), cy(72), cx(798), cy(185)); // Sacred Spring
openBox(cx(616), cy(315), cx(796), cy(502)); // Central Hub
openBox(cx(880), cy(120), cx(1118), cy(222)); // Upper Snake Room
openBox(cx(880), cy(335), cx(1122), cy(508)); // Treasure Vault
openBox(cx(1200), cy(250), cx(1348), cy(525)); // Serpent Lair
openBox(cx(69), cy(235), cx(215), cy(415)); // West Annex
openBox(cx(700), cy(625), cx(1250), cy(815)); // SE Snake Pit

// Corridors
tunnel(cx(228), cy(325), cx(278), cy(244));
tunnel(cx(395), cy(390), cx(395), cy(495));
tunnel(cx(570), cy(130), cx(618), cy(128));
tunnel(cx(808), cy(130), cx(880), cy(170));
tunnel(cx(708), cy(210), cx(706), cy(315));
tunnel(cx(815), cy(470), cx(880), cy(420));
tunnel(cx(705), cy(555), cx(705), cy(625));
tunnel(cx(1000), cy(235), cx(1000), cy(335));
tunnel(cx(1145), cy(350), cx(1200), cy(350));
tunnel(cx(1145), cy(500), cx(1200), cy(500));
tunnel(cx(600), cy(650), cx(700), cy(700));

// SW dirt mound
fillBox(cx(340), cy(488), cx(515), cy(718), 'd');
openBox(cx(198), cy(700), cx(280), cy(760));
openBox(cx(360), cy(720), cx(450), cy(760));

// Sacred Spring as dig chamber
fillBox(cx(618), cy(72), cx(798), cy(185), 'd');

place(396, 755, 'P');

{
  const ex = cx(396);
  set(ex, 0, 'E');
  set(ex + 1, 0, 'E');
  set(ex, 1, '.');
  set(ex + 1, 1, '.');
  set(ex, 2, '.');
  set(ex + 1, 2, '.');
}

// Idol Hall face boulders
place(345, 134, 'B');
place(448, 134, 'B');
place(306, 224, 'B');
place(484, 224, 'B');
place(358, 305, 'B');
place(434, 305, 'B');
place(395, 412, 'B');
place(572, 608, 'B');
place(398, 668, 'B');
place(472, 668, 'B');

place(396, 502, 'T');
place(898, 332, 'T');
place(1102, 355, 'T');
place(902, 416, 'T');
place(952, 406, 't');
place(902, 482, 'T');

place(138, 348, 'g');
place(668, 387, 'g');
place(952, 450, 'g');
place(240, 560, 'g');

// Dig-gated reds LAST — seal 3×3 dirt around each (center = r)
const reds = [
  [631, 387], // Hub
  [706, 418],
  [472, 616], // SW dirt
  [670, 120], // Sacred Spring
  [902, 660], // SE pit
];

function sealDigRed(px, py) {
  const x = cx(px),
    y = cy(py);
  for (let dy = -1; dy <= 1; dy++)
    for (let dx = -1; dx <= 1; dx++) {
      if (clampOpen(x + dx, y + dy)) g[y + dy][x + dx] = 'd';
    }
  set(x, y, 'r');
}

for (const [px, py] of reds) sealDigRed(px, py);

// Ensure spawn stays open and EE path clear of dirt
place(396, 755, 'P');
{
  const ex = cx(396);
  set(ex, 1, '.');
  set(ex + 1, 1, '.');
  set(ex, 2, '.');
  set(ex + 1, 2, '.');
}

const rows = g.map((r) => r.join(''));

const errors = [];
if (rows.length !== H || rows.some((r) => r.length !== W)) errors.push('size');
const count = (c) => rows.join('').split(c).length - 1;
if (count('P') !== 1) errors.push(`need one P (have ${count('P')})`);
if (!rows[0].includes('EE') || count('E') !== 2) errors.push('need EE in top row');
if (count('r') !== TOTAL) errors.push(`need ${TOTAL} reds, have ${count('r')}`);
for (let x = 0; x < W; x++) if (rows[H - 1][x] !== '#') errors.push('bottom border open at ' + x);
for (let y = 0; y < H; y++) if (rows[y][0] !== '#' || rows[y][W - 1] !== '#') errors.push('side border at y=' + y);
for (let x = 0; x < W; x++) {
  const c = rows[0][x];
  if (c !== '#' && c !== 'E') errors.push(`top border bad at ${x}='${c}'`);
}

function flood(passDirt) {
  const seen = new Set();
  let start;
  rows.forEach((r, y) => {
    const x = r.indexOf('P');
    if (x >= 0) start = [x, y];
  });
  if (!start) return seen;
  const q = [start];
  seen.add(start.join(','));
  while (q.length) {
    const [x, y] = q.shift();
    for (const [dx, dy] of [
      [1, 0],
      [-1, 0],
      [0, 1],
      [0, -1],
    ]) {
      const nx = x + dx,
        ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
      const c = rows[ny][nx];
      if (c === '#' || c === 'B' || (c === 'd' && !passDirt)) continue;
      const k = nx + ',' + ny;
      if (seen.has(k)) continue;
      seen.add(k);
      if (c !== 'E') q.push([nx, ny]);
    }
  }
  return seen;
}

const all = flood(true),
  noDig = flood(false);
rows.forEach((r, y) =>
  [...r].forEach((c, x) => {
    const k = x + ',' + y;
    if ((c === 'r' || c === 'E') && !all.has(k)) errors.push(`unreachable ${c} at ${k}`);
    if (c === 'r' && noDig.has(k)) errors.push(`red at ${k} reachable without digging`);
  }),
);

console.log(rows.map((r) => `            "${r}",`).join('\n'));
console.log(
  errors.length
    ? 'ERRORS:\n' + errors.join('\n')
    : `OK: ${W}x${H}, reds ${TOTAL} (required ${REQUIRED}), B=${count('B')} T=${count('T')} t=${count('t')} g=${count('g')} d=${count('d')}`,
);
