export async function onRequestPost(context) {
  if (context.env.ORDERING_ENABLED !== "true") {
    return Response.json(
      { ok: false, message: "QR sipariş özelliği işletme tarafından henüz etkinleştirilmedi." },
      { status: 403 }
    );
  }
  return Response.json(
    { ok: false, message: "Sipariş bağlantısı KaldiPOS ile eşleştirme aşamasında." },
    { status: 503 }
  );
}
