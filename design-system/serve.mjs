import { createServer } from "node:http";
import { readFile } from "node:fs/promises";
import { resolve, extname, sep } from "node:path";
const root = resolve("dist");
const mime = {
  ".html": "text/html",
  ".css": "text/css",
  ".woff2": "font/woff2",
};
createServer(async (request, response) => {
  const path = resolve(
    root,
    "." +
      new URL(request.url, "http://localhost").pathname.replace(
        /\/$/,
        "/index.html",
      ),
  );
  if (!path.startsWith(root + sep)) {
    response.writeHead(403).end();
    return;
  }
  try {
    response.setHeader(
      "Content-Type",
      mime[extname(path)] || "application/octet-stream",
    );
    response.end(await readFile(path));
  } catch {
    response.writeHead(404).end("Not found");
  }
}).listen(4318, "localhost");
