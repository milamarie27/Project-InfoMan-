/* ═══════════════════════════════════════════════════════════
   Online Clearance System - Main Application Script
   ═══════════════════════════════════════════════════════════ */

const API = 'http://localhost:5000/api';

// ─── STATE ───────────────────────────────────────────────────────────────────
let auth = JSON.parse(localStorage.getItem('oc_auth') || 'null');
let currentView = null;

// ─── API CLIENT ──────────────────────────────────────────────────────────────
async function api(method, path, body = null, raw = false) {
  const opts = {
    method,
    headers: { 'Content-Type': 'application/json' },
  };
  if (auth?.token) opts.headers['Authorization'] = `Bearer ${auth.token}`;
  if (body) opts.body = JSON.stringify(body);

  const res = await fetch(`${API}${path}`, opts);
  if (raw) return res; // for file downloads

  let data;
  try { data = await res.json(); } catch { data = {}; }

  if (!res.ok) {
    const msg = data.message || data.title || `Error ${res.status}`;
    throw new Error(msg);
  }
  return data;
}

// ─── AUTH ─────────────────────────────────────────────────────────────────────
document.getElementById('login-form').addEventListener('submit', async e => {
  e.preventDefault();
  const btn = document.getElementById('login-btn');
  const err = document.getElementById('login-error');
  err.classList.add('hidden');
  btn.disabled = true; btn.textContent = 'Signing in...';
  try {
    const data = await api('POST', '/auth/login', {
      username: document.getElementById('login-user').value,
      password: document.getElementById('login-pass').value,
    });
    auth = data;
    localStorage.setItem('oc_auth', JSON.stringify(auth));
    initApp();
  } catch(ex) {
    err.textContent = ex.message;
    err.classList.remove('hidden');
  } finally {
    btn.disabled = false; btn.textContent = 'Sign In';
  }
});

document.getElementById('register-form').addEventListener('submit', async e => {
  e.preventDefault();
  const err = document.getElementById('reg-error');
  const suc = document.getElementById('reg-success');
  err.classList.add('hidden'); suc.classList.add('hidden');
  try {
    await api('POST', '/auth/register/student', {
      username: document.getElementById('reg-uname').value,
      password: document.getElementById('reg-pwd').value,
      firstName: document.getElementById('reg-fname').value,
      lastName:  document.getElementById('reg-lname').value,
      middleInitial: document.getElementById('reg-mi').value || null,
      suffixName: document.getElementById('reg-suffix').value || null,
      studentNumber: document.getElementById('reg-snum').value,
      curriculumId: parseInt(document.getElementById('reg-curriculum').value),
      status: document.getElementById('reg-status').value,
    });
    suc.textContent = 'Account created! You can now log in.';
    suc.classList.remove('hidden');
    setTimeout(() => showPage('page-login'), 2000);
  } catch(ex) {
    err.textContent = ex.message;
    err.classList.remove('hidden');
  }
});

function logout() {
  auth = null;
  localStorage.removeItem('oc_auth');
  showPage('page-login');
}

// ─── PAGE ROUTING ─────────────────────────────────────────────────────────────
function showPage(id) {
  document.querySelectorAll('.page').forEach(p => p.classList.add('hidden'));
  document.getElementById(id).classList.remove('hidden');
  if (id === 'page-register') loadCurriculaForRegister();
}

async function loadCurriculaForRegister() {
  try {
    // temp token-less call — public endpoint if configured, else skip
    const sel = document.getElementById('reg-curriculum');
    sel.innerHTML = '<option value="">Select section...</option>';
  } catch {}
}

// ─── INIT APP ─────────────────────────────────────────────────────────────────
function initApp() {
  if (!auth) { showPage('page-login'); return; }
  showPage('page-app');
  document.getElementById('nav-username').textContent = `${auth.fullName} (${auth.role})`;
  buildSidebar();
  navigateTo(getDefaultView());
}

function getDefaultView() {
  if (auth.role === 'admin')     return 'dashboard-admin';
  if (auth.role === 'signatory') return 'dashboard-signatory';
  return 'dashboard-student';
}

// ─── SIDEBAR ──────────────────────────────────────────────────────────────────
const MENUS = {
  admin: [
    { section: 'Dashboard' },
    { id: 'dashboard-admin',   icon: '📊', label: 'Overview' },
    { section: 'Students' },
    { id: 'students-list',     icon: '👨‍🎓', label: 'All Students' },
    { id: 'generate-clearance',icon: '⚡', label: 'Generate Clearance' },
    { section: 'Clearance' },
    { id: 'clearance-subjects-admin', icon: '📋', label: 'Subject Clearances' },
    { id: 'clearance-orgs-admin',     icon: '🏫', label: 'Org Clearances' },
    { section: 'Academic Setup' },
    { id: 'setup-courses',     icon: '📚', label: 'Courses' },
    { id: 'setup-curriculum',  icon: '📗', label: 'Curriculum' },
    { id: 'setup-periods',     icon: '📅', label: 'Academic Periods' },
    { id: 'setup-subjects',    icon: '📖', label: 'Subjects' },
    { id: 'setup-offerings',   icon: '🗓️', label: 'Subject Offerings' },
    { id: 'setup-orgs',        icon: '🏛️', label: 'Organizations' },
    { id: 'setup-signatories', icon: '✍️', label: 'Signatories' },
    { section: 'Reports' },
    { id: 'reports',           icon: '📈', label: 'Reports & Export' },
    { section: 'Announcements' },
    { id: 'announcements',     icon: '📢', label: 'Announcements' },
  ],
  signatory: [
    { section: 'Dashboard' },
    { id: 'dashboard-signatory', icon: '📊', label: 'Overview' },
    { section: 'Clearance' },
    { id: 'clearance-subjects-sign', icon: '📋', label: 'Subject Clearances' },
    { id: 'clearance-orgs-sign',     icon: '🏫', label: 'Org Clearances' },
    { section: 'Announcements' },
    { id: 'announcements',           icon: '📢', label: 'Announcements' },
  ],
  student: [
    { section: 'My Clearance' },
    { id: 'dashboard-student',   icon: '📊', label: 'My Dashboard' },
    { id: 'my-clearance',        icon: '📋', label: 'My Clearance Status' },
    { section: 'Info' },
    { id: 'announcements',       icon: '📢', label: 'Announcements' },
  ],
};

function buildSidebar() {
  const sb = document.getElementById('sidebar');
  const items = MENUS[auth.role] || MENUS.student;
  sb.innerHTML = items.map(item => {
    if (item.section) return `<div class="sidebar-section">${item.section}</div>`;
    return `<div class="sidebar-item" id="nav-${item.id}" onclick="navigateTo('${item.id}')">
              <span class="icon">${item.icon}</span>${item.label}
            </div>`;
  }).join('');
}

function setActiveNav(id) {
  document.querySelectorAll('.sidebar-item').forEach(el => el.classList.remove('active'));
  const el = document.getElementById(`nav-${id}`);
  if (el) el.classList.add('active');
}

// ─── NAVIGATION ───────────────────────────────────────────────────────────────
const VIEWS = {
  'dashboard-admin':            viewAdminDashboard,
  'dashboard-signatory':        viewSignatoryDashboard,
  'dashboard-student':          viewStudentDashboard,
  'students-list':              viewStudentsList,
  'generate-clearance':         viewGenerateClearance,
  'clearance-subjects-admin':   () => viewClearanceSubjects('admin'),
  'clearance-orgs-admin':       () => viewClearanceOrgs('admin'),
  'clearance-subjects-sign':    () => viewClearanceSubjects('signatory'),
  'clearance-orgs-sign':        () => viewClearanceOrgs('signatory'),
  'my-clearance':               viewMyClearance,
  'setup-courses':              viewCourses,
  'setup-curriculum':           viewCurriculum,
  'setup-periods':              viewPeriods,
  'setup-subjects':             viewSubjects,
  'setup-offerings':            viewOfferings,
  'setup-orgs':                 viewOrganizations,
  'setup-signatories':          viewSignatories,
  'reports':                    viewReports,
  'announcements':              viewAnnouncements,
};

function navigateTo(id) {
  currentView = id;
  setActiveNav(id);
  const fn = VIEWS[id];
  if (fn) fn();
}

function setContent(html) {
  document.getElementById('main-content').innerHTML = html;
}

function loading() {
  setContent('<div class="loading-spinner">Loading...</div>');
}

// ─── TOAST ────────────────────────────────────────────────────────────────────
let toastContainer;
function toast(msg, type = 'info') {
  if (!toastContainer) {
    toastContainer = document.createElement('div');
    toastContainer.id = 'toast-container';
    document.body.appendChild(toastContainer);
  }
  const t = document.createElement('div');
  t.className = `toast toast-${type}`;
  t.textContent = msg;
  toastContainer.appendChild(t);
  setTimeout(() => t.remove(), 3500);
}

// ─── MODAL ────────────────────────────────────────────────────────────────────
function openModal(title, bodyHtml) {
  document.getElementById('modal-title').textContent = title;
  document.getElementById('modal-body').innerHTML = bodyHtml;
  document.getElementById('modal-overlay').classList.remove('hidden');
}
function closeModal() {
  document.getElementById('modal-overlay').classList.add('hidden');
}
document.getElementById('modal-overlay').addEventListener('click', e => {
  if (e.target === document.getElementById('modal-overlay')) closeModal();
});

// ─── HELPERS ──────────────────────────────────────────────────────────────────
function fmt(dt) { return dt ? new Date(dt).toLocaleDateString('en-PH', { year:'numeric', month:'short', day:'numeric' }) : '—'; }
function fmtDt(dt) { return dt ? new Date(dt).toLocaleString('en-PH') : '—'; }
function statusBadge(label) {
  const map = { Pending:'pending', Cleared:'cleared', Rejected:'rejected',
                General:'general', Urgent:'urgent', Event:'event', Reminder:'reminder' };
  return `<span class="badge badge-${map[label]||'general'}">${label}</span>`;
}
function emptyState(msg = 'No data found.') {
  return `<div class="empty-state"><div class="es-icon">📭</div><p>${msg}</p></div>`;
}

// ═══════════════════════════════════════════════════════════
// VIEWS
// ═══════════════════════════════════════════════════════════

// ─── ADMIN DASHBOARD ──────────────────────────────────────
async function viewAdminDashboard() {
  loading();
  try {
    const [students, announcements, periods] = await Promise.all([
      api('GET', '/students'),
      api('GET', '/announcements'),
      api('GET', '/periods'),
    ]);
    const active = periods.find(p => p.isActive);

    let cleared = 0, pending = 0;
    if (active) {
      try {
        const report = await api('GET', `/reports/clearance?periodId=${active.id}`);
        cleared = report.filter(r => r.overallStatus === 'Cleared').length;
        pending = report.filter(r => r.overallStatus !== 'Cleared').length;
      } catch {}
    }

    setContent(`
      <div class="page-title">Admin Dashboard</div>
      <div class="stats-grid">
        <div class="stat-card blue">
          <div class="stat-icon">👨‍🎓</div>
          <div class="stat-value">${students.length}</div>
          <div class="stat-label">Total Students</div>
        </div>
        <div class="stat-card green">
          <div class="stat-icon">✅</div>
          <div class="stat-value">${cleared}</div>
          <div class="stat-label">Fully Cleared</div>
        </div>
        <div class="stat-card yellow">
          <div class="stat-icon">⏳</div>
          <div class="stat-value">${pending}</div>
          <div class="stat-label">Pending Clearance</div>
        </div>
        <div class="stat-card red">
          <div class="stat-icon">📢</div>
          <div class="stat-value">${announcements.length}</div>
          <div class="stat-label">Announcements</div>
        </div>
      </div>
      <div class="card">
        <div class="card-header">
          <h2>Active Period</h2>
        </div>
        ${active
          ? `<p>📅 <strong>${active.academicYear} — ${active.semester}</strong></p>
             <p style="margin-top:8px;color:var(--gray-500)">Period ID: ${active.id}</p>`
          : `<div class="alert alert-warning">No active academic period set. Go to Academic Periods to activate one.</div>`}
      </div>
      <div class="card" style="margin-top:20px">
        <div class="card-header"><h2>Recent Announcements</h2></div>
        ${announcements.slice(0,3).map(a => `
          <div class="ann-card">
            <div class="ann-meta">${statusBadge(a.type)}<span style="font-size:11px;color:var(--gray-400)">${fmt(a.createdAt)}</span></div>
            <div class="ann-title">${a.title}</div>
            <div class="ann-content">${a.content.substring(0,120)}...</div>
          </div>`).join('') || emptyState('No announcements yet.')}
      </div>
    `);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

// ─── SIGNATORY DASHBOARD ──────────────────────────────────
async function viewSignatoryDashboard() {
  loading();
  try {
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive);

    // find my signatory id
    const sigs = await api('GET', '/signatories');
    const me = sigs.find(s => s.employeeId === auth.employeeId);
    if (!me) { setContent('<div class="alert alert-warning">Signatory profile not found. Contact admin.</div>'); return; }

    let mySubjects = [], myOrgs = [];
    if (active) {
      mySubjects = await api('GET', `/clearance/subjects?periodId=${active.id}&instructorId=${me.id}`);
      myOrgs     = await api('GET', `/clearance/organizations?periodId=${active.id}&signatoryId=${me.id}`);
    }

    const pendingSub = mySubjects.filter(s => s.statusLabel === 'Pending').length;
    const pendingOrg = myOrgs.filter(o => o.statusLabel === 'Pending').length;

    setContent(`
      <div class="page-title">My Dashboard</div>
      <div class="stats-grid">
        <div class="stat-card blue"><div class="stat-icon">📋</div><div class="stat-value">${mySubjects.length}</div><div class="stat-label">Subject Clearances</div></div>
        <div class="stat-card yellow"><div class="stat-icon">⏳</div><div class="stat-value">${pendingSub}</div><div class="stat-label">Pending Subjects</div></div>
        <div class="stat-card green"><div class="stat-icon">🏫</div><div class="stat-value">${myOrgs.length}</div><div class="stat-label">Org Clearances</div></div>
        <div class="stat-card red"><div class="stat-icon">⏳</div><div class="stat-value">${pendingOrg}</div><div class="stat-label">Pending Orgs</div></div>
      </div>
      <div class="card">
        <div class="card-header"><h2>Active Period</h2></div>
        ${active ? `<p>📅 <strong>${active.academicYear} — ${active.semester}</strong></p>` : '<div class="alert alert-warning">No active period.</div>'}
      </div>
    `);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

// ─── STUDENT DASHBOARD ────────────────────────────────────
async function viewStudentDashboard() {
  loading();
  try {
    const studentRes = await api('GET', `/students/by-number/${auth.studentNumber}`);
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive);
    const ann = await api('GET', '/announcements');

    let summary = null;
    if (active) {
      try { summary = await api('GET', `/clearance/summary/${studentRes.id}/${active.id}`); } catch {}
    }

    const totalPct = summary
      ? Math.round(((summary.clearedSubjectItems + summary.clearedOrgItems) /
          Math.max(1, summary.totalSubjectItems + summary.totalOrgItems)) * 100)
      : 0;

    setContent(`
      <div class="page-title">Welcome, ${auth.fullName}!</div>
      <div class="stats-grid">
        <div class="stat-card blue"><div class="stat-icon">🎓</div><div class="stat-value">${studentRes.courseCode}</div><div class="stat-label">${studentRes.courseDescription}</div></div>
        <div class="stat-card green"><div class="stat-icon">📚</div><div class="stat-value">Year ${studentRes.yearLevel}</div><div class="stat-label">Section ${studentRes.section}</div></div>
        <div class="stat-card ${summary?.isFullyCleared ? 'green' : 'yellow'}">
          <div class="stat-icon">${summary?.isFullyCleared ? '✅' : '⏳'}</div>
          <div class="stat-value">${totalPct}%</div>
          <div class="stat-label">${summary?.isFullyCleared ? 'Fully Cleared' : 'Clearance Progress'}</div>
        </div>
      </div>
      ${summary ? `
        <div class="card section-gap">
          <div class="card-header"><h2>Clearance Progress — ${summary.academicYear} ${summary.semester}</h2></div>
          <div style="margin-bottom:16px">
            <div style="display:flex;justify-content:space-between;margin-bottom:6px;font-size:13px">
              <span>Overall Progress</span><span>${totalPct}%</span>
            </div>
            <div class="progress"><div class="progress-bar ${totalPct===100?'green':totalPct>50?'':'red'}" style="width:${totalPct}%"></div></div>
          </div>
          <div class="clearance-summary">
            <div class="cs-item"><div class="cs-num" style="color:var(--blue-600)">${summary.clearedSubjectItems}/${summary.totalSubjectItems}</div><div class="cs-lbl">Subjects Cleared</div></div>
            <div class="cs-item"><div class="cs-num" style="color:var(--green-600)">${summary.clearedOrgItems}/${summary.totalOrgItems}</div><div class="cs-lbl">Orgs Cleared</div></div>
          </div>
          ${summary.isFullyCleared ? `
            <div class="fully-cleared">
              <div class="fc-icon">🎉</div>
              <h3>You are fully cleared!</h3>
              <p>All requirements for ${summary.academicYear} ${summary.semester} have been signed off.</p>
            </div>` : ''}
        </div>` : active ? `<div class="alert alert-info">No clearance entries found for the active period. Contact the registrar to generate your clearance.</div>` : ''}
      <div class="card">
        <div class="card-header"><h2>Latest Announcements</h2></div>
        ${ann.slice(0,5).map(a => `
          <div class="ann-card">
            <div class="ann-meta">${statusBadge(a.type)}<span style="font-size:11px;color:var(--gray-400)">${fmt(a.createdAt)}</span></div>
            <div class="ann-title">${a.title}</div>
            <div class="ann-content">${a.content}</div>
          </div>`).join('') || emptyState()}
      </div>
    `);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

// ─── MY CLEARANCE (STUDENT) ───────────────────────────────
async function viewMyClearance() {
  loading();
  try {
    const studentRes = await api('GET', `/students/by-number/${auth.studentNumber}`);
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive);
    if (!active) { setContent('<div class="alert alert-warning">No active academic period.</div>'); return; }

    const [subjects, orgs] = await Promise.all([
      api('GET', `/clearance/subjects?studentId=${studentRes.id}&periodId=${active.id}`),
      api('GET', `/clearance/organizations?studentId=${studentRes.id}&periodId=${active.id}`),
    ]);

    setContent(`
      <div class="page-title">My Clearance — ${active.academicYear} ${active.semester}</div>
      <div class="clearance-section">
        <h3>📚 Subject Clearances</h3>
        ${subjects.length ? `
          <div class="table-wrap">
            <table>
              <thead><tr><th>MIS Code</th><th>Subject</th><th>Instructor</th><th>Status</th><th>Remarks</th><th>Signed At</th></tr></thead>
              <tbody>
                ${subjects.map(s => `<tr>
                  <td><code>${s.misCode}</code></td>
                  <td>${s.subjectCode}</td>
                  <td>${s.instructorName}</td>
                  <td>${statusBadge(s.statusLabel)}</td>
                  <td>${s.remarks || '—'}</td>
                  <td>${fmt(s.signedAt)}</td>
                </tr>`).join('')}
              </tbody>
            </table>
          </div>` : emptyState('No subject clearances generated yet.')}
      </div>
      <div class="clearance-section">
        <h3>🏫 Organization Clearances</h3>
        ${orgs.length ? `
          <div class="table-wrap">
            <table>
              <thead><tr><th>Organization</th><th>Position</th><th>Signatory</th><th>Status</th><th>Remarks</th><th>Signed At</th></tr></thead>
              <tbody>
                ${orgs.map(o => `<tr>
                  <td>${o.orgName}</td>
                  <td>${o.positionTitle}</td>
                  <td>${o.signatoryName}</td>
                  <td>${statusBadge(o.statusLabel)}</td>
                  <td>${o.remarks || '—'}</td>
                  <td>${fmt(o.signedAt)}</td>
                </tr>`).join('')}
              </tbody>
            </table>
          </div>` : emptyState('No organization clearances found.')}
      </div>
    `);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

// ─── STUDENTS LIST ────────────────────────────────────────
async function viewStudentsList() {
  loading();
  try {
    const [students, curricula] = await Promise.all([
      api('GET', '/students'),
      api('GET', '/curriculum'),
    ]);

    setContent(`
      <div class="page-title">Students</div>
      <div class="card">
        <div class="card-header">
          <h2>All Students (${students.length})</h2>
          <div class="card-actions">
            <button class="btn btn-primary btn-sm" onclick="showRegisterStudentModal()">+ Add Student</button>
          </div>
        </div>
        <div class="filter-bar">
          <input type="text" id="student-search" placeholder="Search by name or student no..." oninput="filterStudents()" />
          <select id="student-curriculum" onchange="filterStudents()">
            <option value="">All Sections</option>
            ${curricula.map(c => `<option value="${c.id}">${c.courseCode} Y${c.yearLevel}-${c.section}</option>`).join('')}
          </select>
        </div>
        <div class="table-wrap">
          <table id="students-table">
            <thead><tr><th>Student No.</th><th>Full Name</th><th>Course</th><th>Year</th><th>Section</th><th>Status</th><th>Actions</th></tr></thead>
            <tbody id="students-tbody">
              ${students.map(s => studentRow(s)).join('')}
            </tbody>
          </table>
        </div>
      </div>
    `);
    window._allStudents = students;
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

function studentRow(s) {
  return `<tr>
    <td><strong>${s.studentNumber}</strong></td>
    <td>${s.fullName}</td>
    <td>${s.courseCode}</td>
    <td>${s.yearLevel}</td>
    <td>${s.section}</td>
    <td>${statusBadge(s.status)}</td>
    <td>
      <button class="btn btn-ghost btn-sm" onclick="viewStudentClearance(${s.id})">📋 Clearance</button>
      <button class="btn btn-danger btn-sm" onclick="deleteStudent(${s.id})">🗑</button>
    </td>
  </tr>`;
}

function filterStudents() {
  const q = document.getElementById('student-search').value.toLowerCase();
  const cid = document.getElementById('student-curriculum').value;
  const rows = (window._allStudents || []).filter(s => {
    const matchQ = !q || s.studentNumber.toLowerCase().includes(q) || s.fullName.toLowerCase().includes(q);
    const matchC = !cid || s.curriculumId == cid;
    return matchQ && matchC;
  });
  document.getElementById('students-tbody').innerHTML = rows.map(studentRow).join('') || `<tr><td colspan="7">${emptyState()}</td></tr>`;
}

async function deleteStudent(id) {
  if (!confirm('Delete this student?')) return;
  try {
    await api('DELETE', `/students/${id}`);
    toast('Student deleted.', 'success');
    viewStudentsList();
  } catch(ex) { toast(ex.message, 'error'); }
}

async function viewStudentClearance(studentId) {
  try {
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive) || periods[0];
    if (!active) { toast('No period available.', 'error'); return; }
    const [subjects, orgs, student] = await Promise.all([
      api('GET', `/clearance/subjects?studentId=${studentId}&periodId=${active.id}`),
      api('GET', `/clearance/organizations?studentId=${studentId}&periodId=${active.id}`),
      api('GET', `/students/${studentId}`),
    ]);
    openModal(`Clearance — ${student.fullName}`, `
      <p style="margin-bottom:12px"><strong>Period:</strong> ${active.academicYear} ${active.semester}</p>
      <p style="margin-bottom:8px;font-weight:600">📚 Subject Clearances</p>
      ${subjects.length ? `<div class="table-wrap" style="margin-bottom:16px"><table>
        <thead><tr><th>Subject</th><th>Status</th><th>Remarks</th></tr></thead>
        <tbody>${subjects.map(s => `<tr><td>${s.subjectCode}</td><td>${statusBadge(s.statusLabel)}</td><td>${s.remarks||'—'}</td></tr>`).join('')}</tbody>
      </table></div>` : '<p style="color:var(--gray-400);margin-bottom:16px">None</p>'}
      <p style="margin-bottom:8px;font-weight:600">🏫 Org Clearances</p>
      ${orgs.length ? `<div class="table-wrap"><table>
        <thead><tr><th>Organization</th><th>Status</th><th>Remarks</th></tr></thead>
        <tbody>${orgs.map(o => `<tr><td>${o.orgName}</td><td>${statusBadge(o.statusLabel)}</td><td>${o.remarks||'—'}</td></tr>`).join('')}</tbody>
      </table></div>` : '<p style="color:var(--gray-400)">None</p>'}
    `);
  } catch(ex) { toast(ex.message, 'error'); }
}

// ─── GENERATE CLEARANCE ───────────────────────────────────
async function viewGenerateClearance() {
  loading();
  try {
    const [students, periods] = await Promise.all([
      api('GET', '/students'),
      api('GET', '/periods'),
    ]);
    const active = periods.find(p => p.isActive);

    setContent(`
      <div class="page-title">Generate Clearance</div>
      <div class="card">
        <div class="card-header"><h2>Bulk Generate for Active Period</h2></div>
        ${!active ? '<div class="alert alert-warning">No active period. Activate a period first.</div>' : `
          <div class="alert alert-info" style="margin-bottom:16px">
            Active Period: <strong>${active.academicYear} — ${active.semester}</strong>
          </div>
          <p style="margin-bottom:16px;color:var(--gray-600)">This will create clearance entries (subject + org) for each selected student for the active period. Existing entries are skipped.</p>
          <div class="form-group">
            <label>Select Student (or generate for all)</label>
            <select id="gen-student">
              <option value="all">— All Students —</option>
              ${students.map(s => `<option value="${s.id}">${s.studentNumber} — ${s.fullName}</option>`).join('')}
            </select>
          </div>
          <button class="btn btn-primary" onclick="runGenerateClearance(${active.id}, ${JSON.stringify(students.map(s=>s.id))})">⚡ Generate Clearance</button>
        `}
      </div>
    `);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

async function runGenerateClearance(periodId, allIds) {
  const sel = document.getElementById('gen-student').value;
  const ids = sel === 'all' ? allIds : [parseInt(sel)];
  const btn = event.target;
  btn.disabled = true; btn.textContent = 'Generating...';
  let ok = 0, fail = 0;
  for (const id of ids) {
    try { await api('POST', '/clearance/generate', { studentId: id, periodId }); ok++; }
    catch { fail++; }
  }
  btn.disabled = false; btn.textContent = '⚡ Generate Clearance';
  toast(`Done: ${ok} generated, ${fail} failed.`, ok > 0 ? 'success' : 'error');
}

// ─── CLEARANCE SUBJECTS (ADMIN + SIGNATORY) ───────────────
async function viewClearanceSubjects(role) {
  loading();
  try {
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive) || periods[0];

    let instructorId = null;
    if (role === 'signatory') {
      const sigs = await api('GET', '/signatories');
      const me = sigs.find(s => s.employeeId === auth.employeeId);
      instructorId = me?.id;
    }

    const url = `/clearance/subjects?${active ? `periodId=${active.id}` : ''}${instructorId ? `&instructorId=${instructorId}` : ''}`;
    const items = await api('GET', url);

    setContent(`
      <div class="page-title">Subject Clearances</div>
      <div class="card">
        <div class="card-header">
          <h2>${active ? `${active.academicYear} — ${active.semester}` : 'All Periods'}</h2>
          ${role === 'signatory' ? `<button class="btn btn-success btn-sm" onclick="bulkApprove(${active?.id})">✅ Bulk Approve All Pending</button>` : ''}
        </div>
        <div class="filter-bar">
          <input type="text" id="cs-search" placeholder="Search student or subject..." oninput="filterClearanceTable('cs')" />
          <select id="cs-status" onchange="filterClearanceTable('cs')">
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Cleared">Cleared</option>
            <option value="Rejected">Rejected</option>
          </select>
        </div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Student No.</th><th>Student Name</th><th>MIS Code</th><th>Subject</th><th>Instructor</th><th>Status</th><th>Remarks</th><th>Signed At</th><th>Action</th></tr></thead>
            <tbody id="cs-tbody">
              ${items.map(s => csRow(s, role)).join('') || `<tr><td colspan="9">${emptyState()}</td></tr>`}
            </tbody>
          </table>
        </div>
      </div>
    `);
    window._csItems = items;
    window._csRole = role;
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

function csRow(s, role) {
  const canApprove = role !== 'student' && s.statusLabel === 'Pending';
  return `<tr id="cs-row-${s.id}">
    <td>${s.studentNumber}</td>
    <td>${s.studentName}</td>
    <td><code>${s.misCode}</code></td>
    <td>${s.subjectCode}</td>
    <td>${s.instructorName}</td>
    <td>${statusBadge(s.statusLabel)}</td>
    <td>${s.remarks || '—'}</td>
    <td>${fmt(s.signedAt)}</td>
    <td>
      ${canApprove ? `
        <button class="btn btn-success btn-sm" onclick="approveSubject(${s.id}, 2)">✅</button>
        <button class="btn btn-danger btn-sm" onclick="openRejectModal('subject', ${s.id})">❌</button>
      ` : '—'}
    </td>
  </tr>`;
}

function filterClearanceTable(prefix) {
  const q = (document.getElementById(`${prefix}-search`)?.value || '').toLowerCase();
  const st = document.getElementById(`${prefix}-status`)?.value || '';
  const src = prefix === 'cs' ? window._csItems : window._coItems;
  const role = prefix === 'cs' ? window._csRole : window._coRole;
  const fn = prefix === 'cs' ? csRow : coRow;
  const filtered = (src || []).filter(i => {
    const matchQ = !q || i.studentNumber.toLowerCase().includes(q) || i.studentName.toLowerCase().includes(q);
    const matchS = !st || i.statusLabel === st;
    return matchQ && matchS;
  });
  document.getElementById(`${prefix}-tbody`).innerHTML = filtered.map(i => fn(i, role)).join('') || `<tr><td colspan="9">${emptyState()}</td></tr>`;
}

async function approveSubject(id, statusId, remarks = null) {
  try {
    await api('PUT', `/clearance/subjects/${id}/approve`, { statusId, remarks });
    toast('Updated.', 'success');
    viewClearanceSubjects(window._csRole || 'admin');
  } catch(ex) { toast(ex.message, 'error'); }
}

async function approveOrg(id, statusId, remarks = null) {
  try {
    await api('PUT', `/clearance/organizations/${id}/approve`, { statusId, remarks });
    toast('Updated.', 'success');
    viewClearanceOrgs(window._coRole || 'admin');
  } catch(ex) { toast(ex.message, 'error'); }
}

function openRejectModal(type, id) {
  openModal('Reject Clearance', `
    <div class="form-group"><label>Remarks / Reason for Rejection</label><textarea id="reject-remarks" placeholder="Enter reason..."></textarea></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-danger" onclick="${type==='subject' ? `approveSubject(${id}, 3, document.getElementById('reject-remarks').value)` : `approveOrg(${id}, 3, document.getElementById('reject-remarks').value)`};closeModal()">Confirm Reject</button>
    </div>
  `);
}

async function bulkApprove(periodId) {
  if (!confirm('Approve all pending subject clearances for this period?')) return;
  try {
    const res = await api('POST', '/clearance/subjects/bulk-approve', { periodId });
    toast(res.message, 'success');
    viewClearanceSubjects('signatory');
  } catch(ex) { toast(ex.message, 'error'); }
}

// ─── CLEARANCE ORGS ───────────────────────────────────────
async function viewClearanceOrgs(role) {
  loading();
  try {
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive) || periods[0];

    let signatoryId = null;
    if (role === 'signatory') {
      const sigs = await api('GET', '/signatories');
      const me = sigs.find(s => s.employeeId === auth.employeeId);
      signatoryId = me?.id;
    }

    const url = `/clearance/organizations?${active ? `periodId=${active.id}` : ''}${signatoryId ? `&signatoryId=${signatoryId}` : ''}`;
    const items = await api('GET', url);

    setContent(`
      <div class="page-title">Organization Clearances</div>
      <div class="card">
        <div class="card-header">
          <h2>${active ? `${active.academicYear} — ${active.semester}` : 'All Periods'}</h2>
        </div>
        <div class="filter-bar">
          <input type="text" id="co-search" placeholder="Search student or org..." oninput="filterClearanceTable('co')" />
          <select id="co-status" onchange="filterClearanceTable('co')">
            <option value="">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Cleared">Cleared</option>
            <option value="Rejected">Rejected</option>
          </select>
        </div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Student No.</th><th>Student</th><th>Organization</th><th>Position</th><th>Signatory</th><th>Status</th><th>Remarks</th><th>Signed</th><th>Action</th></tr></thead>
            <tbody id="co-tbody">
              ${items.map(o => coRow(o, role)).join('') || `<tr><td colspan="9">${emptyState()}</td></tr>`}
            </tbody>
          </table>
        </div>
      </div>
    `);
    window._coItems = items;
    window._coRole = role;
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}

function coRow(o, role) {
  const canApprove = role !== 'student' && o.statusLabel === 'Pending';
  return `<tr>
    <td>${o.studentNumber}</td>
    <td>${o.studentName}</td>
    <td>${o.orgName}</td>
    <td>${o.positionTitle}</td>
    <td>${o.signatoryName}</td>
    <td>${statusBadge(o.statusLabel)}</td>
    <td>${o.remarks || '—'}</td>
    <td>${fmt(o.signedAt)}</td>
    <td>
      ${canApprove ? `
        <button class="btn btn-success btn-sm" onclick="approveOrg(${o.id}, 2)">✅</button>
        <button class="btn btn-danger btn-sm" onclick="openRejectModal('org', ${o.id})">❌</button>
      ` : '—'}
    </td>
  </tr>`;
}

// ─── SETUP: COURSES ───────────────────────────────────────
async function viewCourses() {
  loading();
  try {
    const courses = await api('GET', '/courses');
    setContent(`
      <div class="page-title">Courses</div>
      <div class="card">
        <div class="card-header"><h2>All Courses</h2>
          <button class="btn btn-primary btn-sm" onclick="openModal('Add Course', courseForm())">+ Add Course</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>ID</th><th>Course Code</th><th>Description</th><th>Actions</th></tr></thead>
          <tbody>${courses.map(c => `<tr>
            <td>${c.id}</td><td><strong>${c.courseCode}</strong></td><td>${c.description||'—'}</td>
            <td><button class="btn btn-danger btn-sm" onclick="deleteCourse(${c.id})">🗑</button></td>
          </tr>`).join('') || `<tr><td colspan="4">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function courseForm(c = {}) {
  return `<div class="form-group"><label>Course Code*</label><input id="cf-code" value="${c.courseCode||''}" required/></div>
    <div class="form-group"><label>Description</label><input id="cf-desc" value="${c.description||''}"/></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveCourse()">Save</button>
    </div>`;
}
async function saveCourse() {
  try {
    await api('POST', '/courses', { courseCode: document.getElementById('cf-code').value, description: document.getElementById('cf-desc').value || null });
    toast('Course saved.', 'success'); closeModal(); viewCourses();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deleteCourse(id) {
  if (!confirm('Delete this course?')) return;
  try { await api('DELETE', `/courses/${id}`); toast('Deleted.', 'success'); viewCourses(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── SETUP: CURRICULUM ───────────────────────────────────
async function viewCurriculum() {
  loading();
  try {
    const [curricula, courses] = await Promise.all([api('GET', '/curriculum'), api('GET', '/courses')]);
    setContent(`
      <div class="page-title">Curriculum</div>
      <div class="card">
        <div class="card-header"><h2>All Curriculum Entries</h2>
          <button class="btn btn-primary btn-sm" onclick="openModal('Add Curriculum', curriculumForm(${JSON.stringify(courses)}))">+ Add</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>ID</th><th>Course</th><th>Year Level</th><th>Section</th><th>Actions</th></tr></thead>
          <tbody>${curricula.map(c => `<tr>
            <td>${c.id}</td><td>${c.courseCode}</td><td>Year ${c.yearLevel}</td><td>${c.section}</td>
            <td><button class="btn btn-danger btn-sm" onclick="deleteCurriculum(${c.id})">🗑</button></td>
          </tr>`).join('') || `<tr><td colspan="5">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function curriculumForm(courses) {
  return `<div class="form-group"><label>Course*</label><select id="crf-course">${courses.map(c=>`<option value="${c.id}">${c.courseCode}</option>`).join('')}</select></div>
    <div class="form-row">
      <div class="form-group"><label>Year Level*</label><select id="crf-year"><option value="1">1st Year</option><option value="2">2nd Year</option><option value="3">3rd Year</option><option value="4">4th Year</option></select></div>
      <div class="form-group"><label>Section*</label><input id="crf-section" placeholder="e.g. A"/></div>
    </div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveCurriculum()">Save</button>
    </div>`;
}
async function saveCurriculum() {
  try {
    await api('POST', '/curriculum', { courseId: parseInt(document.getElementById('crf-course').value), yearLevel: parseInt(document.getElementById('crf-year').value), section: document.getElementById('crf-section').value });
    toast('Curriculum added.', 'success'); closeModal(); viewCurriculum();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deleteCurriculum(id) {
  if (!confirm('Delete?')) return;
  try { await api('DELETE', `/curriculum/${id}`); toast('Deleted.', 'success'); viewCurriculum(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── SETUP: PERIODS ───────────────────────────────────────
async function viewPeriods() {
  loading();
  try {
    const periods = await api('GET', '/periods');
    setContent(`
      <div class="page-title">Academic Periods</div>
      <div class="card">
        <div class="card-header"><h2>Academic Periods</h2>
          <button class="btn btn-primary btn-sm" onclick="openModal('Add Period', periodForm())">+ Add</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>ID</th><th>Academic Year</th><th>Semester</th><th>Status</th><th>Actions</th></tr></thead>
          <tbody>${periods.map(p => `<tr>
            <td>${p.id}</td><td>${p.academicYear}</td><td>${p.semester}</td>
            <td>${p.isActive ? '<span class="badge badge-cleared">Active</span>' : '<span class="badge badge-pending">Inactive</span>'}</td>
            <td>
              ${!p.isActive ? `<button class="btn btn-success btn-sm" onclick="activatePeriod(${p.id})">✅ Activate</button>` : ''}
              <button class="btn btn-danger btn-sm" onclick="deletePeriod(${p.id})">🗑</button>
            </td>
          </tr>`).join('') || `<tr><td colspan="5">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function periodForm() {
  return `<div class="form-group"><label>Academic Year*</label><input id="pf-year" placeholder="e.g. 2024-2025"/></div>
    <div class="form-group"><label>Semester*</label><select id="pf-sem"><option>1st Semester</option><option>2nd Semester</option><option>Summer</option></select></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="savePeriod()">Save</button>
    </div>`;
}
async function savePeriod() {
  try {
    await api('POST', '/periods', { academicYear: document.getElementById('pf-year').value, semester: document.getElementById('pf-sem').value });
    toast('Period added.', 'success'); closeModal(); viewPeriods();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function activatePeriod(id) {
  try { await api('PUT', `/periods/${id}/activate`); toast('Period activated!', 'success'); viewPeriods(); }
  catch(ex) { toast(ex.message, 'error'); }
}
async function deletePeriod(id) {
  if (!confirm('Delete this period?')) return;
  try { await api('DELETE', `/periods/${id}`); toast('Deleted.', 'success'); viewPeriods(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── SETUP: SUBJECTS ──────────────────────────────────────
async function viewSubjects() {
  loading();
  try {
    const subjects = await api('GET', '/subjects');
    setContent(`
      <div class="page-title">Subjects</div>
      <div class="card">
        <div class="card-header"><h2>All Subjects</h2>
          <button class="btn btn-primary btn-sm" onclick="openModal('Add Subject', subjectForm())">+ Add</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>Code</th><th>Title</th><th>Lec Units</th><th>Lab Units</th><th>Actions</th></tr></thead>
          <tbody>${subjects.map(s => `<tr>
            <td><code>${s.subjectCode}</code></td><td>${s.title}</td><td>${s.lecUnits}</td><td>${s.labUnits}</td>
            <td><button class="btn btn-danger btn-sm" onclick="deleteSubject(${s.id})">🗑</button></td>
          </tr>`).join('') || `<tr><td colspan="5">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function subjectForm() {
  return `<div class="form-row">
    <div class="form-group"><label>Subject Code*</label><input id="sf-code"/></div>
    <div class="form-group"><label>Title*</label><input id="sf-title"/></div>
  </div>
  <div class="form-row">
    <div class="form-group"><label>Lec Units</label><input id="sf-lec" type="number" value="3" min="0"/></div>
    <div class="form-group"><label>Lab Units</label><input id="sf-lab" type="number" value="0" min="0"/></div>
  </div>
  <div class="form-actions">
    <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
    <button class="btn btn-primary" onclick="saveSubject()">Save</button>
  </div>`;
}
async function saveSubject() {
  try {
    await api('POST', '/subjects', { subjectCode: document.getElementById('sf-code').value, title: document.getElementById('sf-title').value, lecUnits: parseInt(document.getElementById('sf-lec').value), labUnits: parseInt(document.getElementById('sf-lab').value) });
    toast('Subject saved.', 'success'); closeModal(); viewSubjects();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deleteSubject(id) {
  if (!confirm('Delete?')) return;
  try { await api('DELETE', `/subjects/${id}`); toast('Deleted.', 'success'); viewSubjects(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── SETUP: OFFERINGS ────────────────────────────────────
async function viewOfferings() {
  loading();
  try {
    const [offerings, periods, sigs, subjects] = await Promise.all([
      api('GET', '/offerings'),
      api('GET', '/periods'),
      api('GET', '/signatories'),
      api('GET', '/subjects'),
    ]);
    setContent(`
      <div class="page-title">Subject Offerings</div>
      <div class="card">
        <div class="card-header"><h2>All Offerings</h2>
          <button class="btn btn-primary btn-sm" onclick="openModal('Add Offering', offeringForm(${JSON.stringify(periods)}, ${JSON.stringify(sigs)}, ${JSON.stringify(subjects)}))">+ Add</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>MIS Code</th><th>Subject</th><th>Instructor</th><th>Period</th><th>Actions</th></tr></thead>
          <tbody>${offerings.map(o => `<tr>
            <td><code>${o.misCode}</code></td><td>${o.subjectCode}</td><td>${o.instructorName}</td>
            <td>${o.academicYear} ${o.semester}</td>
            <td><button class="btn btn-danger btn-sm" onclick="deleteOffering(${o.id})">🗑</button></td>
          </tr>`).join('') || `<tr><td colspan="5">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function offeringForm(periods, sigs, subjects) {
  return `<div class="form-group"><label>MIS Code*</label><input id="of-mis"/></div>
    <div class="form-group"><label>Subject Code*</label><select id="of-subject">${subjects.map(s=>`<option value="${s.subjectCode}">${s.subjectCode} — ${s.title}</option>`).join('')}</select></div>
    <div class="form-group"><label>Instructor*</label><select id="of-instructor">${sigs.map(s=>`<option value="${s.id}">${s.fullName} (${s.employeeId})</option>`).join('')}</select></div>
    <div class="form-group"><label>Period*</label><select id="of-period">${periods.map(p=>`<option value="${p.id}">${p.academicYear} — ${p.semester}</option>`).join('')}</select></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveOffering()">Save</button>
    </div>`;
}
async function saveOffering() {
  try {
    await api('POST', '/offerings', { misCode: document.getElementById('of-mis').value, subjectCode: document.getElementById('of-subject').value, instructorId: parseInt(document.getElementById('of-instructor').value), periodId: parseInt(document.getElementById('of-period').value) });
    toast('Offering saved.', 'success'); closeModal(); viewOfferings();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deleteOffering(id) {
  if (!confirm('Delete?')) return;
  try { await api('DELETE', `/offerings/${id}`); toast('Deleted.', 'success'); viewOfferings(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── SETUP: ORGANIZATIONS ─────────────────────────────────
async function viewOrganizations() {
  loading();
  try {
    const [orgs, sigs, curricula] = await Promise.all([api('GET', '/organizations'), api('GET', '/signatories'), api('GET', '/curriculum')]);
    setContent(`
      <div class="page-title">Organizations</div>
      <div class="card">
        <div class="card-header"><h2>All Organizations</h2>
          <button class="btn btn-primary btn-sm" onclick="openModal('Add Organization', orgForm(${JSON.stringify(sigs)}, ${JSON.stringify(curricula)}))">+ Add</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>Organization</th><th>Position Title</th><th>Signatory</th><th>Curriculum</th><th>Actions</th></tr></thead>
          <tbody>${orgs.map(o => `<tr>
            <td><strong>${o.orgName}</strong></td><td>${o.positionTitle}</td><td>${o.signatoryName}</td>
            <td>${o.courseCode} Y${o.yearLevel}-${o.section}</td>
            <td><button class="btn btn-danger btn-sm" onclick="deleteOrg(${o.id})">🗑</button></td>
          </tr>`).join('') || `<tr><td colspan="5">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function orgForm(sigs, curricula) {
  return `<div class="form-group"><label>Organization Name*</label><input id="ogf-name"/></div>
    <div class="form-group"><label>Position Title*</label><input id="ogf-pos" placeholder="e.g. Treasurer"/></div>
    <div class="form-group"><label>Signatory*</label><select id="ogf-sig">${sigs.map(s=>`<option value="${s.id}">${s.fullName}</option>`).join('')}</select></div>
    <div class="form-group"><label>Curriculum / Section*</label><select id="ogf-cur">${curricula.map(c=>`<option value="${c.id}">${c.courseCode} Year ${c.yearLevel} - ${c.section}</option>`).join('')}</select></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveOrg()">Save</button>
    </div>`;
}
async function saveOrg() {
  try {
    await api('POST', '/organizations', { orgName: document.getElementById('ogf-name').value, positionTitle: document.getElementById('ogf-pos').value, signatoryId: parseInt(document.getElementById('ogf-sig').value), curriculumId: parseInt(document.getElementById('ogf-cur').value) });
    toast('Organization saved.', 'success'); closeModal(); viewOrganizations();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deleteOrg(id) {
  if (!confirm('Delete?')) return;
  try { await api('DELETE', `/organizations/${id}`); toast('Deleted.', 'success'); viewOrganizations(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── SETUP: SIGNATORIES ───────────────────────────────────
async function viewSignatories() {
  loading();
  try {
    const sigs = await api('GET', '/signatories');
    setContent(`
      <div class="page-title">Signatories</div>
      <div class="card">
        <div class="card-header"><h2>All Signatories (${sigs.length})</h2>
          <button class="btn btn-primary btn-sm" onclick="showRegisterSignatoryModal()">+ Add Signatory</button>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>Employee ID</th><th>Full Name</th><th>Username</th><th>Signature</th><th>Active</th><th>Actions</th></tr></thead>
          <tbody>${sigs.map(s => `<tr>
            <td><code>${s.employeeId}</code></td><td>${s.fullName}</td><td>${s.username}</td>
            <td>${s.signaturePath ? `<img src="${API.replace('/api','')}${s.signaturePath}" style="height:32px;border:1px solid #eee"/>` : '—'}</td>
            <td>${s.isActive ? '✅' : '❌'}</td>
            <td>
              ${s.isActive ? `<button class="btn btn-warning btn-sm" onclick="deactivateSignatory(${s.id})">Deactivate</button>` : ''}
            </td>
          </tr>`).join('') || `<tr><td colspan="6">${emptyState()}</td></tr>`}</tbody></table>
        </div>
      </div>`);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function showRegisterSignatoryModal() {
  openModal('Register Signatory', `
    <div class="form-row">
      <div class="form-group"><label>First Name*</label><input id="sig-fname"/></div>
      <div class="form-group"><label>Last Name*</label><input id="sig-lname"/></div>
    </div>
    <div class="form-group"><label>Employee ID*</label><input id="sig-empid"/></div>
    <div class="form-row">
      <div class="form-group"><label>Username*</label><input id="sig-uname"/></div>
      <div class="form-group"><label>Password*</label><input type="password" id="sig-pwd"/></div>
    </div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveSignatory()">Register</button>
    </div>`);
}
async function saveSignatory() {
  try {
    await api('POST', '/auth/register/signatory', { firstName: document.getElementById('sig-fname').value, lastName: document.getElementById('sig-lname').value, employeeId: document.getElementById('sig-empid').value, username: document.getElementById('sig-uname').value, password: document.getElementById('sig-pwd').value });
    toast('Signatory registered.', 'success'); closeModal(); viewSignatories();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deactivateSignatory(id) {
  if (!confirm('Deactivate this signatory?')) return;
  try { await api('PUT', `/signatories/${id}/deactivate`); toast('Deactivated.', 'success'); viewSignatories(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── REPORTS ──────────────────────────────────────────────
async function viewReports() {
  loading();
  try {
    const periods = await api('GET', '/periods');
    const active = periods.find(p => p.isActive) || periods[0];
    const report = active ? await api('GET', `/reports/clearance?periodId=${active.id}`) : [];
    const cleared = report.filter(r => r.overallStatus === 'Cleared');
    const pending = report.filter(r => r.overallStatus !== 'Cleared');

    setContent(`
      <div class="page-title">Reports</div>
      <div class="stats-grid">
        <div class="stat-card blue"><div class="stat-icon">👥</div><div class="stat-value">${report.length}</div><div class="stat-label">Total Students with Clearance</div></div>
        <div class="stat-card green"><div class="stat-icon">✅</div><div class="stat-value">${cleared.length}</div><div class="stat-label">Fully Cleared</div></div>
        <div class="stat-card yellow"><div class="stat-icon">⏳</div><div class="stat-value">${pending.length}</div><div class="stat-label">Pending</div></div>
      </div>
      <div class="card">
        <div class="card-header">
          <h2>${active ? `${active.academicYear} — ${active.semester}` : 'No Active Period'}</h2>
          <div class="card-actions">
            <select id="rpt-filter" onchange="filterReport(this.value)">
              <option value="all">All Students</option>
              <option value="Cleared">Cleared Only</option>
              <option value="Pending">Pending Only</option>
            </select>
            ${active ? `<button class="btn btn-success btn-sm" onclick="exportExcel(${active.id})">📥 Export Excel</button>` : ''}
          </div>
        </div>
        <div class="table-wrap">
          <table><thead><tr><th>Student No.</th><th>Name</th><th>Course</th><th>Year</th><th>Section</th><th>Subjects</th><th>Orgs</th><th>Status</th></tr></thead>
          <tbody id="rpt-tbody">
            ${report.map(r => reportRow(r)).join('') || `<tr><td colspan="8">${emptyState()}</td></tr>`}
          </tbody></table>
        </div>
      </div>`);
    window._reportData = report;
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function reportRow(r) {
  return `<tr>
    <td>${r.studentNumber}</td><td>${r.fullName}</td><td>${r.course}</td>
    <td>${r.yearLevel}</td><td>${r.section}</td>
    <td>${r.clearedSubjects}/${r.totalSubjects}</td>
    <td>${r.clearedOrgs}/${r.totalOrgs}</td>
    <td>${statusBadge(r.overallStatus)}</td>
  </tr>`;
}
function filterReport(val) {
  const data = (window._reportData || []).filter(r => val === 'all' || r.overallStatus === val);
  document.getElementById('rpt-tbody').innerHTML = data.map(reportRow).join('') || `<tr><td colspan="8">${emptyState()}</td></tr>`;
}
async function exportExcel(periodId) {
  try {
    const res = await api('GET', `/reports/export?periodId=${periodId}`, null, true);
    const blob = await res.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = `clearance_report.xlsx`; a.click();
    toast('Export downloaded.', 'success');
  } catch(ex) { toast(ex.message, 'error'); }
}

// ─── ANNOUNCEMENTS ────────────────────────────────────────
async function viewAnnouncements() {
  loading();
  try {
    const anns = await api('GET', '/announcements');
    const canEdit = auth.role === 'admin' || auth.role === 'signatory';
    setContent(`
      <div class="page-title">Announcements</div>
      ${canEdit ? `<div style="margin-bottom:20px"><button class="btn btn-primary" onclick="openModal('New Announcement', annForm())">+ New Announcement</button></div>` : ''}
      ${anns.length ? anns.map(a => `
        <div class="ann-card">
          <div class="ann-meta">${statusBadge(a.type)}<strong>${a.title}</strong></div>
          <div class="ann-content">${a.content}</div>
          <div class="ann-footer">
            <span>By ${a.authorName}</span>
            <span>${fmtDt(a.createdAt)}</span>
            ${canEdit && auth.role === 'admin' ? `<button class="btn btn-danger btn-sm" onclick="deleteAnnouncement(${a.id})">🗑</button>` : ''}
          </div>
        </div>`).join('') : emptyState('No announcements yet.')}
    `);
  } catch(ex) { setContent(`<div class="alert alert-danger">${ex.message}</div>`); }
}
function annForm(a = {}) {
  return `<div class="form-group"><label>Title*</label><input id="af-title" value="${a.title||''}"/></div>
    <div class="form-group"><label>Type</label><select id="af-type">
      <option ${a.type==='General'?'selected':''}>General</option>
      <option ${a.type==='Urgent'?'selected':''}>Urgent</option>
      <option ${a.type==='Event'?'selected':''}>Event</option>
      <option ${a.type==='Reminder'?'selected':''}>Reminder</option>
    </select></div>
    <div class="form-group"><label>Content*</label><textarea id="af-content">${a.content||''}</textarea></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveAnnouncement()">Post</button>
    </div>`;
}
async function saveAnnouncement() {
  try {
    await api('POST', '/announcements', { title: document.getElementById('af-title').value, content: document.getElementById('af-content').value, type: document.getElementById('af-type').value });
    toast('Announcement posted.', 'success'); closeModal(); viewAnnouncements();
  } catch(ex) { toast(ex.message, 'error'); }
}
async function deleteAnnouncement(id) {
  if (!confirm('Delete this announcement?')) return;
  try { await api('DELETE', `/announcements/${id}`); toast('Deleted.', 'success'); viewAnnouncements(); }
  catch(ex) { toast(ex.message, 'error'); }
}

// ─── REGISTER STUDENT MODAL ───────────────────────────────
async function showRegisterStudentModal() {
  const curricula = await api('GET', '/curriculum');
  openModal('Register Student', `
    <div class="form-row">
      <div class="form-group"><label>First Name*</label><input id="rsf-fname"/></div>
      <div class="form-group"><label>Last Name*</label><input id="rsf-lname"/></div>
    </div>
    <div class="form-row">
      <div class="form-group"><label>Middle Initial</label><input id="rsf-mi" maxlength="3"/></div>
      <div class="form-group"><label>Suffix</label><input id="rsf-suffix"/></div>
    </div>
    <div class="form-row">
      <div class="form-group"><label>Student Number*</label><input id="rsf-snum"/></div>
      <div class="form-group"><label>Curriculum*</label><select id="rsf-cur">${curricula.map(c=>`<option value="${c.id}">${c.courseCode} Y${c.yearLevel}-${c.section}</option>`).join('')}</select></div>
    </div>
    <div class="form-group"><label>Status</label><select id="rsf-status"><option>Regular</option><option>Irregular</option></select></div>
    <div class="form-row">
      <div class="form-group"><label>Username*</label><input id="rsf-uname"/></div>
      <div class="form-group"><label>Password*</label><input type="password" id="rsf-pwd"/></div>
    </div>
    <div id="rsf-err" class="alert alert-danger hidden"></div>
    <div class="form-actions">
      <button class="btn btn-outline" onclick="closeModal()">Cancel</button>
      <button class="btn btn-primary" onclick="saveNewStudent()">Register</button>
    </div>`);
}
async function saveNewStudent() {
  const err = document.getElementById('rsf-err');
  err.classList.add('hidden');
  try {
    await api('POST', '/auth/register/student', {
      firstName: document.getElementById('rsf-fname').value,
      lastName:  document.getElementById('rsf-lname').value,
      middleInitial: document.getElementById('rsf-mi').value || null,
      suffixName: document.getElementById('rsf-suffix').value || null,
      studentNumber: document.getElementById('rsf-snum').value,
      curriculumId: parseInt(document.getElementById('rsf-cur').value),
      status: document.getElementById('rsf-status').value,
      username: document.getElementById('rsf-uname').value,
      password: document.getElementById('rsf-pwd').value,
    });
    toast('Student registered.', 'success'); closeModal(); viewStudentsList();
  } catch(ex) {
    err.textContent = ex.message; err.classList.remove('hidden');
  }
}

// ─── BOOT ─────────────────────────────────────────────────
if (auth) {
  initApp();
} else {
  showPage('page-login');
}
