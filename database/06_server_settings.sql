-- =============================================================
-- 06) SQL Server sunucu ayarları (bir kere çalıştırılır, kalıcıdır)
-- =============================================================
-- NEDEN?
-- Geliştirme bilgisayarında SQL Server; Visual Studio, tarayıcı ve
-- diğer programlarla aynı RAM'i paylaşıyor. Varsayılan ayarda
-- "min server memory" sadece 16 MB. Bilgisayarın belleği azalınca
-- Windows, SQL Server'ın belleğini geri alıyor:
--   - SalesPulseDb'nin bellekteki payı 0 MB'a düştü (ölçüldü),
--   - her sorgu veriyi diskten okumak zorunda kaldı,
--   - hazırlanan sorgu planları bellekten atıldı,
--   - Dashboard / Satış Analizi 120 sn'de zaman aşımına uğradı.
-- Veritabanının tamamı ~500 MB. SQL Server'a en az 1 GB ayırınca
-- veri bellekte kalıyor ve raporlar 1-2 sn'de dönüyor.
--
-- Not: SQL Server Express zaten en fazla ~1,4 GB tampon bellek
-- kullanabiliyor; 1024 MB bu sınırın altında, güvenli bir değer.
-- =============================================================

EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

-- 1) SQL Server'a en az 1 GB bellek garanti et
EXEC sp_configure 'min server memory (MB)', 1024;

-- 2) Paralel çalıştırma eşiği: varsayılan 5 çok düşük; küçük sorgular da
--    birden fazla çekirdeğe bölünüp birbirini bekliyordu. 50 yaygın kabul gören değer.
EXEC sp_configure 'cost threshold for parallelism', 50;
RECONFIGURE;
GO

-- Kontrol
SELECT name, value_in_use
FROM sys.configurations
WHERE name IN ('min server memory (MB)', 'cost threshold for parallelism');
GO
