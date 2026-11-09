export type Role = 'KASIR' | 'ANGGOTA' | 'ADMIN';
export interface User { id: number; role: Role; nama: string; nik: string | null; default_nik: string; ganti_password: boolean }
export interface Product { id: number; nama: string; kode: string; barcode: string; satuan: string; harga: number; stok: number | null; kategori_id: string; kategori: string; foto: number | null }
export interface Line { product: Product; qty: number }
export interface Draft { key: string; userId: number; role: Role; nomor?: string; nik: string; tenor: number; uang_bayar?: number; baris: { product_id: number; qty: number }[] }
export type Data = Record<string, any>;
