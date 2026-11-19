// Pemetaan nama ikon semantik (nav.ts) -> komponen Lucide. Diimpor satu per satu agar tree-shaking menjaga bundel.
import type { Component } from 'svelte';
import LayoutDashboard from '@lucide/svelte/icons/layout-dashboard';
import Users from '@lucide/svelte/icons/users';
import Package from '@lucide/svelte/icons/package';
import PackageX from '@lucide/svelte/icons/package-x';
import ClipboardCheck from '@lucide/svelte/icons/clipboard-check';
import TrendingDown from '@lucide/svelte/icons/trending-down';
import Tags from '@lucide/svelte/icons/tags';
import ArrowUpDown from '@lucide/svelte/icons/arrow-up-down';
import ChartColumn from '@lucide/svelte/icons/chart-column';
import Scale from '@lucide/svelte/icons/scale';
import UserRound from '@lucide/svelte/icons/user-round';
import ScanBarcode from '@lucide/svelte/icons/scan-barcode';
import ShoppingBasket from '@lucide/svelte/icons/shopping-basket';
import ReceiptText from '@lucide/svelte/icons/receipt-text';
import IdCard from '@lucide/svelte/icons/id-card';
import PackagePlus from '@lucide/svelte/icons/package-plus';
import type { NamaIkon } from './nav';

export const IKON: Record<NamaIkon, Component<any>> = {
  beranda: LayoutDashboard, pengguna: Users, produk: Package, stokHabis: PackageX, opname: ClipboardCheck,
  hargaRendah: TrendingDown, kategori: Tags, urutan: ArrowUpDown, rekap: ChartColumn, tagihan: Scale,
  profil: UserRound, kasir: ScanBarcode, belanja: ShoppingBasket, riwayat: ReceiptText, kartu: IdCard, pembelian: PackagePlus
};
