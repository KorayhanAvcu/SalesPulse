# Kritik sorguların süresini, okuma sayısını ve yürütme planını ölçer.
#
#   .\measure.ps1 -Label before     (02_indexes.sql çalıştırılmadan önce)
#   .\measure.ps1 -Label after      (02_indexes.sql çalıştırıldıktan sonra)
#
# Çıktı: docs/performance/<label>.json ve her sorgu için <label>-<kod>.sqlplan
# (.sqlplan dosyaları SSMS'te grafik plan olarak açılır)
param(
    [Parameter(Mandatory)][string]$Label,
    [string]$Server = "$env:COMPUTERNAME\SQLEXPRESS",
    [int]$Runs = 5
)

$outDir = Join-Path $PSScriptRoot '..\..\docs\performance'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$year  = "@From='2025-11-01', @To='2026-11-01'"
$month = "@From='2026-10-01', @To='2026-11-01'"

# Order Explorer'ın gerçekten ürettiği sorgular (OrderRepository ile aynı biçim)
$explorerPage = @"
;WITH PageIds AS (
    SELECT o.OrderId, o.OrderDate FROM dbo.Orders o
    WHERE o.OrderDate >= '2026-10-01' AND o.OrderDate < '2026-11-01'
    ORDER BY o.OrderDate DESC, o.OrderId DESC
    OFFSET 2000 ROWS FETCH NEXT 25 ROWS ONLY
)
SELECT o.OrderId, c.FirstName + N' ' + c.LastName AS CustomerName, ci.Name, o.OrderDate, o.TotalAmount, pm.Name, s.Name
FROM PageIds p
JOIN dbo.Orders o ON o.OrderId = p.OrderId
JOIN dbo.Customers c ON c.CustomerId = o.CustomerId
JOIN dbo.Cities ci ON ci.CityId = o.ShippingCityId
JOIN dbo.OrderStatuses s ON s.StatusId = o.StatusId
JOIN dbo.Payments pay ON pay.OrderId = o.OrderId
JOIN dbo.PaymentMethods pm ON pm.PaymentMethodId = pay.PaymentMethodId
ORDER BY p.OrderDate DESC, p.OrderId DESC;
"@

$customerSearch = @"
SELECT COUNT(*) FROM dbo.Orders o
WHERE o.CustomerId IN (SELECT CustomerId FROM dbo.Customers WHERE FirstName LIKE N'Ayşe%' AND LastName LIKE N'Yıl%');
"@

$queries = [ordered]@{
    Q1_KpiMonth       = @{ Title = 'Dashboard KPI (1 ay)';                  Sql = "EXEC dbo.usp_Dashboard_GetKpis $month" }
    Q2_TrendYear      = @{ Title = 'Aylık satış eğilimi (12 ay)';            Sql = "EXEC dbo.usp_Dashboard_GetMonthlyTrend $year" }
    Q3_CategoryMonth  = @{ Title = 'İlk 10 kategori (1 ay)';                 Sql = "EXEC dbo.usp_Dashboard_GetTopCategories $month" }
    Q4_CategoryYear   = @{ Title = 'İlk 10 kategori (12 ay)';                Sql = "EXEC dbo.usp_Dashboard_GetTopCategories $year" }
    Q5_OrderDetail    = @{ Title = 'Sipariş detayı (tek sipariş)';           Sql = "EXEC dbo.usp_Orders_GetDetail @OrderId=1500000" }
    Q6_ExplorerPage   = @{ Title = 'Order Explorer sayfası (1 ay, sayfa 81)'; Sql = $explorerPage }
    Q7_CustomerSearch = @{ Title = 'Müşteri adı araması';                    Sql = $customerSearch }
    Q8_TopCustomers   = @{ Title = 'En iyi 20 müşteri (12 ay)';              Sql = "EXEC dbo.usp_Report_GetTopCustomers $year" }
}

$connection = New-Object System.Data.SqlClient.SqlConnection "Server=$Server;Database=SalesPulseDb;Integrated Security=True;TrustServerCertificate=True"
$script:reads = 0
$handler = [System.Data.SqlClient.SqlInfoMessageEventHandler] {
    param($s, $e)
    foreach ($m in [regex]::Matches($e.Message, 'logical reads (\d+)')) { $script:reads += [long]$m.Groups[1].Value }
}
$connection.add_InfoMessage($handler)
$connection.FireInfoMessageEventOnUserErrors = $true
$connection.Open()

function Invoke-Sql([string]$sql) {
    $cmd = $connection.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 0
    $plans = New-Object System.Collections.ArrayList
    $reader = $cmd.ExecuteReader()
    do {
        while ($reader.Read()) {
            if ($reader.FieldCount -eq 1 -and $reader.GetName(0) -like 'Microsoft SQL Server*XML*') { [void]$plans.Add($reader.GetString(0)) }
        }
    } while ($reader.NextResult())
    $reader.Close()
    return , $plans
}

$results = New-Object System.Collections.ArrayList

foreach ($code in $queries.Keys) {
    $query = $queries[$code]

    # 1) Süre: plan önbelleği ısındıktan sonra medyan. İlk çalıştırma ısındırma, sayılmaz.
    [void](Invoke-Sql $query.Sql)
    $times = for ($i = 0; $i -lt $Runs; $i++) {
        $watch = [Diagnostics.Stopwatch]::StartNew()
        [void](Invoke-Sql $query.Sql)
        $watch.Stop()
        $watch.Elapsed.TotalMilliseconds
    }
    $sorted = @($times | Sort-Object)
    $median = $sorted[[int][math]::Floor($sorted.Count / 2)]

    # 2) Okuma sayısı ve plan
    $script:reads = 0
    [void](Invoke-Sql 'SET STATISTICS IO ON; SET STATISTICS XML ON;')
    $plans = Invoke-Sql $query.Sql
    [void](Invoke-Sql 'SET STATISTICS XML OFF; SET STATISTICS IO OFF;')

    # Plandaki erişim işlemleri: hangi tablo, hangi indeks, Scan mı Seek mi?
    $access = New-Object System.Collections.ArrayList
    $planIndex = 0
    foreach ($plan in $plans) {
        $planIndex++
        $suffix = if ($plans.Count -gt 1) { "-$planIndex" } else { '' }
        [IO.File]::WriteAllText((Join-Path $outDir "$Label-$code$suffix.sqlplan"), $plan, [Text.Encoding]::Unicode)

        $xml = [xml]$plan
        $ns = New-Object System.Xml.XmlNamespaceManager $xml.NameTable
        $ns.AddNamespace('p', 'http://schemas.microsoft.com/sqlserver/2004/07/showplan')
        foreach ($op in $xml.SelectNodes('//p:RelOp[p:IndexScan]', $ns)) {
            $obj = $op.SelectSingleNode('p:IndexScan/p:Object', $ns)
            $table = $obj.GetAttribute('Table').Trim('[', ']')
            if ($table -in 'Orders', 'OrderDetails', 'Payments', 'Customers') {
                $index = $obj.GetAttribute('Index').Trim('[', ']')
                [void]$access.Add("$table/$index ($($op.GetAttribute('PhysicalOp')))")
            }
        }
    }

    $row = [ordered]@{
        Code         = $code
        Title        = $query.Title
        MedianMs     = [math]::Round($median, 1)
        LogicalReads = $script:reads
        Access       = @($access | Select-Object -Unique)
    }
    [void]$results.Add($row)
    Write-Host ("{0,-18} {1,10:N1} ms {2,12:N0} okuma  {3}" -f $code, $median, $script:reads, (@($access | Select-Object -Unique) -join ', '))
}

$connection.Close()
$json = ConvertTo-Json -InputObject $results -Depth 4
[IO.File]::WriteAllText((Join-Path $outDir "$Label.json"), $json, (New-Object Text.UTF8Encoding $false))
