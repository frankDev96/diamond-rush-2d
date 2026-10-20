// Static server for ArtSource/Gameplay/preview.html (file:// blocks the asset loads).
// Usage (from the repo root, in the background): node .cursor/skills/diamond-rush-mobile-ui/scripts/serve_preview.mjs [port]
import http from 'http';
import fs from 'fs';
import path from 'path';

const port = +(process.argv[2] || 8765);
const root = process.cwd();
const types = { '.html': 'text/html', '.png': 'image/png', '.jpg': 'image/jpeg', '.js': 'text/javascript', '.mjs': 'text/javascript' };

http.createServer((req, res) => {
  const file = path.join(root, decodeURIComponent(req.url.split('?')[0]));
  if (!file.startsWith(root)) { res.writeHead(403); return res.end(); }
  fs.readFile(file, (err, data) => {
    if (err) { res.writeHead(404); return res.end(); }
    res.writeHead(200, { 'Content-Type': types[path.extname(file)] || 'application/octet-stream', 'Cache-Control': 'no-store' });
    res.end(data);
  });
}).listen(port, () => console.log(`serving ${root} on http://localhost:${port}`));
