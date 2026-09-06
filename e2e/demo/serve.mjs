import { createServer } from "node:http";
import { createReadStream } from "node:fs";
import { stat } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

const output = fileURLToPath(
  new URL("../../.local/live-demo/", import.meta.url),
);
const files = new Map([
  ["/", ["index.html", "text/html; charset=utf-8"]],
  [
    "/stewardship-live-demo-5min.mp4",
    ["stewardship-live-demo-5min.mp4", "video/mp4"],
  ],
  ["/evidence.json", ["evidence.json", "application/json"]],
]);
const server = createServer(async (request, response) => {
  const file = files.get(request.url?.split("?")[0]);
  if (!file || !["GET", "HEAD"].includes(request.method)) {
    response.writeHead(404).end();
    return;
  }
  try {
    const path = join(output, file[0]);
    const { size } = await stat(path);
    const range = request.headers.range;
    let start = 0,
      end = size - 1;
    if (range) {
      const match = /^bytes=(\d+)-(\d*)$/.exec(range);
      if (match) {
        start = Number(match[1]);
        end = match[2] ? Math.min(Number(match[2]), size - 1) : size - 1;
      }
      if (!match || start >= size || start > end) {
        response.writeHead(416, { "Content-Range": `bytes */${size}` }).end();
        return;
      }
    }
    response.writeHead(range ? 206 : 200, {
      "Content-Type": file[1],
      "Content-Length": end - start + 1,
      "Accept-Ranges": "bytes",
      "Cache-Control": "no-store",
      ...(range ? { "Content-Range": `bytes ${start}-${end}/${size}` } : {}),
    });
    if (request.method === "HEAD") response.end();
    else {
      const stream = createReadStream(path, { start, end });
      stream.on("error", () => response.destroy());
      response.on("close", () => stream.destroy());
      stream.pipe(response);
    }
  } catch {
    response.writeHead(404).end();
  }
});
server.listen(4321, "127.0.0.1", () =>
  console.log("Video player: http://127.0.0.1:4321"),
);
