export function onRequestGet() {
  return Response.json({ ok: true, service: "KaldiQR", time: new Date().toISOString() });
}
