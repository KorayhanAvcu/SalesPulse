-- =============================================
-- SalesPulse - Veritabanı oluşturma
-- Veritabanı yoksa oluşturur; tekrar çalıştırılırsa hata vermez.
-- =============================================
IF DB_ID('SalesPulseDb') IS NULL
    CREATE DATABASE SalesPulseDb;
GO
