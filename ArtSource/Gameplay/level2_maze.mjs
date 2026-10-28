// Angkor Level 2 — THE COURTYARD. Push Boulders! Multi-room topology from overview map.
// Usage: /opt/homebrew/opt/node@22/bin/node ArtSource/Gameplay/level2_maze.mjs
const W = 34, H = 18, REQUIRED = 2, TOTAL = 4;
const g = Array.from({ length: H }, () => Array(W).fill('#'));

const open = (x0, y0, x1, y1) => {
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    if (x > 0 && x < W - 1 && y >= 0 && y < H - 1) g[y][x] = '.';
  }
};
const fill = (x0, y0, x1, y1, c) => {
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    if (x > 0 && x < W - 1 && y > 0 && y < H - 1) g[y][x] = c;
  }
};
const set = (x, y, c) => { g[y][x] = c; };

// Border stays #. y=0 is top (door), y=H-1 bottom.

// SW spawn
open(1, 14, 7, 16);
set(2, 15, 'P');

// East corridor -> SE boulder courtyard
open(7, 14, 16, 15);

// SE boulder courtyard (mockup snake pit)
open(14, 11, 24, 16);
set(16, 13, 'B');
set(18, 14, 'B');
set(21, 12, 'B');
set(22, 15, 'B');

// North shaft from spawn -> Idol Hall
open(3, 7, 4, 14);

// Idol Hall (mockup Chamber of Skulls) — boulders + grass
open(1, 3, 10, 8);
set(4, 5, 'B');
set(6, 4, 'B');
set(7, 6, 'B');
fill(2, 6, 3, 7, 'd');

// Soft-clear choke on shaft (must dig to go north the short way)
fill(3, 10, 4, 11, 'd');

// Corridor Idol -> Dig Garden
open(10, 4, 13, 5);

// Dig Garden (mockup Botanical) — required reds in dirt pockets
open(12, 1, 22, 7);

// Required red 1: dirt chamber, only dig approaches
fill(14, 2, 16, 4, 'd');
set(15, 3, 'r');

// Required red 2: second dirt pocket
fill(18, 3, 20, 5, 'd');
set(19, 4, 'r');

// Door approach (floor ring so EE is dig-reachable via garden floors after digging into garden)
open(15, 1, 17, 1);
set(15, 0, 'E');
set(16, 0, 'E');

// Corridor Dig Garden -> Vault
open(22, 5, 24, 6);
open(23, 6, 24, 9);

// Treasure vault (mockup Crypt)
open(22, 8, 30, 12);
set(24, 10, 'T');
set(27, 9, 't');
set(28, 11, 'g');

// Lab dig pocket (mockup Laboratory) — extra red 1
open(27, 2, 32, 7);
open(30, 7, 31, 8);
fill(29, 3, 31, 5, 'd');
set(30, 4, 'r');

// Link courtyard up into vault
open(23, 12, 24, 13);

// Loop Dig Garden down into courtyard
open(17, 7, 18, 11);

// Extra red 2: dead-end west of Idol, dig-gated
open(1, 9, 2, 11);
fill(1, 9, 2, 10, 'd');
set(1, 11, 'r');
// Gate the red: surround so only path is through dirt above
// Path: Idol -> (1-2,8) already open in Idol? Idol is y=3..8. Connect:
open(1, 8, 2, 8);
fill(1, 9, 2, 10, 'd');
set(1, 11, 'r');
fill(1, 11, 2, 11, 'd'); // red must sit on floor but approach dig-only — put red inside dirt block
set(1, 11, 'r');

const rows = g.map(r => r.join(''));

const errors = [];
if (rows.length !== H || rows.some(r => r.length !== W)) errors.push('size');
const count = c => rows.join('').split(c).length - 1;
if (count('P') !== 1) errors.push('need one P');
if (!rows[0].includes('EE') || count('E') !== 2) errors.push('need EE in top row');
if (count('r') !== TOTAL) errors.push(`need ${TOTAL} reds, have ${count('r')}`);
for (let x = 0; x < W; x++) if (rows[H - 1][x] !== '#') errors.push('bottom border open at ' + x);
for (let y = 0; y < H; y++) if (rows[y][0] !== '#' || rows[y][W - 1] !== '#') errors.push('side border at y=' + y);

function flood(passDirt) {
  const seen = new Set();
  let start;
  rows.forEach((r, y) => { const x = r.indexOf('P'); if (x >= 0) start = [x, y]; });
  if (!start) return seen;
  const q = [start];
  seen.add(start.join(','));
  while (q.length) {
    const [x, y] = q.shift();
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = x + dx, ny = y + dy;
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

const all = flood(true), noDig = flood(false);
rows.forEach((r, y) => [...r].forEach((c, x) => {
  const k = x + ',' + y;
  if ((c === 'r' || c === 'E') && !all.has(k)) errors.push(`unreachable ${c} at ${k}`);
  if (c === 'r' && noDig.has(k)) errors.push(`red at ${k} reachable without digging`);
}));

console.log(rows.map(r => `            "${r}",`).join('\n'));
console.log(errors.length ? 'ERRORS:\n' + errors.join('\n') : `OK: ${W}x${H}, reds ${TOTAL} (required ${REQUIRED}), B=${count('B')} T=${count('T')} t=${count('t')} g=${count('g')}`);
