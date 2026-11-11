const $ = (id) => document.getElementById(id);
const esc = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

/** Parses "11.5", "3/4" or "11 1/2" into a number; returns NaN when invalid. */
function parseInches(text) {
  const s = String(text).trim().replace(/["”″]|in$/gi, '').trim();
  if (!s) return NaN;
  const m = s.match(/^(?:(\d+(?:\.\d+)?)\s+)?(\d+)\s*\/\s*(\d+)$/);
  if (m) return (m[1] ? parseFloat(m[1]) : 0) + parseInt(m[2], 10) / parseInt(m[3], 10);
  return /^\d*\.?\d+$/.test(s) ? parseFloat(s) : NaN;
}

function fmt(v) {
  const sixteenths = Math.round(v * 16);
  const whole = Math.floor(sixteenths / 16);
  let rem = sixteenths % 16, den = 16;
  if (!rem) return `${whole}"`;
  while (rem % 2 === 0) { rem /= 2; den /= 2; }
  return whole ? `${whole} ${rem}/${den}"` : `${rem}/${den}"`;
}

let devices = JSON.parse(localStorage.getItem('mcd.devices') || '[]');
let catalog = [];

function save() { localStorage.setItem('mcd.devices', JSON.stringify(devices)); }

function renderDevices() {
  const body = document.querySelector('#devices tbody');
  body.innerHTML = '';
  devices.forEach((d, i) => {
    const tr = document.createElement('tr');
    tr.innerHTML = `<td><input class="name" value="${esc(d.name)}"></td>
      <td><input value="${esc(d.width)}"></td><td><input value="${esc(d.height)}"></td><td><input value="${esc(d.depth)}"></td>
      <td><input class="qty" type="number" min="1" max="20" value="${d.qty}"></td>
      <td><button type="button" aria-label="Remove">✕</button></td>`;
    const ins = tr.querySelectorAll('input');
    ['name', 'width', 'height', 'depth', 'qty'].forEach((k, n) => {
      ins[n].addEventListener('change', () => { d[k] = k === 'qty' ? parseInt(ins[n].value, 10) || 1 : ins[n].value; save(); });
    });
    tr.querySelector('button').addEventListener('click', () => { devices.splice(i, 1); save(); renderDevices(); });
    body.appendChild(tr);
  });
  $('empty').hidden = devices.length > 0;
  $('devices').hidden = devices.length === 0;
}

async function init() {
  catalog = await (await fetch('/api/catalog')).json();
  const sel = $('catalog');
  const groups = {};
  catalog.forEach((c, i) => {
    if (!groups[c.category]) { groups[c.category] = document.createElement('optgroup'); groups[c.category].label = c.category; sel.appendChild(groups[c.category]); }
    const o = document.createElement('option');
    o.value = i;
    o.textContent = `${c.name} — ${fmt(c.width)} × ${fmt(c.height)} × ${fmt(c.depth)}`;
    groups[c.category].appendChild(o);
  });
  renderDevices();
}

$('addCatalog').addEventListener('click', () => {
  const c = catalog[$('catalog').value];
  const existing = devices.find((d) => d.name === c.name);
  if (existing) existing.qty++;
  else devices.push({ name: c.name, width: String(c.width), height: String(c.height), depth: String(c.depth), qty: 1 });
  save(); renderDevices();
});

$('addCustom').addEventListener('click', () => {
  const name = $('cName').value.trim();
  if (!name) { $('error').textContent = 'Give the custom device a name.'; return; }
  devices.push({ name, width: $('cW').value, height: $('cH').value, depth: $('cD').value, qty: 1 });
  ['cName', 'cW', 'cH', 'cD'].forEach((id) => ($(id).value = ''));
  $('error').textContent = '';
  save(); renderDevices();
});

$('form').addEventListener('submit', async (e) => {
  e.preventDefault();
  $('error').textContent = '';
  const num = (id, label) => {
    const v = parseInches($(id).value);
    if (Number.isNaN(v)) throw new Error(`"${label}" is not a valid measurement.`);
    return v;
  };
  try {
    const dev = devices.map((d) => {
      const w = parseInches(d.width), h = parseInches(d.height), dp = parseInches(d.depth);
      if ([w, h, dp].some(Number.isNaN)) throw new Error(`Check the dimensions for "${d.name}".`);
      return { name: d.name, width: w, height: h, depth: dp, qty: d.qty };
    });
    const [sw, sl] = $('sheet').value.split('x').map(Number);
    const body = {
      devices: dev,
      materials: {
        carcassThickness: parseFloat($('carcass').value),
        backThickness: parseFloat($('back').value),
        sheetWidth: sw, sheetLength: sl,
        kerf: num('kerf', 'Saw kerf'),
      },
      options: {
        maxWidth: num('maxW', 'Max width'),
        maxHeight: num('maxH', 'Max height'),
        clearanceSide: num('cSide', 'Gap'),
        clearanceTop: num('cTop', 'Clearance above'),
        clearanceBack: num('cBack', 'Rear clearance'),
        plinthHeight: num('plinth', 'Plinth'),
      },
    };
    const res = await fetch('/api/design', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    const data = await res.json();
    if (!res.ok) throw new Error(data.error || 'Could not generate a design.');
    render(data, body.materials);
  } catch (err) {
    $('error').textContent = err.message;
  }
});

function render(r, mat) {
  const s = r.summary;
  const stat = (label, value) => `<div class="stat">${label}<b>${value}</b></div>`;
  const carcassSheets = r.sheets.filter((x) => x.material === 'carcass').length;
  const backSheets = r.sheets.filter((x) => x.material === 'back').length;
  let html = `<h2>Your media centre</h2>
    <div class="stats">
      ${stat('Overall (W × H × D)', `${fmt(s.outerWidth)} × ${fmt(s.outerHeight)} × ${fmt(s.outerDepth)}`)}
      ${stat('Shelf opening (W × D)', `${fmt(s.interiorWidth)} × ${fmt(s.interiorDepth)}`)}
      ${stat('Shelves', s.shelfCount)}
      ${stat(`${fmt(mat.carcassThickness)} sheets`, carcassSheets)}
      ${mat.backThickness > 0 ? stat(`${fmt(mat.backThickness)} sheets`, backSheets) : ''}
    </div>
    ${r.warnings.map((w) => `<div class="warn">⚠ ${esc(w)}</div>`).join('')}
    ${r.overviewSvg}
    <table><thead><tr><th>Shelf (bottom→top)</th><th>Opening height</th><th>Devices</th></tr></thead><tbody>
      ${s.shelves.map((x) => `<tr><td>${x.index}</td><td>${fmt(x.interiorHeight)}</td><td>${x.devices.map(esc).join(', ')}</td></tr>`).join('')}
    </tbody></table>

    <h2>Material cut list</h2>
    <table class="cuts"><thead><tr><th>Part</th><th>Name</th><th>Qty</th><th>Length</th><th>Width</th><th>Thickness</th></tr></thead><tbody>
      ${r.parts.map((p) => `<tr><td><b>${p.letter}</b></td><td>${esc(p.name)}</td><td>${p.qty}</td><td>${fmt(p.length)}</td><td>${fmt(p.width)}</td><td>${fmt(p.thickness)}</td></tr>`).join('')}
    </tbody></table>
    <p>Length runs with the grain. Sizes are finished sizes; saw kerf of ${fmt(mat.kerf)} is allowed for in the sheet layouts.</p>
    <h3>Shopping list</h3>
    <ul>
      <li>${carcassSheets} × ${mat.sheetWidth}" × ${mat.sheetLength}" sheet(s) of ${fmt(mat.carcassThickness)} material</li>
      ${backSheets ? `<li>${backSheets} × ${mat.sheetWidth}" × ${mat.sheetLength}" sheet(s) of ${fmt(mat.backThickness)} material</li>` : ''}
      ${r.hardware.map((h) => `<li>${esc(h)}</li>`).join('')}
    </ul>

    <h2>Sheet layouts</h2>
    ${r.sheetSvgs.join('')}

    <h2>Build plans</h2>
    ${r.steps.map((st) => `<article class="step"><div class="num">${st.number}</div><div><h3>${esc(st.title)}</h3><p>${esc(st.text)}</p></div>${st.svg}</article>`).join('')}`;
  $('out').innerHTML = html;
  $('result').hidden = false;
  $('result').scrollIntoView({ behavior: 'smooth' });
}

$('print').addEventListener('click', () => window.print());
init();
