// Mini-DSL untuk membuat skrip FigCraft (figcraft-cli run). ID layer berurutan mulai dari 2.
export const cmds = [];
let nextId = 2;
/** Mulai dokumen baru (satu file desain per role). */
export function reset() {
  cmds.length = 0;
  nextId = 2;
  awalan = '';
}
let awalan = '';
/** Awalan nomor ("01") untuk nama frame teratas berikutnya, agar urutan layer dan PNG mengikuti alur. */
export function setAwalan(nomor) {
  awalan = nomor;
}
const PATHS = {
  back: 'M15 5l-7 7 7 7',
  chevR: 'M9 5l7 7-7 7',
  chevD: 'M5 9l7 7 7-7',
  plus: 'M12 5v14M5 12h14',
  minus: 'M5 12h14',
  x: 'M6 6l12 12M18 6L6 18',
  check: 'M5 12l5 5 9-10',
  scan: 'M4 8V5a1 1 0 011-1h3M16 4h3a1 1 0 011 1v3M20 16v3a1 1 0 01-1 1h-3M8 20H5a1 1 0 01-1-1v-3M7 12h10',
  logout: 'M10 5H5v14h5M14 8l4 4-4 4M18 12H9',
  user: 'M12 11a4 4 0 100-8 4 4 0 000 8zM4 21c0-4 4-6 8-6s8 2 8 6',
  print: 'M7 9V4h10v5M7 17H5v-6h14v6h-2M7 14h10v6H7z',
  cart: 'M3 4h2.5l2 11h10.5l2-8H6M9 20h.01M17 20h.01',
  search: 'M10.5 4a6.5 6.5 0 100 13 6.5 6.5 0 000-13zM15.5 15.5L20 20',
  wifi: 'M2 9a15 15 0 0120 0M5 12.5a10 10 0 0114 0M8.5 16a5 5 0 017 0M12 19.5h.01',
  alert: 'M12 4l9 16H3zM12 10v4M12 17h.01',
  lock: 'M6 11h12v9H6zM8 11V8a4 4 0 018 0v3',
  more: 'M11.5 5.5h1v1h-1zM11.5 11.5h1v1h-1zM11.5 17.5h1v1h-1z',
  eye: 'M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12zM12 9a3 3 0 100 6 3 3 0 000-6z',
  tag: 'M3 12V4h8l10 10-8 8z',
  users: 'M9 11a3.500 3.500 0 100-7 3.500 3.500 0 000 7zM2.500 20c0-3.500 3-6 6.500-6s6.500 2.500 6.500 6M16.500 4.500a3.500 3.500 0 010 6.500M18 14.500c2 .8 3.500 2.700 3.500 5.500',
  eyeoff: 'M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12zM12 9a3 3 0 100 6 3 3 0 000-6zM4 4l16 16',
  login: 'M14 4h4a2 2 0 012 2v12a2 2 0 01-2 2h-4M10 8l4 4-4 4M14 12H3',
  qr: 'M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h2v2h-2zM18 14h2v2h-2zM14 18h2v2h-2zM18 18h2v2h-2z',
  flash: 'M13 2L5 14h6l-1 8 8-12h-6z',
  shield: 'M12 3l8 3v6c0 5-3.5 8-8 9-4.500-1-8-4-8-9V6z',
  home: 'M3 10.5L12 3l9 7.5M5.5 9.5V20h13V9.5M9 20v-6h6v6',
  receipt: 'M6 3h12v18l-3-2-3 2-3-2-3 2zM9 8h6M9 12h6M9 16h3',
  grid: 'M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z',
  download: 'M12 3v12m0 0l-5-5m5 5l5-5M4 17v4h16v-4',
};

// Ekstensi jalur sebenarnya (dalam kotak 24) agar skala ikon proporsional.
const EXT = { back: [7, 14], chevR: [7, 14], chevD: [14, 7], plus: [14, 14], minus: [14, 0], x: [12, 12], check: [14, 10], scan: [16, 16], logout: [14, 14], user: [16, 18], print: [14, 16], cart: [18, 16], search: [16, 16], wifi: [20, 11], alert: [18, 16], lock: [12, 16], more: [1, 13], eye: [20, 14], tag: [18, 18], users: [19, 16], eyeoff: [20, 16], login: [17, 16], qr: [16, 16], flash: [13, 20], shield: [16, 18], home: [18, 17], receipt: [12, 18], grid: [16, 16], download: [16, 18] };

/** Bentuk vektor terisi (tanpa garis), untuk ilustrasi produk. */
export function solid(parent, pathData, w, h, fill, opacity = 1, name = 'bentuk') {
  const params = { path: pathData, width: w, height: h, name, parent, select: false, props: { fill, stroke: 'none', opacity } };
  return emit('vector.create', params);
}

/** Bentuk vektor dari path berkoordinat absolut di frame induk (tanpa skala), untuk ilustrasi lanskap. fill boleh gradien. */
export function vec(parent, pathData, fill, opacity = 1, name = 'ilustrasi') {
  const props = { stroke: 'none', opacity, absolute: true };
  if (typeof fill === 'object') props.fills = [fill];
  else props.fill = fill;
  return emit('vector.create', { path: pathData, x: 0, y: 0, name, parent, select: false, props });
}

/** Tempatkan foto dari berkas (image.place), mode fill = menutupi area dengan crop. */
export function gambar(parent, berkas, x, y, w, h, mode = 'fill', name = 'foto') {
  return emit('image.place', { path: berkas, x, y, width: w, height: h, parent, name, mode });
}

/** Garis jalur bebas (siku bidik kamera dsb.), diabaikan auto layout. */
export function strokePath(parent, pathData, x, y, w, h, color, sw = 6, name = 'garis') {
  return emit('vector.create', { path: pathData, x, y, width: w, height: h, name, parent, select: false, props: { fill: 'none', stroke: color, strokeWeight: sw, strokeCap: 'round', strokeJoin: 'round', absolute: true } });
}

/** Persegi panjang yang diabaikan auto layout (garis pindai dsb.). */
export function absRect(parent, x, y, w, h, fill, r = 0, opacity = 1, name = 'bar') {
  return emit('shape.rectangle', { x, y, width: w, height: h, parent, name, props: { fill, cornerRadius: r, opacity, absolute: true }, select: false });
}

/** Elips dekoratif yang diabaikan auto layout (posisi bebas di dalam frame induk). */
export function deco(parent, x, y, d, fill, opacity = 0.15) {
  return emit('shape.ellipse', { x, y, width: d, height: d, parent, name: 'dekorasi', props: { fill, opacity, absolute: true }, select: false });
}

function emit(command, params) {
  cmds.push({ command, params });
  return nextId++;
}

export const cl = (c) => c; // pembantu keterbacaan

/** Frame (auto layout opsional). o: {name,x,y,w,h,fill,stroke,sw,r,layout,pad,gap,main,cross,sh,sv,clip,shadow,opacity} */
export function box(parent, o = {}) {
  const props = {};
  if (o.fill && typeof o.fill === 'object') props.fills = [o.fill]; // gradien
  else props.fill = o.fill ?? 'none';
  if (o.stroke) {
    props.stroke = o.stroke;
    props.strokeWeight = o.sw ?? 1;
    props.strokeAlign = 'inside';
  }
  if (o.r !== undefined) props.cornerRadius = o.r;
  if (o.layout) props.layout = o.layout;
  if (o.pad !== undefined) props.padding = o.pad;
  if (o.gap !== undefined) props.gap = o.gap;
  if (o.main) props.mainAlign = o.main;
  if (o.cross) props.crossAlign = o.cross;
  props.sizingH = o.sh ?? (o.w !== undefined ? 'fixed' : 'hug');
  props.sizingV = o.sv ?? (o.h !== undefined ? 'fixed' : 'hug');
  if (o.clip !== undefined) props.clip = o.clip;
  if (o.opacity !== undefined) props.opacity = o.opacity;
  if (o.wrap) props.wrap = true;
  if (o.abs) props.absolute = true; // diabaikan auto layout induk
  if (o.shadow) props.effects = [{ type: 'dropShadow', offset: [0, o.shadow.y ?? 2], radius: o.shadow.blur ?? 8, color: o.shadow.color ?? '#00000026' }];
  const params = { name: o.name ?? 'Frame', props, select: false };
  if (o.x !== undefined) params.x = o.x;
  if (o.y !== undefined) params.y = o.y;
  if (o.w !== undefined) params.width = o.w;
  if (o.h !== undefined) params.height = o.h;
  if (parent !== null) params.parent = parent;
  else if (awalan) params.name = `${awalan} ${params.name}`;
  return emit('frame.create', params);
}

/** Teks. o: {size,weight,color,align,w,sh,name,ls,lh} weight: Regular|Medium|SemiBold|Bold */
export function txt(parent, text, o = {}) {
  const props = {
    fontFamily: o.font ?? 'Poppins', // v2: satu keluarga huruf; `serif` (judul) jadi Poppins SemiBold
    fontStyle: o.weight ?? (o.serif ? 'SemiBold' : 'Regular'),
    fontSize: o.size ?? 14,
    fill: o.color ?? '#000000',
  };
  if (o.align) props.textAlign = o.align;
  props.textAutoResize = o.sh === 'fill' || o.w !== undefined ? 'height' : 'widthAndHeight';
  if (o.sh) props.sizingH = o.sh;
  if (o.lh) props.lineHeight = o.lh;
  if (o.ls) props.letterSpacing = o.ls;
  const params = { type: 'text', text, props, select: false, name: o.name ?? text.slice(0, 24) };
  if (o.x !== undefined) params.x = o.x; // posisi bebas bila induk tanpa auto layout
  if (o.y !== undefined) params.y = o.y;
  if (parent !== null) params.parent = parent;
  if (o.w !== undefined) params.width = o.w;
  return emit('node.create', params);
}

/** Ikon garis 24x24 dari PATHS. */
export function icon(parent, name, color, size = 24, sw = 2) {
  const params = {
    path: PATHS[name],
    width: ((EXT[name]?.[0] ?? 24) * size) / 24,
    height: ((EXT[name]?.[1] ?? 24) * size) / 24,
    name: `ikon-${name}`,
    parent,
    select: false,
    props: { fill: 'none', stroke: color, strokeWeight: sw, strokeCap: 'round', strokeJoin: 'round' },
  };
  if (!PATHS[name]) throw new Error(`ikon ${name}`);
  if (!EXT[name]?.[1]) delete params.height; // garis datar tanpa tinggi
  return emit('vector.create', params);
}

export function dot(parent, d, fill, name = 'dot') {
  return emit('shape.ellipse', { x: 0, y: 0, width: d, height: d, parent, name, props: { fill }, select: false });
}

export function rect(parent, w, h, fill, r = 0, name = 'rect') {
  return emit('shape.rectangle', { x: 0, y: 0, width: w, height: h, parent, name, props: { fill, cornerRadius: r }, select: false });
}

export function setProps(id, props) {
  cmds.push({ command: 'node.set', params: { ids: [id], props } });
}

export function lastId() {
  return nextId - 1;
}
