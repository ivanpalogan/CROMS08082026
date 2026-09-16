/* CROMS shared application shell — sidebar + topbar. Injected into #shell-mount.
   Each page sets window.CROMS_PAGE = { navKey, crumbs:['Group','Screen'], title, notifCount } before this script runs. */

const ICONS = {
  grid:'<rect x="2" y="2" width="7" height="7" rx="1.5"/><rect x="11" y="2" width="7" height="7" rx="1.5"/><rect x="2" y="11" width="7" height="7" rx="1.5"/><rect x="11" y="11" width="7" height="7" rx="1.5"/>',
  users:'<circle cx="7" cy="6.5" r="2.6"/><path d="M2 17c0-3 2.3-5 5-5s5 2 5 5"/><circle cx="15" cy="7.5" r="2.1"/><path d="M12.5 12.3c2.3.3 3.9 2 4.5 4.7"/>',
  list:'<path d="M2 5h2M2 10h2M2 15h2"/><path d="M7 5h11M7 10h11M7 15h11"/>',
  doc:'<path d="M5 2h7l4 4v12H5z"/><path d="M12 2v4h4"/><path d="M8 11h5M8 14h5"/>',
  heart:'<path d="M10 17S3 12.4 3 7.6C3 4.9 5 3 7.4 3 9 3 10 4 10 4s1-1 2.6-1C15 3 17 4.9 17 7.6 17 12.4 10 17 10 17z"/>',
  cross:'<circle cx="10" cy="10" r="8"/><path d="M10 6.5v7M6.5 10h7"/>',
  book:'<path d="M3 3.5C4.5 2.8 7 2.5 10 3.6c3-1.1 5.5-.8 7 -.1v12.5c-1.5-.7-4-1-7 .1-3-1.1-5.5-.8-7-.1z"/><path d="M10 3.6v12.5"/>',
  inbox:'<path d="M3 11l1.5-6.5h11L17 11"/><path d="M3 11v5h14v-5h-4.2a2.8 2.8 0 01-5.6 0z"/>',
  layers:'<path d="M10 3l7 3.4L10 9.8 3 6.4z"/><path d="M3 10.6l7 3.4 7-3.4"/><path d="M3 14.2l7 3.4 7-3.4"/>',
  check:'<circle cx="10" cy="10" r="8"/><path d="M6.3 10.2l2.4 2.4 5-5"/>',
  flag:'<path d="M5 17V3"/><path d="M5 4c2-1 4 1 6 0s4-1 6 0v7c-2-1-4 1-6 0s-4-1-6 0z"/>',
  scan:'<path d="M3 6V4a1 1 0 011-1h2M17 6V4a1 1 0 00-1-1h-2M3 14v2a1 1 0 001 1h2M17 14v2a1 1 0 01-1 1h-2"/><rect x="5.5" y="7" width="9" height="6" rx="1"/>',
  search:'<circle cx="8.5" cy="8.5" r="5.5"/><path d="M16.5 16.5L13 13"/>',
  archive:'<rect x="2.5" y="4" width="15" height="4" rx="1"/><path d="M4 8v7a1 1 0 001 1h10a1 1 0 001-1V8"/><path d="M8 11.5h4"/>',
  cash:'<rect x="2" y="5" width="16" height="10" rx="1.5"/><circle cx="10" cy="10" r="2.6"/><path d="M4.5 7.5v0M15.5 12.5v0"/>',
  chart:'<path d="M3 17V9M9 17V3M15 17v-6"/><path d="M2 17h16"/>',
  db:'<ellipse cx="10" cy="4.5" rx="6.5" ry="2.3"/><path d="M3.5 4.5v11c0 1.27 2.9 2.3 6.5 2.3s6.5-1.03 6.5-2.3v-11"/><path d="M3.5 10c0 1.27 2.9 2.3 6.5 2.3s6.5-1.03 6.5-2.3"/>',
  shield:'<path d="M10 2.5l6.5 2.4V9c0 4.6-2.9 7.6-6.5 8.5C6.4 16.6 3.5 13.6 3.5 9V4.9z"/><circle cx="10" cy="8.4" r="2"/><path d="M6.6 13.5c.6-1.7 2-2.5 3.4-2.5s2.8.8 3.4 2.5"/>',
  gear:'<circle cx="10" cy="10" r="2.6"/><path d="M10 2.5v2M10 15.5v2M17.5 10h-2M4.5 10h-2M15.3 4.7l-1.4 1.4M6.1 13.9l-1.4 1.4M15.3 15.3l-1.4-1.4M6.1 6.1L4.7 4.7"/>',
  bell:'<path d="M6 8a4 4 0 018 0c0 4 1.5 5 1.5 5h-11S6 12 6 8z"/><path d="M8.3 15.5a1.8 1.8 0 003.4 0"/>',
  home:'<path d="M3 9.5L10 3l7 6.5"/><path d="M5 8.5V17h10V8.5"/>'
};
function svgIcon(name){return `<svg class="ic" viewBox="0 0 20 20" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round">${ICONS[name]||ICONS.doc}</svg>`;}

const NAV = [
  {group:'Client services', items:[
    {key:'dashboard', label:'Dashboard', icon:'grid', href:'02-shell-dashboard.html'},
    {key:'queue', label:'Queue management', icon:'users', href:'20-queue-management.html', badge:6},
    {key:'transactions', label:'Transactions', icon:'list', href:'#'},
  ]},
  {group:'Civil registry', items:[
    {key:'birth', label:'Birth registration', icon:'doc', href:'#'},
    {key:'marriage', label:'Marriage registration', icon:'heart', href:'30-marriage-desk.html'},
    {key:'death', label:'Death registration', icon:'cross', href:'#'},
    {key:'books', label:'Registry books', icon:'book', href:'#'},
  ]},
  {group:'Applications & requests', items:[
    {key:'certrequest', label:'Certificate request', icon:'inbox', href:'#'},
    {key:'breqs', label:'PSA copies (BREQS)', icon:'layers', href:'#'},
    {key:'release', label:'Release & claim', icon:'check', href:'50-release-claim.html'},
  ]},
  {group:'Petitions & legal', items:[
    {key:'petitions', label:'Petitions & case tracking', icon:'flag', href:'#'},
  ]},
  {group:'Document processing', items:[
    {key:'ocr', label:'Intelligent document processing', icon:'scan', href:'40-ocr-verification.html'},
  ]},
  {group:'Records', items:[
    {key:'search', label:'Record search', icon:'search', href:'#'},
    {key:'archive', label:'Records archive', icon:'archive', href:'#'},
  ]},
  {group:'Operations', items:[
    {key:'fees', label:'Fees & payments', icon:'cash', href:'#'},
    {key:'reports', label:'Reports & analytics', icon:'chart', href:'#'},
  ]},
  {group:'Administration', items:[
    {key:'masterfiles', label:'Master files', icon:'db', href:'#'},
    {key:'users', label:'Users & audit trail', icon:'shield', href:'#'},
    {key:'settings', label:'Settings', icon:'gear', href:'#'},
  ]},
];

function renderShell(){
  const page = window.CROMS_PAGE || {navKey:'dashboard', crumbs:['Dashboard'], title:'Dashboard'};
  let nav = '';
  NAV.forEach(g=>{
    nav += `<div class="nav-group"><div class="group-label">${g.group}</div>`;
    g.items.forEach(it=>{
      const active = it.key===page.navKey ? ' active':'';
      const badge = it.badge ? `<span class="badge-count">${it.badge}</span>` : '';
      nav += `<a class="nav-item${active}" href="${it.href}" title="${it.label}"><span class="ic">${svgIcon(it.icon)}</span><span class="nav-label">${it.label}</span>${badge}</a>`;
    });
    nav += `</div>`;
  });

  const crumbs = (page.crumbs||[]).map((c,i,arr)=> i===arr.length-1 ? `<b>${c}</b>` : `<span>${c}</span><span class="sep">/</span>`).join('');

  document.getElementById('shell-mount').innerHTML = `
  <div class="app-shell">
    <div class="sidebar" id="sidebar">
      <div class="brand">
        <div class="brand-mark">C</div>
        <div class="brand-text"><b>CROMS</b><span>LCRO Peñablanca</span></div>
      </div>
      <div class="sidebar-scroll">${nav}</div>
      <div class="sidebar-collapse-btn" id="collapseBtn">« Collapse</div>
    </div>
    <div class="main-col">
      <div class="topbar">
        <div class="crumbs">${crumbs}</div>
        <div class="gsearch" id="gsearch">${svgIcon('search')}<span>Search records, registry no., clients, transactions…</span><kbd>Ctrl K</kbd></div>
        <div class="topbar-right">
          <div class="icon-btn" title="Notifications" id="bellBtn">${svgIcon('bell')}${page.notifCount?'<span class="dot"></span>':''}</div>
          <div class="user-chip" title="Account">
            <div class="av">JD</div>
            <div class="who"><b>Juan Dela Cruz</b><span>Registrar</span></div>
          </div>
        </div>
      </div>
      <div class="content" id="page-content"></div>
    </div>
  </div>`;

  document.getElementById('collapseBtn').addEventListener('click', ()=>{
    const sb = document.getElementById('sidebar');
    sb.classList.toggle('collapsed');
    document.getElementById('collapseBtn').innerText = sb.classList.contains('collapsed') ? '»' : '« Collapse';
  });

  document.title = 'CROMS — ' + (page.title||'');
  const body = document.getElementById('page-body-template');
  if(body){ document.getElementById('page-content').innerHTML = body.innerHTML; }
  if(window.onShellReady) window.onShellReady();
}
document.addEventListener('DOMContentLoaded', renderShell);
