// Turn a saved CDP Page.captureScreenshot response (JSON log file) into a PNG.
// Usage: node .cursor/skills/diamond-rush-mobile-ui/scripts/decode_cdp_screenshot.mjs <cdp-response.json> <out.png>
import fs from 'fs';

const [src, out] = process.argv.slice(2);
if (!src || !out) {
  console.error('usage: decode_cdp_screenshot.mjs <cdp-response.json> <out.png>');
  process.exit(2);
}
const json = JSON.parse(fs.readFileSync(src, 'utf8'));
const data = (json.result || json).data || json.data;
if (!data) {
  console.error('no base64 "data" field in ' + src);
  process.exit(1);
}
fs.writeFileSync(out, Buffer.from(data, 'base64'));
console.log('wrote ' + out);
