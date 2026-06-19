# Inventory Service

Project ini dibuat menggunakan ASP.NET Core .NET 8 untuk memenuhi kebutuhan 
technical test inventory dan order management.

Fokus utama pada project ini adalah memastikan data tetap konsisten ketika terjadi 
request yang berjalan bersamaan (concurrent request), 
terutama pada proses pengurangan stock dan update status order.

## Tech Stack

* ASP.NET Core .NET 8
* Entity Framework Core
* SQL Server
* xUnit

## Struktur Project

* InventoryAPI : Endpoint API
* InventoryCore : Entity dan Enum
* InventoryData : DbContext dan konfigurasi database
* InventoryService : Business Logic
* InventoryShared : DTO
* InventoryTest : Unit Test

## Strategi Concurrency

## Order Status

Untuk update status order menggunakan RowVersion (optimistic concurrency).

Dengan cara ini sistem dapat mendeteksi jika data order sudah diubah oleh user lain sebelum proses update dilakukan.

## Product Stock

Untuk pengurangan stock menggunakan atomic SQL update.

Contoh:

sql
UPDATE Products
SET StockQty = StockQty - @qty
WHERE Id = @id
AND StockQty >= @qty


Cara ini dipilih agar stock tidak menjadi minus ketika ada beberapa order yang diproses 
pada waktu yang hampir bersamaan.

## Idempotency

Create Order menggunakan Idempotency-Key yang disimpan ke tabel khusus dan dilindungi dengan unique index.

Tujuannya untuk mencegah order ganda apabila client melakukan retry request karena timeout atau gangguan jaringan.

## Fitur

* Create Order
* Get Order Detail
* Get Orders
* Update Status Order
* Cancel Order
* Idempotency Key
* Global Exception Handling
* Correlation Id

## Additional Race Conditions

1. Double Cancel Order

Kemungkinan:
Dua admin melakukan cancel order yang sama secara bersamaan sehingga stock bisa dikembalikan dua kali.

Pencegahan:
Update status dilakukan secara atomic dan hanya order dengan status Pending atau Confirmed yang dapat di-cancel.

2. Duplicate Idempotency Request

Kemungkinan:
Dua request dengan Idempotency-Key yang sama masuk pada waktu yang hampir bersamaan.

Pencegahan:
Menggunakan unique index pada tabel IdempotencyRequests sehingga hanya satu request yang dapat tersimpan.

## Database

Project ini menggunakan SQL Server.

Alasan memilih SQL Server karena sudah familiar digunakan pada project sehari-hari,
mendukung transaction dengan baik, serta memiliki fitur RowVersion yang dapat digunakan untuk optimistic concurrency.

## Testing

Project test menggunakan xUnit.

Test yang dibuat:

* Test_Stock()

Skenario:

* Stock awal 15
* Dua request melakukan pembelian masing-masing qty 10
* Hanya satu request yang berhasil
* Stock akhir tetap valid dan tidak menjadi negatif

