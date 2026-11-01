using System.Data;
using Microsoft.Data.SqlClient;

namespace SalesPulse.DataGenerator;

// Bellekteki DataTable'ı SqlBulkCopy ile tek seferde SQL Server'a gönderir.
// --dry-run modunda veritabanına dokunmaz, sadece satır sayısını yazar.
public class BulkWriter
{
    private readonly string _connectionString;
    private readonly bool _dryRun;

    public BulkWriter(string connectionString, bool dryRun)
    {
        _connectionString = connectionString;
        _dryRun = dryRun;
    }

    public void Write(DataTable table)
    {
        if (_dryRun)
        {
            Console.WriteLine($"   [dry-run] {table.TableName,-13}: {table.Rows.Count,9:N0} satır (yazılmadı)");
            return;
        }

        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        // KeepIdentity     : Id'leri biz verdik, SQL Server yenisini üretmesin
        // TableLock        : tabloyu kilitle → çok daha hızlı toplu yazma
        // CheckConstraints : FK/CHECK kontrol edilsin. Edilmezse FK'ler "güvenilmez"
        //                    (not trusted) olur ve sorgu optimizasyonu kötüleşir!
        var options = SqlBulkCopyOptions.KeepIdentity
                    | SqlBulkCopyOptions.TableLock
                    | SqlBulkCopyOptions.CheckConstraints;

        using var bulk = new SqlBulkCopy(connection, options, null)
        {
            DestinationTableName = "dbo." + table.TableName,
            BatchSize = Settings.BatchSize,
            BulkCopyTimeout = 0 // süre sınırı yok
        };

        // Kolonları isimle eşle (sıraya güvenme)
        foreach (DataColumn column in table.Columns)
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        bulk.WriteToServer(table);
        Console.WriteLine($"   {table.TableName,-13}: {table.Rows.Count,9:N0} satır yazıldı");
    }

    // Tablo boş mu? (veri iki kez basılmasın diye)
    public bool IsEmpty(string tableName)
    {
        if (_dryRun) return true;

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand($"SELECT COUNT(*) FROM dbo.{tableName}", connection);
        return (int)command.ExecuteScalar() == 0;
    }
}
