# KaldiQR

Kaldi Cafe için KaldiPOS ile bütünleşecek mobil QR menü.

## İlk sürüm
- 21 kategori / 145 ürün mevcut Kaldi menüsünden gelir.
- Gerçek Kaldi ürün görselleri kullanılır.
- Mobil öncelikli tasarım.
- `/masa/12` biçimindeki adreslerden masa numarası algılanır.
- QR sipariş kodu hazırdır fakat `orderingEnabled` varsayılan olarak kapalıdır.
- Cloudflare Pages Functions için `/api/health` ve `/api/orders` başlangıç uçları vardır.

## Cloudflare Pages
Git bağlantısında proje kökü: `KaldiQR`
Derleme komutu: boş bırakılabilir
Çıktı dizini: `public`

Özel domain zorunlu değildir. İlk yayın `*.pages.dev` adresinde çalışabilir.
