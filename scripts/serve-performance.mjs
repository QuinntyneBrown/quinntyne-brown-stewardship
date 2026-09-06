import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { resolve, extname, sep } from "node:path";
import { fileURLToPath } from "node:url";
import { brotliCompressSync, constants } from "node:zlib";

const root = fileURLToPath(
  new URL("../frontend/dist/performance/browser/", import.meta.url),
);
const mime = {
  ".html": "text/html",
  ".js": "text/javascript",
  ".css": "text/css",
  ".woff2": "font/woff2",
  ".svg": "image/svg+xml",
};
const assets = new Map();
createServer(async (request, response) => {
  try {
    const pathname = decodeURIComponent(
      new URL(request.url, "http://localhost").pathname,
    );
    const path = resolve(
      root,
      "." + (extname(pathname) ? pathname : "/index.html"),
    );
    if (!path.startsWith(resolve(root) + sep)) {
      response.writeHead(403).end();
      return;
    }
    if (!assets.has(path)) {
      const body = await readFile(path);
      // .NET's CompressionLevel.Optimal maps to Brotli quality 4.
      assets.set(path, { body, compressed: brotliCompressSync(body, { params: { [constants.BROTLI_PARAM_QUALITY]: 4 } }) });
    }
    const asset = assets.get(path);
    const compressed = /\bbr\b/.test(request.headers["accept-encoding"] ?? "");
    response.writeHead(200, {
      "Content-Type": mime[extname(path)] ?? "application/octet-stream",
      "Cache-Control": "no-store",
      Vary: "Accept-Encoding",
      ...(compressed ? { "Content-Encoding": "br" } : {}),
    });
    response.end(compressed ? asset.compressed : asset.body);
  } catch {
    response.writeHead(404).end("Not found");
  }
}).listen(4319, "localhost");
