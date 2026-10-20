// Carves the Level 1 tunnel maze, validates it with the same rules as LevelLayoutValidator.cs,
// and prints the rows to paste into Assets/Scripts/Gameplay/Level1Layout.cs.
// Usage: node ArtSource/Gameplay/level1_maze.mjs
const W = 48, H = 20, REQUIRED = 3, TOTAL = 5;
const g = Array.from({ length: H }, () => Array(W).fill('#'));

const open = (x0, y0, x1, y1) => { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) g[y][x] = '.'; };
const dirt = (x0, y0, x1, y1) => { for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) g[y][x] = 'd'; };
const set = (x, y, c) => { g[y][x] = c; };

// Zone A: spawn chamber, up-shaft, top run, down-shaft with the first chokepoint.
open(1, 16, 5, 18);
open(3, 5, 4, 15);
dirt(3, 14, 4, 15);
open(1, 8, 2, 9);
open(3, 4, 9, 5);
open(8, 6, 9, 13);
dirt(8, 9, 9, 10);
dirt(5, 10, 5, 10);
set(6, 10, 'r');
open(8, 13, 13, 14);

// Zone B: switchbacks, red 2 behind dirt, extra red in a dead-end pocket.
open(12, 7, 13, 14);
open(12, 7, 21, 7);
dirt(15, 7, 16, 7);
set(17, 7, 'r');
open(20, 7, 21, 16);
open(15, 11, 19, 12);
dirt(18, 11, 19, 12);
set(15, 11, 'r');
open(20, 16, 26, 17);

// Zone C: loop around a temple block, red 3 in a dirt chamber, extra red up top.
open(25, 3, 26, 17);
open(25, 3, 34, 4);
open(33, 3, 34, 17);
open(25, 16, 34, 17);
dirt(27, 9, 31, 11);
set(30, 10, 'r');
dirt(29, 1, 30, 2);
set(29, 1, 'r');
dirt(29, 16, 30, 17);

// Zone D: winding corridor up to the door in the top wall.
open(35, 12, 38, 13);
open(37, 6, 38, 13);
open(37, 6, 42, 6);
open(39, 2, 40, 5);
open(42, 6, 43, 16);
dirt(42, 11, 43, 12);
open(42, 16, 46, 17);
open(45, 1, 46, 17);
open(44, 1, 46, 2);
dirt(45, 8, 46, 9);
set(44, 0, 'E');
set(45, 0, 'E');
set(2, 17, 'P');

const rows = g.map(r => r.join(''));

// ---- validation (mirrors LevelLayoutValidator.cs) ----
const errors = [];
if (rows.length !== H || rows.some(r => r.length !== W)) errors.push('size');
const count = c => rows.join('').split(c).length - 1;
if (count('P') !== 1) errors.push('need one P');
if (!rows[0].includes('EE') || count('E') !== 2) errors.push('need EE in top row');
if (count('r') !== TOTAL) errors.push(`need ${TOTAL} reds, have ${count('r')}`);
for (let x = 0; x < W; x++) if (rows[H - 1][x] !== '#') errors.push('bottom border open');
for (let y = 0; y < H; y++) if (rows[y][0] !== '#' || rows[y][W - 1] !== '#') errors.push('side border open at ' + y);

function flood(passDirt) {
  const seen = new Set();
  let start;
  rows.forEach((r, y) => { const x = r.indexOf('P'); if (x >= 0) start = [x, y]; });
  const q = [start];
  seen.add(start.join());
  while (q.length) {
    const [x, y] = q.shift();
    for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
      const nx = x + dx, ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
      const c = rows[ny][nx];
      if (c === '#' || (c === 'd' && !passDirt)) continue;
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
  if ((c === '.' || c === 'd') && !all.has(k)) errors.push(`isolated cell ${k}`);
}));

console.log(rows.map(r => `            "${r}",`).join('\n'));
console.log(errors.length ? 'ERRORS:\n' + errors.join('\n') : `OK: ${W}x${H}, reds ${TOTAL} (required ${REQUIRED})`);
