# CROMS integrated flowchart + DFD + CLD generator -> .drawio (+ PIL preview)
import html, math, sys
from PIL import Image, ImageDraw, ImageFont

W, H = 5420, 5200
nodes, order, edges = {}, [], []

C = dict(flow='#1F4E79', data='#2E7D32', causal='#C62828')
LANEFILL = {
    'client': ('#EAF4FF', '#6C8EBF'), 'kiosk': ('#E9F7EF', '#67AB7E'),
    'queue': ('#FFF4D6', '#D6B656'), 'staff': ('#F2E9FA', '#9673A6'),
    'module': ('#FDECEA', '#C77B72'), 'data': ('#EEF1F4', '#7F8C97'),
}
NODEFILL = {  # lane -> fill for processes
    'client': '#DCEBFF', 'kiosk': '#D4F0DF', 'queue': '#FFE9A8', 'staff': '#E4D3F3',
    'module': '#FAD4D0', 'data': '#F3F5F7',
}


def N(id, kind, x, y, w, h, text, lane=None, fill=None, stroke=None, fs=11, bold=False, dash=False, fc='#111111'):
    nodes[id] = dict(id=id, kind=kind, x=x, y=y, w=w, h=h, text=text, lane=lane, fill=fill, stroke=stroke,
                     fs=fs, bold=bold, dash=dash, fc=fc)
    order.append(id)
    return id


def cx(i): n = nodes[i]; return n['x'] + n['w'] / 2
def cy(i): n = nodes[i]; return n['y'] + n['h'] / 2


def anchor(i, side, frac=0.5):
    n = nodes[i]
    x, y, w, h = n['x'], n['y'], n['w'], n['h']
    if isinstance(side, tuple):  # raw point
        px, py = side
        return px, py, (px - x) / w, (py - y) / h
    if side == 'r': return x + w, y + h * frac, 1, frac
    if side == 'l': return x, y + h * frac, 0, frac
    if side == 't': return x + w * frac, y, frac, 0
    if side == 'b': return x + w * frac, y + h, frac, 1
    raise ValueError(side)


def E(kind, s, ss, t, ts, via=None, label=None, sf=0.5, tf=0.5, both=False, lseg=0, lpos=None, w=None):
    sa = anchor(s, ss, sf); ta = anchor(t, ts, tf)
    pts = [(sa[0], sa[1])]
    if via:
        pts += via
    else:
        # auto elbow
        (x1, y1), (x2, y2) = (sa[0], sa[1]), (ta[0], ta[1])
        if abs(x1 - x2) > 1 and abs(y1 - y2) > 1:
            if ss in 'rl' and ts in 'rl':
                xm = (x1 + x2) / 2
                pts += [(xm, y1), (xm, y2)]
            elif ss in 'bt' and ts in 'bt':
                ym = (y1 + y2) / 2
                pts += [(x1, ym), (x2, ym)]
            elif ss in 'rl' and ts in 'bt':
                pts += [(x2, y1)]
            elif ss in 'bt' and ts in 'rl':
                pts += [(x1, y2)]
    pts.append((ta[0], ta[1]))
    edges.append(dict(kind=kind, s=s, t=t, sa=sa, ta=ta, pts=pts, label=label, both=both, lseg=lseg, lpos=lpos, w=w))


def F(*a, **k): E('flow', *a, **k)
def D(*a, **k): E('data', *a, **k)
def K(*a, **k): E('causal', *a, **k)


# ---------------------------------------------------------------- geometry
LEFT, RIGHT = 40, 5380
LANES = [  # key, title, y0, y1
    ('client', 'LANE 1  CLIENT', 755, 935),
    ('kiosk', 'LANE 2  CROMS KIOSK', 935, 1520),
    ('queue', 'LANE 3  QUEUE MANAGEMENT / CROMS DISPLAY', 1520, 1970),
    ('staff', 'LANE 4  LCRO STAFF / CROMS MAIN', 1970, 2820),
    ('module', 'LANE 5  SERVICE MODULES (CROMS MAIN)', 2820, 4240),
    ('data', 'LANE 6  DATABASE / DFD DATA STORES', 4240, 4600),
]
for k, t, y0, y1 in LANES:
    f, s = LANEFILL[k]
    N('lane_' + k, 'lane', LEFT, y0, RIGHT - LEFT, y1 - y0, t, fill=f, stroke=s)

P = 165
def X(i): return 120 + P * i
NW, NH = 150, 60

# ---------------------------------------------------------------- title + legend
N('title', 'text', 60, 12, 5300, 60, '<b>CROMS — INTEGRATED SYSTEM MODEL</b>', fs=30, bold=True)
N('subtitle', 'text', 60, 62, 5300, 30,
  'System Flowchart  +  Data Flow Diagram (DFD)  +  Causal Loop Diagram (CLD) — Civil Registry Operations Management System, LCRO Peñablanca',
  fs=15)
N('legbox', 'lane', 60, 100, 5300, 150, 'LEGEND', fill='#FAFAFA', stroke='#888888')
lx = 90; ly = 145
N('lg_oval', 'oval', lx, ly, 90, 40, 'Start / End', lane='staff', fs=10)
N('lg_proc', 'proc', lx + 120, ly, 100, 40, 'Process', lane='staff', fs=10)
N('lg_dec', 'dec', lx + 250, ly - 5, 110, 50, 'Decision', lane='staff', fs=10)
N('lg_io', 'io', lx + 390, ly, 110, 40, 'Input / Output', lane='staff', fs=10)
N('lg_store', 'store', lx + 530, ly - 5, 100, 55, 'D# Data Store', lane='data', fs=10)
N('lg_conn', 'conn', lx + 660, ly + 4, 34, 34, 'R', lane='staff', fs=10)
N('lg_conn_t', 'text', lx + 698, ly + 8, 130, 30, 'Off-page connector', fs=10)
N('lg_tag', 'tag', lx + 830, ly + 6, 70, 26, 'B1 ▸ CLD tag', fs=9)
N('lg_fa', 'text', lx + 920, ly - 2, 10, 10, '', fs=8)
N('lg_a1s', 'pt', lx + 940, ly + 20, 2, 2, ''); N('lg_a1e', 'pt', lx + 1040, ly + 20, 2, 2, '')
N('lg_a1t', 'text', lx + 1050, ly + 6, 260, 30, 'Solid blue arrow = operational flow (flowchart)', fs=10)
N('lg_a2s', 'pt', lx + 1330, ly + 20, 2, 2, ''); N('lg_a2e', 'pt', lx + 1430, ly + 20, 2, 2, '')
N('lg_a2t', 'text', lx + 1440, ly + 6, 300, 30, 'Dashed green arrow = data movement (DFD)', fs=10)
N('lg_a3s', 'pt', lx + 1770, ly + 20, 2, 2, ''); N('lg_a3e', 'pt', lx + 1870, ly + 20, 2, 2, '')
N('lg_a3t', 'text', lx + 1880, ly + 6, 330, 30, 'Dashed red arrow = causal link (CLD), with (+) / (−)', fs=10)
N('lg_txt', 'text', lx, ly + 55, 5100, 40,
  '<b>R</b> = Reinforcing loop   <b>B</b> = Balancing loop   (+) same direction   (−) opposite direction   |   '
  'Circled letter = off-page connector (R = task returns to Client Tasks check, K1 = window config feeds kiosk, L-K / L-D = launcher starts Kiosk / Display, Q = forwarded task returns to queue)   |   '
  'D3, D2, D9, D10 appear more than once beside the processes that use them (same store)', fs=10)
edges_leg = [('lg_a1s', 'lg_a1e', 'flow'), ('lg_a2s', 'lg_a2e', 'data'), ('lg_a3s', 'lg_a3e', 'causal')]


# ---------------------------------------------------------------- SECTION A/B: startup (staff lane) + window config (queue lane)
S1 = 2040
def SN(id, kind, i, y, text, lane='staff', w=NW, h=NH, **k):
    return N(id, kind, X(i), y - h / 2, w, h, text, lane=lane, **k)

N('a_start', 'oval', X(0), S1 - 25, 110, 50, 'START', lane='staff', fs=12, bold=True)
SN('a_launch', 'proc', 1, S1, 'Launch CROMS Launcher')
SN('a_opts', 'io', 2, S1, 'Display options: Main / Kiosk / Display / Run All')
SN('a_runall', 'dec', 3, S1, 'Run All?', h=76)
SN('a_launchall', 'proc', 4, S1, 'YES: Launch Main + Kiosk + Display (NO: selected app only)')
SN('a_login', 'io', 5, S1, 'CROMS Main Login: staff/admin enters username + password')
SN('a_valid', 'dec', 6, S1, 'Credentials valid?', h=76)
SN('a_role', 'proc', 7, S1, 'Retrieve user account and role')
SN('a_audit', 'proc', 8, S1, 'Record login activity in Audit Trail')
SN('a_dash', 'proc', 9, S1, 'Open CROMS Main Dashboard')
SN('a_err', 'io', 6, S1 + 125, 'NO: Display login error; return to Login', h=52)
for a, b in [('a_start', 'a_launch'), ('a_launch', 'a_opts'), ('a_opts', 'a_runall'), ('a_runall', 'a_launchall'),
             ('a_launchall', 'a_login'), ('a_login', 'a_valid'), ('a_valid', 'a_role'), ('a_role', 'a_audit'),
             ('a_audit', 'a_dash')]:
    F(a, 'r', b, 'l', label='YES' if (a, b) in [('a_runall', 'a_launchall'), ('a_valid', 'a_role')] else None)
F('a_valid', 'b', 'a_err', 't', label='NO')
F('a_err', 'l', 'a_login', 'b', via=[(X(5) + 75, S1 + 125)], lseg=0)
# DFD stores beside login
N('d1', 'store', X(7) + 10, S1 + 85, 130, 66, 'D1 USERS / AUTHENTICATION', lane='data', fs=9)
N('d10_a', 'store', X(8) + 10, S1 + 85, 130, 66, 'D10 AUDIT LOGS', lane='data', fs=9)
D('a_login', 'b', 'd1', 'l', via=[(X(5) + 75, S1 + 118), (X(7) - 8, S1 + 118)], both=True, label='login credentials ↔ accounts, roles', lpos=0.35)
D('a_role', 'b', 'd1', 't', both=True)
D('a_audit', 'b', 'd10_a', 't')

# connectors from launcher
N('c_LK', 'conn', X(4) + 58, S1 - 92, 34, 34, 'L-K', lane='staff', fs=8)
N('c_LD', 'conn', X(4) + 108, S1 - 92, 34, 34, 'L-D', lane='staff', fs=8)
F('a_launchall', 't', 'c_LK', 'b', via=[(X(4) + 75, S1 - 40), (X(4) + 75, S1 - 40), (X(4) + 75, S1 - 40)])
F('a_launchall', 't', 'c_LD', 'b', via=[(X(4) + 75, S1 - 40), (X(4) + 125, S1 - 40)])

# Section B: window configuration chain in QUEUE lane, right-flowing starting above dashboard
Q1 = 1610; Q2 = 1750
def QN(id, kind, x, y, text, w=NW, h=NH, **k):
    return N(id, kind, x, y - h / 2, w, h, text, lane='queue', **k)
bx = [X(9) + 0] + [X(9) + P * i for i in range(1, 9)]  # start above dashboard x
QN('b1', 'proc', X(9), Q1, 'Open Queue / Window Management')
QN('b2', 'proc', X(10), Q1, 'Select Window')
QN('b3', 'proc', X(11), Q1, 'Configure transactions allowed at window')
QN('b4', 'dec', X(12), Q1, 'Allow all transactions?', h=76)
QN('b5', 'proc', X(13), Q1, 'YES: Enable all supported transactions')
QN('b6', 'proc', X(12), Q2, 'NO: Select specific transactions (Birth, Marr. App/Reg, Death, CTC, Release, Petition, BREQS, OCR)', h=72)
QN('b7', 'proc', X(14), Q1, 'Activate window')
QN('b8', 'proc', X(15), Q1, 'Save window configuration')
N('d9', 'store', X(15) + 10, 1830, 130, 66, 'D9 SYSTEM SETTINGS', lane='data', fs=9)
F('a_dash', 't', 'b1', 'b', via=[(cx('a_dash'), 1720), (cx('b1'), 1720)] if False else None)
F('b1', 'r', 'b2', 'l'); F('b2', 'r', 'b3', 'l'); F('b3', 'r', 'b4', 'l')
F('b4', 'r', 'b5', 'l', label='YES'); F('b4', 'b', 'b6', 't', label='NO')
F('b5', 'r', 'b7', 'l', via=[(X(13) + 165, Q1 - 0)] if False else None)
F('b6', 'r', 'b7', 'b', via=[(cx('b7'), Q2)])
F('b7', 'r', 'b8', 'l')
D('b8', 'b', 'd9', 't', both=True, label='window configuration', lpos=0.55)

# ---------------------------------------------------------------- SECTION C/D: client + kiosk chain
CL = 830; KA = 1000; KB = 1140
def CN(id, kind, i, text, **k): return N(id, kind, X(i), CL - 30, NW, NH, text, lane='client', **k)
def KN(id, kind, i, y, text, w=NW, h=NH, **k): return N(id, kind, X(i), y - h / 2, w, h, text, lane='kiosk', **k)

N('cl1', 'oval', X(0), CL - 30, NW, NH, 'CLIENT approaches CROMS Kiosk', lane='client', fs=10)
KN('ka0', 'proc', 0, KA, 'Kiosk Landing Page')
KN('ka1', 'proc', 1, KA, 'Check system / kiosk availability')
KN('ka2', 'dec', 2, KA, 'Kiosk open?', h=76)
KN('kb2', 'io', 2, KB, 'NO: "Kiosk Currently Unavailable"; client waits or returns later', h=64)
KN('ka3', 'proc', 3, KA, 'YES: Display "Kiosk is Open" + START button')
CN('cl2', 'io', 4, 'Client taps START')
KN('ka5', 'proc', 5, KA, 'Display available transactions (enabled per window config)', h=70)
CN('cl3', 'io', 6, 'Client selects one or multiple transactions')
KN('ka7', 'dec', 7, KA, 'Multiple transactions?', h=76)
KN('ka8', 'proc', 8, KA, 'YES: Save all selected under ONE Client Transaction; create Client Task per service', h=76)
KN('kb7', 'proc', 7, KB, 'NO: Create ONE Client Task')
CN('cl4', 'io', 9, 'Client provides requester information: identity, contact, valid ID, supporting info')
KN('ka10', 'dec', 10, KA, 'Required info complete?', h=76)
KN('kb10', 'proc', 10, KB, 'NO: Highlight missing info; return to client input', h=64)
KN('ka11', 'dec', 11, KA, 'Service needs extra info?', h=76)

F('cl1', 'b', 'ka0', 't')
F('ka0', 'r', 'ka1', 'l'); F('ka1', 'r', 'ka2', 'l')
F('ka2', 'b', 'kb2', 't', label='NO'); F('ka2', 'r', 'ka3', 'l', label='YES')
F('ka3', 'r', 'cl2', 'b', via=[(cx('cl2'), KA)])
F('cl2', 'r', 'ka5', 't', via=[(cx('ka5'), CL)])
F('ka5', 'r', 'cl3', 'b', via=[(cx('cl3'), KA)])
F('cl3', 'r', 'ka7', 't', via=[(cx('ka7'), CL)])
F('ka7', 'r', 'ka8', 'l', label='YES'); F('ka7', 'b', 'kb7', 't', label='NO')
F('ka8', 'r', 'cl4', 'b', via=[(X(9) + 45, KA)], tf=0.3)
F('kb7', 'r', 'cl4', 'b', via=[(X(9) + 45, KB)], tf=0.3)
F('cl4', 'r', 'ka10', 't', via=[(cx('ka10'), CL)])
F('ka10', 'b', 'kb10', 't', label='NO')
F('kb10', 'l', 'cl4', 'b', via=[(X(9) + 120, KB)], tf=0.8)
F('ka10', 'r', 'ka11', 'l', label='YES')
# D2 / D3 near kiosk chain saves
# Client Task / transaction saved into D3 at Kiosk (per DFD)
N('d3_k1', 'store', X(8) - 5, KB + 60, 140, 60, 'D3 QUEUE & TRANSACTIONS', lane='data', fs=9)
D('ka8', 'b', 'd3_k1', 't', via=[(X(8) + 45, KA + 38), (X(8) + 45, KB + 30)] if False else None, label='selected services', lpos=0.5)

# ---------------------------------------------------------------- SECTION E: kiosk service-specific inputs container (2x4)
EX0, EY0 = 2330, 962
EW, EH = 1240, 540
N('econtainer', 'lane', EX0, EY0, EW, EH, 'E. SERVICE-SPECIFIC KIOSK INPUTS — only the blocks for the selected services appear', fill='#F4FBF7', stroke='#67AB7E')
nodes['econtainer']['dash'] = True
ESPEC = [
    ('E1', 'BIRTH REGISTRATION', ['Select Birth Registration', 'Collect birth registration info', 'Collect requester / registrant info',
                                   'Record supporting docs / requirements', 'Validate required fields', 'Add to Client Tasks', 'Submit to Queue']),
    ('E2', 'MARRIAGE APPLICATION', ['Select Marriage Application', 'Collect applicant / spouse info', 'Record Sex + personal info',
                                    'Record marriage application details', 'Record parental consent / advice info',
                                    'Capture supporting information', 'Validate required fields', 'Add to Client Tasks', 'Submit to Queue']),
    ('E3', 'MARRIAGE REGISTRATION', ['Select Marriage Registration', 'Collect / reference marriage info',
                                     'Create pending Marriage Registration task', 'Submit to Queue',
                                     '(Form 97 capture + OCR happen in CROMS Main)']),
    ('E4', 'DEATH REGISTRATION', ['Select Death Registration', 'Collect death registration / requester info',
                                  'Record supporting requirements', 'Validate required fields', 'Add to Client Tasks', 'Submit to Queue']),
    ('E5', 'CERTIFICATE REQUEST / CPC', ['Select Certificate Request / CPC', 'Select requested civil registry document',
                                         'Enter subject / record info', 'Enter copies / purpose (if applicable)',
                                         'Enter requester information', 'Validate information', 'Add to Client Tasks', 'Submit to Queue']),
    ('E6', 'RELEASE & CLAIM', ['Select Release & Claim', 'Enter PREVIOUS queue no. / txn reference',
                               'Retrieve existing request — found?', 'NO: invalid notice; re-enter reference',
                               'YES: show request; verify claimant', 'Add Release & Claim task', 'Submit to Queue',
                               'Previous Queue No. = Release & Claim ONLY']),
    ('E7', 'PETITION / CASE', ['Select Petition / Case', 'Collect requester / petitioner info', 'Identify petition / case type',
                               'Record reference info + requirements', 'Add to Client Tasks', 'Submit to Queue']),
    ('E8', 'PSA BREQS', ['Select PSA BREQS', 'Select document type', 'Enter number of copies + purpose',
                         'Enter relationship to document owner', 'Enter valid ID information', 'Enter subject name',
                         'Enter date / place information', 'Validate', 'Add to Client Tasks', 'Submit to Queue']),
]
BW, BH = 288, 230
for n, (code, title, steps) in enumerate(ESPEC):
    col, row = n % 4, n // 4
    bx_, by_ = EX0 + 18 + col * (BW + 12), EY0 + 38 + row * (BH + 12)
    txt = '<b>%s  %s</b><br>' % (code, title) + '<br>'.join('%d. %s' % (i + 1, s) if not s.startswith(('NO:', 'YES:', '(', 'Previous')) else s
                                                            for i, s in enumerate(steps))
    N('e_' + code, 'ebox', bx_, by_, BW, BH, txt, lane='kiosk', fs=9)

# YES -> container, NO -> review (over the top)
F('ka11', 'r', 'econtainer', 'l', via=[(EX0 - 60, KA), (EX0 - 60, 1232)], label='YES')

# review chain (kiosk lane) after container
RV = 1232
def RN(id, kind, x, text, w=NW, h=NH, **k): return N(id, kind, x, RV - h / 2, w, h, text, lane='kiosk', **k)
RN('rv0', 'proc', 3640, 'Review client info + selected transactions')
RN('rv1', 'dec', 3805, 'Client confirms?', h=76)
RN('rv2', 'proc', 3970, 'YES: Submit')
RN('rv3', 'proc', 4135, 'Create Client Transaction; generate Queue Number')
RN('rv4', 'proc', 4300, 'Save client information')
RN('rv5', 'proc', 4465, 'Save selected Client Tasks')
RN('rv6', 'proc', 4630, 'Send Queue Entry to Queue Management')
N('rvno', 'io', 3760, 1010, 240, 60, 'NO: Edit information (return to client input) OR Cancel transaction', lane='kiosk', fs=9)
F('econtainer', 'r', 'rv0', 'l')
F('ka11', 't', 'rv0', 't', via=[(cx('ka11'), 950), (cx('rv0'), 950)], label='NO', lpos=0.05)
F('rv0', 'r', 'rv1', 'l'); F('rv1', 'r', 'rv2', 'l'); F('rv2', 'r', 'rv3', 'l'); F('rv3', 'r', 'rv4', 'l')
F('rv4', 'r', 'rv5', 'l'); F('rv5', 'r', 'rv6', 'l')
F('rv1', 't', 'rvno', 'b', label='NO')
N('d2_k', 'store', 4300 + 10, 1360, 130, 66, 'D2 CLIENT RECORDS', lane='data', fs=9)
N('d3_k', 'store', 4465 + 10, 1360, 130, 66, 'D3 QUEUE & TRANSACTIONS', lane='data', fs=9)
D('rv4', 'b', 'd2_k', 't', label='client info')
D('rv5', 'b', 'd3_k', 't', label='tasks, queue no.')

# ---------------------------------------------------------------- SECTION F: queue management + display (queue lane, leftward from x=4630)
FX = lambda i: 4630 - P * i
QN('f1', 'proc', FX(0), Q1, 'Queue Management receives transaction')
QN('f2', 'proc', FX(1), Q1, 'Save Queue No. + Client Tasks')
QN('f3', 'proc', FX(2), Q1, 'Determine appropriate service / window')
QN('f4', 'proc', FX(3), Q1, 'Place client in WAITING queue')
QN('f5', 'proc', FX(3), Q2, 'Staff views Waiting Queue')
QN('f6', 'proc', FX(4), Q2, 'Staff selects / calls next client')
QN('f7', 'proc', FX(5), Q2, 'Update status = CALLED / SERVING')
N('disp', 'proc', 5060, Q1 - 32, 250, 64, 'CROMS DISPLAY: shows Queue No. + Window No.', lane='queue', fs=10, bold=True)
N('audio', 'io', 5060, Q2 - 10, 250, 60, 'Audio: "Please proceed to Window ___"', lane='queue', fs=10)
N('cl6', 'io', 5105, CL - 30, 200, 60, 'Client proceeds to assigned window when called', lane='client', fs=10)
N('d3_q', 'store', 4465 + 10 - 8, 1830, 130, 66, 'D3 QUEUE & TRANSACTIONS', lane='data', fs=9)
N('d10_q', 'store', 3560, 1830, 130, 66, 'D10 AUDIT LOGS', lane='data', fs=9)
F('rv6', 'b', 'f1', 't')
F('f1', 'l', 'f2', 'r'); F('f2', 'l', 'f3', 'r'); F('f3', 'l', 'f4', 'r')
F('f4', 'b', 'f5', 't')
F('f5', 'l', 'f6', 'r'); F('f6', 'l', 'f7', 'r')
D('f1', 'r', 'disp', 'l', label='active queue info', lpos=0.5)
D('f7', 'b', 'disp', 'b', via=[(cx('f7'), 1810), (5185, 1810)], label='status, queue no. + window', lpos=0.8) if False else None
F('disp', 'b', 'audio', 't')
F('audio', 't', 'cl6', 'b', via=[(5185, 1670)] if False else None) if False else None
F('audio', 'r', 'cl6', 'b', via=[(5330, Q2 - 10 + 0)] if False else [(5340, Q2), (5340, CL + 110), (5205, CL + 110)])
D('f2', 'b', 'd3_q', 't', both=True, via=[(cx('f2'), 1790), (cx('d3_q'), 1790)], label='queue no., tasks', lpos=0.6)
D('f7', ('b') if False else 'b', 'd10_q', 't', via=[(3822, 1790), (cx('d10_q'), 1790)], sf=0.1, label='status change', lpos=0.6)
D('d9', 'r', 'f3', 'b', via=[(cx('d9') + 65, 1870), (4250, 1870)] if False else [(cx('d9') + 65 + 8, 1812), (cx('f3'), 1812)], label='enabled windows / services', lpos=0.2)
# window config feeds kiosk (off-page connector K1)
N('c_K1s', 'conn', X(15) + 50, Q1 - 82, 34, 34, 'K1', lane='queue', fs=8)
F('b8', 't', 'c_K1s', 'b')
N('c_K1t', 'conn', X(5) + 58, KB - 17, 34, 34, 'K1', lane='kiosk', fs=8)
F('c_K1t', 't', 'ka5', 'b', label='available services')
N('c_LKt', 'conn', X(0) + 58, KB + 5, 34, 34, 'L-K', lane='kiosk', fs=8)
F('c_LKt', 't', 'ka0', 'b')
N('c_LDt', 'conn', 5168, Q1 - 82, 34, 34, 'L-D', lane='queue', fs=8)
F('c_LDt', 'b', 'disp', 't')
F('c_LDt', 'r', 'disp', 'l', via=[(5052, Q1)]) if False else None

# ---------------------------------------------------------------- SECTION G/H: staff client tasks panel + common verification (leftward)
S2 = 2260
def SN2(id, kind, x, y, text, w=NW, h=NH, **k): return N(id, kind, x, y - h / 2, w, h, text, lane='staff', **k)
SN2('g1', 'proc', 3805, S2, 'Staff opens client: retrieve Client Transaction + all Client Tasks', h=76)
SN2('g2', 'proc', 3640, S2, 'Display Client Tasks Panel (current first, completed below)', h=76)
SN2('g3', 'proc', 3475, S2, 'Staff selects PROCESS on current task; Complete All locked while tasks pending', h=88)
SN2('h1', 'proc', 3310, S2, 'View client info; verify client identity')
SN2('h2', 'proc', 3145, S2, 'Check requirements + supporting documents')
SN2('h3', 'dec', 2980, S2, 'Requirements complete?', h=80)
SN2('h_no', 'io', 2955, 2400, 'NO: record missing requirement; Status = PENDING; save reason / remarks; return / forward per office procedure', w=200, h=76, fs=9)
SN2('disp_t', 'dec', 2745 - 40, S2, 'Task type?', w=180, h=88)
N('tag_g', 'text', 0, 0, 0, 0, '')
F('f7', 'b', 'g1', 't')
F('g1', 'l', 'g2', 'r'); F('g2', 'l', 'g3', 'r'); F('g3', 'l', 'h1', 'r'); F('h1', 'l', 'h2', 'r'); F('h2', 'l', 'h3', 'r')
F('h3', 'l', 'disp_t', 'r', label='YES')
F('h3', 'b', 'h_no', 't', label='NO', via=[(cx('h3'), 2350), (cx('h_no'), 2350)])
N('d3_g', 'store', 3805 + 10, 2380, 130, 66, 'D3 QUEUE & TRANSACTIONS', lane='data', fs=9)
N('d2_g', 'store', 3310 + 10, 2380, 130, 66, 'D2 CLIENT RECORDS', lane='data', fs=9)
N('d10_g', 'store', 3475 + 40, 2380, 130, 66, 'D10 AUDIT LOGS', lane='data', fs=9)
D('g1', 'b', 'd3_g', 't', both=True, label='tasks', lpos=0.5)
D('h1', 'b', 'd2_g', 't', label='client info')
D('h2', 'b', 'd10_g', 't', via=[(cx('h2'), 2345), (cx('d10_g'), 2345)], label='verification actions', lpos=0.4)
D('h_no', 'b', 'd10_g', 'b', via=[(cx('h_no'), 2500), (cx('d10_g'), 2500)], label='pending status + reason', lpos=0.5)

# ---------------------------------------------------------------- SECTION R/S: completion (staff lane right)
R2Y = 2260; S3Y = 2570
def RN2(id, kind, i, y, text, w=NW, h=NH, **k): return N(id, kind, 4080 + P * i, y - h / 2, w, h, text, lane='staff', **k)
N('c_R', 'conn', 4024, S2 - 17, 34, 34, 'R', lane='staff', fs=9)
RN2('r1', 'dec', 0, R2Y, 'Other pending tasks?', h=80)
RN2('r2', 'proc', 1, R2Y, 'YES: select next pending Client Task')
RN2('r3', 'dec', 2, R2Y, 'Can current window process it?', h=88)
RN2('r4', 'proc', 3, R2Y, 'YES: process next task (back to Task type)')
RN2('r5', 'proc', 2, 2420, 'NO: forward Client Task to appropriate window; update queue / window assignment', h=84, fs=10)
RN2('r6', 'proc', 3, 2420, 'Client proceeds when called; new staff opens next task', h=76)
N('c_Q', 'conn', 4080 + P * 3 + 58, 2500, 34, 34, 'Q', lane='staff', fs=9)
F('c_R', 'r', 'r1', 'l')
F('r1', 'r', 'r2', 'l', label='YES'); F('r2', 'r', 'r3', 'l'); F('r3', 'r', 'r4', 'l', label='YES')
F('r3', 'b', 'r5', 't', label='NO'); F('r5', 'r', 'r6', 'l')
F('r6', 'b', 'c_Q', 't')
F('r4', 't', 'disp_t', 't', via=[(cx('r4'), 2130), (cx('disp_t'), 2130)])
N('c_Qt', 'conn', FX(3) + 168, Q2 - 17, 34, 34, 'Q', lane='queue', fs=9)
F('c_Qt', 'l', 'f5', 'r')
# S: completion chain
SN3 = lambda id, i, text, kind='proc', w=NW, h=NH, **k: N(id, kind, 4080 + P * i, S3Y - h / 2, w, h, text, lane='staff', **k)
SN3('s1', 0, 'NO pending tasks: enable COMPLETE ALL', h=64)
SN3('s2', 1, 'Complete Client Transaction: Overall Transaction = COMPLETED', h=76)
SN3('s3', 2, 'Queue = COMPLETED')
SN3('s4', 3, 'Save transaction history, staff actions + audit logs', h=76)
SN3('s5', 4, 'Update Dashboard')
SN3('s6', 5, 'Update Reports & Analytics', h=64)
N('s_end', 'oval', 4080 + P * 6 + 10, S3Y - 25, 90, 50, 'END', lane='staff', fs=12, bold=True)
F('r1', 'b', 's1', 't', label='NO')
for a, b in [('s1', 's2'), ('s2', 's3'), ('s3', 's4'), ('s4', 's5'), ('s5', 's6'), ('s6', 's_end')]:
    F(a, 'r', b, 'l')
N('d3_s', 'store', 4080 + P * 3 - 45, 2680, 130, 66, 'D3 QUEUE & TRANSACTIONS', lane='data', fs=9)
N('d10_s', 'store', 4080 + P * 3 + 110, 2680, 130, 66, 'D10 AUDIT LOGS', lane='data', fs=9)
D('s4', 'b', 'd3_s', 't', via=[(cx('s4') - 20, 2640)], label='final status', lpos=0.3) if False else D('s4', 'b', 'd3_s', 't')
D('s4', 'b', 'd10_s', 't', via=[(cx('s4') + 30, 2645), (cx('d10_s'), 2645)])

# ---------------------------------------------------------------- LANE 5: service modules
COLS = [110, 630, 1150, 2210, 2730, 3250, 3770, 4290, 4810]
CW = [500, 500, 1040, 500, 500, 500, 500, 500, 500]
HEADY = 2860
NY0 = 2945
PITCH, NORM_H, DEC_H = 72, 46, 64
RAILBASE = 440
mod_rails = {}
STOREY = 4290
D10Y = 4440


def module(ci, code, title, steps, data, audit_idx, stores, sidew=170):
    x0, cw = COLS[ci], CW[ci]
    mw = 240
    mx = x0 + 10
    hid = 'm%d_head' % ci
    N(hid, 'head', x0 + 5, HEADY, cw - 10, 46, '<b>%s</b><br>%s' % (code, title), lane='module', fs=10)
    # fan-out from dispatcher
    F('disp_t', 'b', hid, 't', via=[(cx('disp_t'), 2838), (x0 + cw / 2, 2838)], label='%s' % title.split('/')[0][:24] if ci == 4 else None, lpos=0.5) if False else \
        F('disp_t', 'b', hid, 't', via=[(cx('disp_t'), 2838), (x0 + cw / 2, 2838)])
    cur = NY0
    ids = []
    for si, st in enumerate(steps):
        kind = st.get('k', 'proc')
        h = DEC_H if kind == 'dec' else st.get('h', NORM_H)
        xx = mx + st.get('dx', 0)
        yy = st.get('y', cur)
        nid = 'm%d_%d' % (ci, si)
        N(nid, kind, xx, yy, mw, h, st['t'], lane='module', fs=9)
        ids.append(nid)
        if 'y' not in st:
            cur = yy + h + (26 if kind != 'dec' else 22)
    # flow between steps
    for si in range(len(steps) - 1):
        a, b = ids[si], ids[si + 1]
        st = steps[si]
        if st.get('skip'):
            continue
        F(a, 'b', b, 't', label=st.get('mainlabel'))
    N(hid + '_x', 'text', 0, 0, 0, 0, '')
    return ids


def side(ci, ids, si, text, kind='proc', label='YES', rejoin=True, h=52, w=None, tail=None):
    x0 = COLS[ci]
    a = ids[si]
    sid = a + '_s'
    n = nodes[a]
    sx = x0 + 10 + 240 + 24
    ww = w or 155
    N(sid, kind, sx, cy(a) - h / 2, ww, h, text, lane='module', fs=9)
    F(a, 'r', sid, 'l', label=label)
    if rejoin and si + 1 < len(ids):
        nxt = ids[si + 1]
        F(sid, 'b', nxt, 't', via=[(cx(sid), nodes[nxt]['y'] - 12), (cx(nxt), nodes[nxt]['y'] - 12)])
    return sid


def add_data(ci, ids, data, audit_idx, stores):
    x0, cw = COLS[ci], CW[ci]
    sx = {}
    for k, (key, label) in enumerate(stores):
        sid = 'st%d_%s' % (ci, key)
        N(sid, 'store', x0 + 10 + k * 145, STOREY, 130, 66, label, lane='data', fs=9)
        sx[key] = sid
    rails = []
    for (si, key, dirn) in data:
        rails.append((si, key, dirn))
    rails.sort(key=lambda r: r[0])
    n = len(rails)
    for ri, (si, key, dirn) in enumerate(rails):
        rx = x0 + cw - 66 + ri * 0  # placeholder
    # rail x: earlier node -> larger x
    railx = {}
    span = min(9, 52 // max(1, n))
    for ri, (si, key, dirn) in enumerate(rails):
        railx[(si, key)] = x0 + cw - 60 - ri * span
    for ri, (si, key, dirn) in enumerate(rails):
        a = ids[si]
        s = sx[key]
        rx = railx[(si, key)]
        jog = 4246 + (n - 1 - ri) * 5
        pts = [(rx, cy(a)), (rx, jog), (cx(s), jog)]
        D(a, 'r', s, 't', via=pts, both=(dirn == 'b'))
    # audit
    aid = ids[audit_idx]
    ax = x0 + cw - 14
    N('m%d_aud' % ci, 'text', 0, 0, 0, 0, '')
    D(aid, 'r', 'd10_bar', ('t' if False else (ax, D10Y)) if False else (ax, D10Y), via=[(ax, cy(aid))]) if False else None
    D(aid, 'r', 'd10_bar', (ax, D10Y), via=[(ax, cy(aid))])
    # return connector
    cid = 'm%d_R' % ci
    lastid = ids[-1]
    N(cid, 'conn', cx(lastid) - 17, nodes[lastid]['y'] + nodes[lastid]['h'] + 12, 34, 34, 'R', lane='module', fs=9)
    F(lastid, 'b', cid, 't')


N('d10_bar', 'store', 110, D10Y, 5200, 66, 'D10 AUDIT LOGS — login activity, staff actions, status changes, forwarding, corrections, releases, administrative changes', lane='data', fs=10)

# ---- module specs
def S(t, k='proc', **kw): d = dict(t=t, k=k); d.update(kw); return d

birth = [S('Open Birth Registration module'), S('Retrieve kiosk / client information', 'io'), S('Staff verifies Birth Registration requirements'),
         S('Enter / verify birth record information'), S('Attach / scan / capture supporting documents (OCR when digitizing)', 'io', h=52),
         S('Staff reviews extracted OCR information'), S('Extracted information correct?', 'dec'), S('Validate birth record'),
         S('Save Birth Registration; update registry record'), S('Update Client Task = COMPLETED'), S('Record audit activity')]
ids0 = module(0, 'I. BIRTH REGISTRATION', 'Main system', birth, None, 10, None)
side(0, ids0, 6, 'NO: staff corrects / validates information')
add_data(0, ids0, [(4, 'd8', 'b'), (8, 'd4', 'b'), (9, 'd3', 'w')], 10,
         [('d4', 'D4 CIVIL REGISTRY RECORDS'), ('d8', 'D8 DOCUMENTS / OCR'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

marrapp = [S('Open Marriage Application module'), S('Retrieve applicants\' information', 'io'), S('Verify applicant details + requirements (documents ↔ D8)', h=52),
           S('Check age-related requirements'), S('Applicant age 18–20?', 'dec'), S('Applicant age 21–25?', 'dec'),
           S('Verify all required application information'), S('Save Marriage Application'), S('Begin / record posting stage; track posting period', h=52),
           S('After requirements satisfied: proceed to Marriage License processing', h=52), S('Record Marriage License issuance + validity / tracking'),
           S('Update Marriage Application status'), S('Update Client Task'), S('Save history / audit activity')]
ids1 = module(1, 'J. MARRIAGE APPLICATION', 'Main system', marrapp, None, 13, None)
side(1, ids1, 4, 'YES: require Parental Consent details')
side(1, ids1, 5, 'YES: require Parental Advice details')
add_data(1, ids1, [(2, 'd8', 'b'), (7, 'd4', 'b'), (12, 'd3', 'w')], 13,
         [('d4', 'D4 CIVIL REGISTRY RECORDS'), ('d8', 'D8 DOCUMENTS / OCR'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

# Marriage registration (two sub-columns)
mr = [S('Marriage Registration task; Status = PENDING'), S('Receive Certificate of Marriage / Form 97'),
      S('Capture / attach scanned image: which method?', 'dec')]
ids2 = module(2, 'K. MARRIAGE REGISTRATION / FORM 97', 'Main system', mr, None, 2, None)
x0 = COLS[2]
# extend with custom rows
rows_y = [nodes[ids2[-1]]['y'] + DEC_H + 22]
def mrow(i): return NY0 + i * PITCH
# col A nodes (x0+10), col B nodes (x0+290)
A = lambda idn, i, t, k='proc', h=NORM_H: N(idn, k, x0 + 10, mrow(i), 240, h, t, lane='module', fs=9)
B = lambda idn, i, t, k='proc', h=NORM_H: N(idn, k, x0 + 290, mrow(i), 240, h, t, lane='module', fs=9)
# rows: 0 A1,1 A2,2 A3(dec) with B1 at row 2; B2..B5 rows 3-6; A4 at row 6; A5.. rows 7..
nodes['m2_2']['y'] = mrow(2) - 6
B('m2_b1', 2, 'Mobile Capture: staff opens Mobile Capture Web App')
B('m2_b2', 3, 'Select the corresponding pending Marriage Registration', h=52)
B('m2_b3', 4, 'Capture photograph of Form 97')
B('m2_b4', 5, 'Send captured image directly to CROMS Main')
B('m2_b5', 6, 'Attach image to pending Marriage Registration', h=52)
A('m2_a4', 6, 'Attached Form 97 image sent to existing CROMS OCR engine', h=52)
A('m2_a5', 7, 'OCR scans document; extracts marriage information', h=52)
A('m2_a6', 8, 'Display extracted fields beside source document')
A('m2_a7', 9, 'Staff verifies extracted information')
A('m2_a8', 10, 'OCR data correct?', 'dec')
A('m2_a9', 11, 'Validate Marriage Registration information')
A('m2_a10', 12, 'Save validated marriage record')
A('m2_a11', 13, 'Register record: PENDING → REGISTERED')
A('m2_a12', 14, 'Store digital document; update Client Task', h=52)
A('m2_a13', 15, 'Record audit activity')
N('m2_up', 'note', x0 + 10, mrow(3) + 20, 240, 60, 'Option 1: normal file / image upload (goes straight to OCR)', lane='module', fs=9)
N('m2_ocrs', 'note', x0 + 290, mrow(10) + 2, 240, 60, 'NO: staff corrects extracted values', lane='module', fs=9)
F('m2_1', 'b', 'm2_a4', 't') if False else None
F('m2_2', 'r', 'm2_b1', 'l', via=[(x0 + 270, nodes['m2_2']['y'] + DEC_H / 2)], label='Option 2: Mobile Capture', lpos=0.3) if False else \
    F('m2_2', 'r', 'm2_b1', 'l', label='Mobile')
F('m2_2', 'b', 'm2_up', 't', label='Upload')
F('m2_up', 'b', 'm2_a4', 't')
F('m2_b1', 'b', 'm2_b2', 't'); F('m2_b2', 'b', 'm2_b3', 't'); F('m2_b3', 'b', 'm2_b4', 't'); F('m2_b4', 'b', 'm2_b5', 't')
F('m2_b5', 'l', 'm2_a4', 'r')
for a, b in [('m2_a4', 'm2_a5'), ('m2_a5', 'm2_a6'), ('m2_a6', 'm2_a7'), ('m2_a7', 'm2_a8'), ('m2_a8', 'm2_a9'), ('m2_a9', 'm2_a10'),
             ('m2_a10', 'm2_a11'), ('m2_a11', 'm2_a12'), ('m2_a12', 'm2_a13')]:
    F(a, 'b', b, 't', label='YES' if a == 'm2_a8' else None)
F('m2_a8', 'r', 'm2_ocrs', 'l', label='NO')
F('m2_ocrs', 'b', 'm2_a9', 't', via=[(cx('m2_ocrs'), nodes['m2_a9']['y'] - 12), (cx('m2_a9'), nodes['m2_a9']['y'] - 12)])
# remove auto flow arrows that module() created between first three rows except 1->2,2->? keep
edges[:] = [e for e in edges if not (e['s'] == 'm2_2' and e['t'] == 'm2_3')]
ids2 = ['m2_0', 'm2_1', 'm2_2', 'm2_a4', 'm2_a5', 'm2_a6', 'm2_a7', 'm2_a8', 'm2_a9', 'm2_a10', 'm2_a11', 'm2_a12', 'm2_a13']
add_data(2, ids2, [(3, 'd8', 'b'), (9, 'd4', 'b'), (10, 'd4', 'b'), (11, 'd8', 'b'), (10, 'd3', 'w')], 12,
         [('d4', 'D4 CIVIL REGISTRY RECORDS'), ('d8', 'D8 DOCUMENTS / OCR'), ('d3', 'D3 QUEUE & TRANSACTIONS')]) if False else None
# mobile capture: image -> CROMS Main (DFD)
N('mob', 'store', 0, 0, 0, 0, '') if False else None

death = [S('Open Death Registration module'), S('Retrieve client information', 'io'), S('Verify requirements'), S('Enter / verify death record information'),
         S('Attach / scan / capture required documents', 'io'), S('OCR if document digitization is needed'), S('Staff verifies extracted information'),
         S('Validate Death Registration'), S('Save record; update registry'), S('Update Client Task = COMPLETED'), S('Record audit activity')]
ids3 = module(3, 'L. DEATH REGISTRATION', 'Main system', death, None, 10, None)
add_data(3, ids3, [(4, 'd8', 'b'), (8, 'd4', 'b'), (9, 'd3', 'w')], 10,
         [('d4', 'D4 CIVIL REGISTRY RECORDS'), ('d8', 'D8 DOCUMENTS / OCR'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

cert = [S('Open Certificate Request'), S('Retrieve client request'), S('Search Civil Registry Records'), S('Record found?', 'dec'),
        S('Display matched record'), S('Staff verifies correct record'), S('Prepare requested certificate / certified copy'),
        S('Record applicable processing / fees (if used)'), S('Generate / Print certificate'), S('Set request = READY FOR RELEASE'),
        S('Save Certificate Request history'), S('Record audit activity')]
ids4 = module(4, 'M. CERTIFICATE REQUEST / CPC', 'Main system', cert, None, 11, None)
side(4, ids4, 3, 'NO: record result / staff handling; update request status', label='NO', rejoin=False, h=64)
add_data(4, ids4, [(2, 'd4', 'b'), (9, 'd5', 'b'), (10, 'd3', 'w')], 11,
         [('d4', 'D4 CIVIL REGISTRY RECORDS'), ('d5', 'D5 CERTIFICATE REQUESTS & RELEASES'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

rel = [S('Open Release & Claim'), S('Retrieve request via Previous Queue No. / reference'), S('Verify document is ready'),
       S('Verify claimant / client identity'), S('Valid and ready for release?', 'dec'), S('Record release'),
       S('Release document to claimant'), S('Update Request = RELEASED / CLAIMED'), S('Update Client Task = COMPLETED'),
       S('Record release activity')]
ids5 = module(5, 'N. RELEASE & CLAIM', 'Main system', rel, None, 9, None)
side(5, ids5, 4, 'NO: do NOT release; display / record reason; keep status', label='NO', rejoin=False, h=64)
add_data(5, ids5, [(1, 'd5', 'b'), (7, 'd5', 'b'), (8, 'd3', 'w')], 9,
         [('d5', 'D5 CERTIFICATE REQUESTS & RELEASES'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

pet = [S('Open Petition / Case module'), S('Retrieve petitioner information', 'io'),
       S('Identify case type: RA 9048, RA 10172, Legal Instrument, Court Order, other', h=64), S('Check requirements'),
       S('Create / open Case record'), S('Attach supporting documents'), S('Record case details'), S('Track case status / progress'),
       S('Staff records actions / remarks'), S('Update case when additional action occurs'),
       S('Mark case process completed when appropriate; retain full case history', h=64), S('Record audit activity')]
ids6 = module(6, 'O. PETITION / CASE TRACKING', 'Main system', pet, None, 11, None)
N('m6_note', 'note', COLS[6] + 270, nodes[ids6[1]]['y'], 214, 92, 'CROMS tracks and documents the case; authorized LCRO staff make legal / administrative decisions', lane='module', fs=9)
add_data(6, ids6, [(4, 'd6', 'b'), (5, 'd8', 'b'), (10, 'd3', 'w')], 11,
         [('d6', 'D6 PETITIONS & CASES'), ('d8', 'D8 DOCUMENTS / OCR'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

br = [S('Open PSA BREQS task'), S('Retrieve BREQS request information', 'io'), S('Verify requester / subject information'),
      S('Verify valid ID + requirements'), S('Review document type, copies, purpose, relationship', h=52), S('Process / record BREQS request'),
      S('Track request status'), S('Update Client Task when processed / completed'), S('Save transaction history'), S('Record audit activity')]
ids7 = module(7, 'P. PSA BREQS', 'Main system', br, None, 9, None)
add_data(7, ids7, [(1, 'd7', 'b'), (5, 'd7', 'b'), (7, 'd3', 'b')], 9,
         [('d7', 'D7 PSA BREQS RECORDS'), ('d3', 'D3 QUEUE & TRANSACTIONS')])

doc = [S('Document received / captured'), S('Upload / Scan / Mobile Capture', 'io'), S('Store source image'), S('Send image to OCR'),
       S('OCR extracts text / fields'), S('Display extracted data'), S('Staff compares extracted data with original image', h=52),
       S('OCR accurate?', 'dec'), S('Validate data; accept extracted values'),
       S('Link document to Birth / Marriage / Death record, Certificate Request, Petition / Case, other', h=64),
       S('Store validated digital document'), S('Record OCR / verification activity')]
ids8 = module(8, 'Q. DOCUMENT MANAGEMENT / OCR', 'Main system', doc, None, 11, None)
side(8, ids8, 7, 'NO: staff corrects data; save corrected values', label='NO', h=64)
add_data(8, ids8, [(2, 'd8', 'w'), (4, 'd8', 'b'), (10, 'd8', 'w')], 11,
         [('d8', 'D8 DOCUMENTS / OCR')])

# marriage registration data (its own rails; ids2 is custom)
# stores for MR
x0 = COLS[2]
for k, (key, label) in enumerate([('d4', 'D4 CIVIL REGISTRY RECORDS'), ('d8', 'D8 DOCUMENTS / OCR'), ('d3', 'D3 QUEUE & TRANSACTIONS')]):
    N('st2_' + key, 'store', x0 + 10 + k * 145, STOREY, 130, 66, label, lane='data', fs=9)
rail = [(('m2_a4', 'st2_d8', True), 0), (('m2_a11', 'st2_d3', False), 1), (('m2_a10', 'st2_d4', True), 2), (('m2_a12', 'st2_d8', True), 3)]
for ri, ((a, s, both), i) in enumerate(rail):
    rx = x0 + 1040 - 60 - ri * 9
    jog = 4246 + (3 - ri) * 5
    D(a, 'r', s, 't', via=[(rx if a != 'm2_a4' else x0 + 1040 - 60 - ri * 9, cy(a)), (rx, jog), (cx(s), jog)], both=both)
D('m2_b4', 'r', 'st2_d8', 't', via=[(x0 + 560, cy('m2_b4')), (x0 + 560, 4252), (cx('st2_d8'), 4252)], label='Document image → CROMS Main', lpos=0.2) if False else None
D('m2_a13', 'r', 'd10_bar', (x0 + 1040 - 14, D10Y), via=[(x0 + 1040 - 14, cy('m2_a13'))])
N('m2_R', 'conn', cx('m2_a13') - 17, nodes['m2_a13']['y'] + nodes['m2_a13']['h'] + 12, 34, 34, 'R', lane='module', fs=9)
F('m2_a13', 'b', 'm2_R', 't')

# ---------------------------------------------------------------- CLD LOOPS
def loop(id, title, cxx, cyy, vars_, links, rx=330, ry=190, center_note=None, ang0=-90, manual=None):
    n = len(vars_)
    pos = {}
    for i, v in enumerate(vars_):
        a = math.radians(ang0 + 360.0 * i / n)
        px, py = cxx + rx * math.cos(a), cyy + ry * math.sin(a)
        if manual: px, py = manual[v]
        vid = '%s_v%d' % (id, i)
        N(vid, 'cvar', px - 78, py - 24, 156, 48, v, fs=10)
        pos[v] = vid
    N(id + '_lbl', 'ctitle', cxx - 330, cyy - ry - 58, 300, 26, '<b>%s</b>' % title, fs=14)
    for a, b, sgn in links:
        pa, pb = nodes[pos[a]], nodes[pos[b]]
        ax, ay = pa['x'] + pa['w'] / 2, pa['y'] + pa['h'] / 2
        bx_, by_ = pb['x'] + pb['w'] / 2, pb['y'] + pb['h'] / 2
        # clip to ellipse-ish boundary
        dx, dy = bx_ - ax, by_ - ay
        d = math.hypot(dx, dy)
        ux, uy = dx / d, dy / d
        def clip(n_, ux, uy):
            hw, hh = n_['w'] / 2 + 3, n_['h'] / 2 + 3
            t = min(hw / abs(ux) if abs(ux) > 1e-6 else 1e9, hh / abs(uy) if abs(uy) > 1e-6 else 1e9)
            return t
        sx, sy = ax + ux * clip(pa, ux, uy), ay + uy * clip(pa, ux, uy)
        ex, ey = bx_ - ux * clip(pb, -ux, -uy), by_ - uy * clip(pb, -ux, -uy)
        edges.append(dict(kind='causal', s=pos[a], t=pos[b], sa=(sx, sy, 0.5, 0.5), ta=(ex, ey, 0.5, 0.5),
                          pts=[(sx, sy), (ex, ey)], label=sgn, both=False, lseg=0, lpos=None, w=None, free=True))
    return pos


TOPY = 525
p_b3 = loop('B3', 'B3  Client Tasks Balancing Loop', 900, TOPY,
            ['Client Selected Services', 'Client Tasks', 'Required Processing', 'Staff Workload', 'Completed Client Tasks', 'Remaining Client Tasks'],
            [('Client Selected Services', 'Client Tasks', '+'), ('Client Tasks', 'Required Processing', '+'), ('Required Processing', 'Staff Workload', '+'),
             ('Staff Workload', 'Completed Client Tasks', '+'), ('Completed Client Tasks', 'Remaining Client Tasks', '−'),
             ('Remaining Client Tasks', 'Required Processing', '+')], ang0=90)
p_b2 = loop('B2', 'B2  Requirements Loop (pending requirements)', 2700, TOPY,
            ['Missing Requirements', 'Pending Transactions', 'Processing Delay', 'Queue / Workload', 'Successful Processing', 'Complete Requirements'],
            [('Missing Requirements', 'Pending Transactions', '+'), ('Pending Transactions', 'Processing Delay', '+'), ('Processing Delay', 'Queue / Workload', '+'),
             ('Complete Requirements', 'Successful Processing', '+'), ('Successful Processing', 'Pending Transactions', '−')], ang0=-90)
p_b1 = loop('B1', 'B1  Queue Processing Loop', 4500, TOPY,
            ['Client Transactions', 'Waiting Queue', 'Staff Workload ', 'Transactions Processed', 'Completed Transactions'],
            [('Client Transactions', 'Waiting Queue', '+'), ('Waiting Queue', 'Staff Workload ', '+'), ('Staff Workload ', 'Transactions Processed', '+'),
             ('Transactions Processed', 'Completed Transactions', '+'), ('Completed Transactions', 'Waiting Queue', '−')], ang0=90)
BOTY = 4905
p_b5 = loop('B5', 'B5  Window Capacity Loop', 900, BOTY,
            ['Available Service Windows', 'Processing Capacity', 'Transactions Processed ', 'Waiting Queue ', 'Client Waiting Time'],
            [('Available Service Windows', 'Processing Capacity', '+'), ('Processing Capacity', 'Transactions Processed ', '+'),
             ('Transactions Processed ', 'Waiting Queue ', '−'), ('Waiting Queue ', 'Client Waiting Time', '+'),
             ('Client Waiting Time', 'Available Service Windows', '+')])
p_r1 = loop('R1', 'R1  Monitoring / Process Improvement Loop', 2700, BOTY,
            ['Transaction Tracking', 'Staff Visibility', 'Staff Response', 'Transactions Processed  ', 'Updated Transaction Info', 'Tracking Quality'],
            [('Transaction Tracking', 'Staff Visibility', '+'), ('Staff Visibility', 'Staff Response', '+'), ('Staff Response', 'Transactions Processed  ', '+'),
             ('Transactions Processed  ', 'Updated Transaction Info', '+'), ('Updated Transaction Info', 'Tracking Quality', '+'),
             ('Tracking Quality', 'Transaction Tracking', '+')])
B4C = 4500
p_b4 = loop('B4', 'B4  OCR Validation Loop', B4C, BOTY,
            ['Documents Digitized', 'OCR Processing', 'Extracted Data', 'Staff Verification', 'Validated Digital Records',
             'Uncorrected OCR Errors', 'Record Accuracy', 'OCR Errors', 'Required Corrections', 'Processing Time'],
            [('Documents Digitized', 'OCR Processing', '+'), ('OCR Processing', 'Extracted Data', '+'), ('Extracted Data', 'Staff Verification', '+'),
             ('Staff Verification', 'Validated Digital Records', '+'), ('Staff Verification', 'Uncorrected OCR Errors', '−'),
             ('Uncorrected OCR Errors', 'Record Accuracy', '−'), ('OCR Processing', 'OCR Errors', '+'), ('OCR Errors', 'Required Corrections', '+'),
             ('Required Corrections', 'Processing Time', '+')], ry=205,
            manual={'Documents Digitized': (3760, 4780), 'OCR Processing': (4060, 4780), 'Extracted Data': (4360, 4780), 'Staff Verification': (4660, 4780),
                    'Validated Digital Records': (4960, 4780), 'OCR Errors': (4060, 4960), 'Required Corrections': (4060, 5090), 'Processing Time': (4360, 5090),
                    'Uncorrected OCR Errors': (4660, 4960), 'Record Accuracy': (4960, 4960)})

K('B4_v9', 'b', 'B4_v0', 'b', via=[(4360, 5150), (3760, 5150)], label='−', lpos=0.5)

# CLD anchoring tags on flowchart
def tag(id, text, x, y): N(id, 'tag', x, y, 84, 24, text, fs=9)
tag('t_b1a', 'B1 ▸ waiting', nodes['f4']['x'] + 60, nodes['f4']['y'] - 14)
tag('t_b1b', 'B1 ▸ processed', nodes['s3']['x'] + 60, nodes['s3']['y'] - 14)
tag('t_b2a', 'B2 ▸ req.', nodes['h3']['x'] + 46, nodes['h3']['y'] - 12)
tag('t_b3a', 'B3 ▸ tasks', nodes['g2']['x'] + 60, nodes['g2']['y'] - 14)
tag('t_b3b', 'B3 ▸ remaining', nodes['r1']['x'] + 50, nodes['r1']['y'] - 12)
tag('t_b3c', 'B3 ▸ services', nodes['cl3']['x'] - 20, nodes['cl3']['y'] - 14)
tag('t_b4a', 'B4 ▸ OCR', nodes['m2_a5']['x'] + 170, nodes['m2_a5']['y'] - 14)
tag('t_b4b', 'B4 ▸ verify', nodes['m8_6']['x'] + 170, nodes['m8_6']['y'] - 14)
tag('t_b5a', 'B5 ▸ windows', nodes['b7']['x'] + 60, nodes['b7']['y'] - 14)
tag('t_b5b', 'B5 ▸ capacity', nodes['f3']['x'] + 60, nodes['f3']['y'] - 14)
tag('t_r1a', 'R1 ▸ tracking', nodes['s5']['x'] + 60, nodes['s5']['y'] - 14)
tag('t_r1b', 'R1 ▸ visibility', nodes['g2']['x'] + 60, nodes['g2']['y'] + nodes['g2']['h'] - 8)

# CLD <-> flow real links (short, no crossings)
K('B3_v0', 'b', 'cl3', 't', via=[(cx('B3_v0'), 740), (cx('cl3'), 740)], label='+ services chosen', lpos=0.5)
K('B1_v0', 'b', 'rv3', 't', via=[(cx('B1_v0'), 740), (cx('rv3'), 740)], label='+ new client transactions', lpos=0.5)
K('R1_v0', 'b', 'd10_bar', 'b', via=[(cx('R1_v0'), 4560), (cx('R1_v0'), 4560)], label='+ tracking uses audit trail', lpos=0.4)
K('B4_v3', 't', 'd10_bar', 'b', via=[(cx('B4_v3'), 4560), (cx('B4_v3'), 4560)], label='+ verification logged', lpos=0.4)
K('B4_v7', 'b', 'st8_d8' if 'st8_d8' in nodes else 'd10_bar', 't') if False else None

# ---------------------------------------------------------------- emit
def esc(t): return html.escape(t, quote=True).replace('\n', '&#10;')

def style_for(n):
    k, lane = n['kind'], n['lane']
    fill = n['fill'] or NODEFILL.get(lane, '#FFFFFF')
    stroke = n['stroke'] or '#444444'
    base = 'whiteSpace=wrap;html=1;fontSize=%d;fontColor=%s;' % (n['fs'], n['fc'])
    if k == 'lane':
        return 'rounded=0;whiteSpace=wrap;html=1;fillColor=%s;strokeColor=%s;verticalAlign=top;align=left;spacingLeft=10;spacingTop=4;fontSize=14;fontStyle=1;%s' % (
            fill, stroke, 'dashed=1;' if n['dash'] else '')
    if k == 'text': return 'text;html=1;align=left;verticalAlign=top;whiteSpace=wrap;fontSize=%d;fontStyle=%d;' % (n['fs'], 1 if n['bold'] else 0)
    if k == 'pt': return 'text;html=1;'
    if k == 'oval': return 'ellipse;%sfillColor=#C8E6C9;strokeColor=#2E7D32;strokeWidth=2;' % base
    if k == 'proc': return 'rounded=1;arcSize=8;%sfillColor=%s;strokeColor=#444444;' % (base, fill)
    if k == 'head': return 'rounded=1;arcSize=10;%sfillColor=#F4B6AF;strokeColor=#8E2A22;strokeWidth=2;fontStyle=0;' % base
    if k == 'dec': return 'rhombus;%sfillColor=#FFF2CC;strokeColor=#B08900;' % base
    if k == 'io': return 'shape=parallelogram;perimeter=parallelogramPerimeter;%sfillColor=#E1F5FE;strokeColor=#0277BD;fixedSize=1;size=14;' % base
    if k == 'store': return 'shape=cylinder3;boundedLbl=1;backgroundOutline=1;size=9;%sfillColor=#FFF8C4;strokeColor=#8A7A00;strokeWidth=2;' % base
    if k == 'conn': return 'ellipse;%sfillColor=#FFFFFF;strokeColor=#1F4E79;strokeWidth=2;fontStyle=1;' % base
    if k == 'note': return 'shape=note;size=10;%sfillColor=#FFFDE7;strokeColor=#999999;dashed=1;' % base
    if k == 'ebox': return 'rounded=1;arcSize=4;%sfillColor=#FFFFFF;strokeColor=#67AB7E;align=left;verticalAlign=top;spacingLeft=6;spacingTop=2;' % base
    if k == 'tag': return 'rounded=1;arcSize=40;%sfillColor=#FFEBEE;strokeColor=#C62828;fontColor=#C62828;fontStyle=1;' % base
    if k == 'cvar': return 'ellipse;%sfillColor=#FFF3F2;strokeColor=#C62828;strokeWidth=1.5;' % base
    if k == 'ctitle': return 'text;html=1;align=left;verticalAlign=middle;whiteSpace=wrap;fontSize=%d;fontColor=#C62828;' % n['fs']
    return base


def emit(path):
    out = ['<mxfile host="app.diagrams.net" agent="Claude" version="24.7.17" type="device">',
           '<diagram id="croms-integrated" name="CROMS Integrated System Model">',
           '<mxGraphModel dx="%d" dy="%d" grid="1" gridSize="10" guides="1" tooltips="1" connect="1" arrows="1" fold="1" page="1" pageScale="1" pageWidth="%d" pageHeight="%d" math="0" shadow="0">' % (W, H, W, H),
           '<root><mxCell id="0"/><mxCell id="1" parent="0"/>']
    for nid in order:
        n = nodes[nid]
        if n['w'] == 0 and n['h'] == 0: continue
        if n['kind'] == 'pt': continue
        v = esc(n['text'])
        if n['kind'] == 'lane':
            pass
        out.append('<mxCell id="%s" value="%s" style="%s" vertex="1" parent="1"><mxGeometry x="%d" y="%d" width="%d" height="%d" as="geometry"/></mxCell>' % (
            nid, v, style_for(n), n['x'], n['y'], n['w'], n['h']))
    # legend arrows as edges between pt nodes
    for k, (a, b, kind) in enumerate(edges_leg):
        na, nb = nodes[a], nodes[b]
        out.append(edge_xml('leg%d' % k, kind, None, None, [(na['x'], na['y']), (nb['x'], nb['y'])], None, False, None, None, a_raw=(na['x'], na['y']), b_raw=(nb['x'], nb['y'])))
    for i, e in enumerate(edges):
        out.append(edge_xml('e%d' % i, e['kind'], e['s'], e['t'], e['pts'], e['label'], e['both'], e['sa'], e['ta'], lpos=e.get('lpos')))
    out.append('</root></mxGraphModel></diagram></mxfile>')
    open(path, 'w', encoding='utf-8').write('\n'.join(out))


def edge_xml(eid, kind, s, t, pts, label, both, sa, ta, a_raw=None, b_raw=None, lpos=None):
    col = C[kind]
    dash = 'dashed=1;dashPattern=6 4;' if kind in ('data', 'causal') else ''
    sw = 2 if kind == 'flow' else 1.6
    st = 'edgeStyle=none;html=1;rounded=0;strokeColor=%s;strokeWidth=%s;%sendArrow=classic;endFill=1;%s' % (
        col, sw, dash, 'startArrow=classic;startFill=1;' if both else '')
    if sa: st += 'exitX=%.4f;exitY=%.4f;exitDx=0;exitDy=0;' % (max(0, min(1, sa[2])), max(0, min(1, sa[3])))
    if ta: st += 'entryX=%.4f;entryY=%.4f;entryDx=0;entryDy=0;' % (max(0, min(1, ta[2])), max(0, min(1, ta[3])))
    if kind == 'causal': st += 'fontColor=#C62828;fontStyle=1;fontSize=15;labelBackgroundColor=#FFFFFF;'
    else: st += 'fontSize=10;fontStyle=1;fontColor=%s;labelBackgroundColor=#FFFFFF;' % col
    src = ' source="%s"' % s if s and s in nodes and nodes[s]['kind'] not in ('pt',) else ''
    tgt = ' target="%s"' % t if t and t in nodes and nodes[t]['kind'] not in ('pt',) else ''
    mid = ''
    if len(pts) > 2:
        mid = '<Array as="points">%s</Array>' % ''.join('<mxPoint x="%d" y="%d"/>' % (x, y) for x, y in pts[1:-1])
    a0, b0 = pts[0], pts[-1]
    geo = '<mxGeometry relative="1" as="geometry"><mxPoint x="%d" y="%d" as="sourcePoint"/><mxPoint x="%d" y="%d" as="targetPoint"/>%s</mxGeometry>' % (a0[0], a0[1], b0[0], b0[1], mid)
    return '<mxCell id="%s" value="%s" style="%s" edge="1" parent="1"%s%s>%s</mxCell>' % (eid, esc(label or ''), st, src, tgt, geo)


# ---------------------------------------------------------------- PIL preview
def preview(path, scale=0.3, crop=None):
    try:
        f = lambda s: ImageFont.truetype('C:/Windows/Fonts/arial.ttf', max(6, int(s * scale * 1.0 + 0.5)))
    except Exception:
        f = lambda s: ImageFont.load_default()
    x0, y0, x1, y1 = crop or (0, 0, W, H)
    img = Image.new('RGB', (int((x1 - x0) * scale), int((y1 - y0) * scale)), 'white')
    d = ImageDraw.Draw(img)
    T = lambda x, y: ((x - x0) * scale, (y - y0) * scale)
    import re
    def plain(s): return re.sub(r'<[^>]+>', ' ', s.replace('<br>', '\n')).replace('&amp;', '&')
    for nid in order:
        n = nodes[nid]
        if n['w'] == 0 and n['h'] == 0: continue
        if n['kind'] == 'pt': continue
        a = T(n['x'], n['y']); b = T(n['x'] + n['w'], n['y'] + n['h'])
        k = n['kind']
        fill = n['fill'] or NODEFILL.get(n['lane'], '#FFFFFF')
        if k == 'lane':
            d.rectangle([a, b], fill=fill, outline=n['stroke']); d.text((a[0] + 6, a[1] + 3), plain(n['text']), fill='#333', font=f(13)); continue
        if k == 'text':
            d.text(a, plain(n['text']), fill='#000', font=f(n['fs'])); continue
        if k in ('oval', 'conn', 'cvar'):
            d.ellipse([a, b], fill={'oval': '#C8E6C9', 'conn': '#FFF', 'cvar': '#FFF3F2'}[k], outline={'oval': '#2E7D32', 'conn': '#1F4E79', 'cvar': '#C62828'}[k])
        elif k == 'dec':
            cxx, cyy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            d.polygon([(cxx, a[1]), (b[0], cyy), (cxx, b[1]), (a[0], cyy)], fill='#FFF2CC', outline='#B08900')
        elif k == 'io':
            s_ = 14 * scale
            d.polygon([(a[0] + s_, a[1]), (b[0], a[1]), (b[0] - s_, b[1]), (a[0], b[1])], fill='#E1F5FE', outline='#0277BD')
        elif k == 'store':
            d.rectangle([a, b], fill='#FFF8C4', outline='#8A7A00'); d.line([a[0], a[1] + 9 * scale, b[0], a[1] + 9 * scale], fill='#8A7A00')
        elif k == 'head':
            d.rectangle([a, b], fill='#F4B6AF', outline='#8E2A22')
        elif k == 'tag':
            d.rounded_rectangle([a, b], radius=6, fill='#FFEBEE', outline='#C62828')
        elif k == 'ebox':
            d.rectangle([a, b], fill='#FFF', outline='#67AB7E')
        elif k == 'note':
            d.rectangle([a, b], fill='#FFFDE7', outline='#999')
        elif k == 'ctitle':
            pass
        else:
            d.rounded_rectangle([a, b], radius=4, fill=fill, outline='#444')
        fs = n['fs']
        txt = plain(n['text'])
        if k == 'ebox':
            fnt = f(fs); lines=[]
            for para in txt.split(chr(10)):
                cur=''
                for wd in para.split():
                    t2=(cur+' '+wd).strip()
                    if fnt.getlength(t2) <= (b[0]-a[0])-8 or not cur: cur=t2
                    else: lines.append(cur); cur=wd
                lines.append(cur)
            d.multiline_text((a[0] + 3, a[1] + 2), chr(10).join(lines), fill='#000', font=fnt, spacing=1)
        else:
            fnt = f(fs)
            avail = (b[0] - a[0]) * (0.62 if k in ('dec','oval','cvar','conn') else 0.9)
            lines = []
            for para in txt.split(chr(10)):
                cur = ''
                for wd in para.split():
                    t2 = (cur + ' ' + wd).strip()
                    if fnt.getlength(t2) <= avail or not cur: cur = t2
                    else: lines.append(cur); cur = wd
                lines.append(cur)
            d.multiline_text(((a[0] + b[0]) / 2, (a[1] + b[1]) / 2), chr(10).join(lines), fill='#B71C1C' if k in ('tag', 'ctitle') else '#000', font=fnt, anchor='mm', align='center', spacing=1)
    for e in edges:
        pts = [T(*p) for p in e['pts']]
        col = C[e['kind']]
        d.line(pts, fill=col, width=max(1, int(2 * scale + 0.5)))
        (x1_, y1_), (x2_, y2_) = pts[-2], pts[-1]
        ang = math.atan2(y2_ - y1_, x2_ - x1_); L = max(4, 11 * scale)
        d.polygon([(x2_, y2_), (x2_ - L * math.cos(ang - .4), y2_ - L * math.sin(ang - .4)), (x2_ - L * math.cos(ang + .4), y2_ - L * math.sin(ang + .4))], fill=col)
        if e['both']:
            (x1_, y1_), (x2_, y2_) = pts[1], pts[0]
            ang = math.atan2(y2_ - y1_, x2_ - x1_)
            d.polygon([(x2_, y2_), (x2_ - L * math.cos(ang - .4), y2_ - L * math.sin(ang - .4)), (x2_ - L * math.cos(ang + .4), y2_ - L * math.sin(ang + .4))], fill=col)
        if e['label']:
            if e.get('free'):
                mx, my = (pts[0][0] + pts[-1][0]) / 2, (pts[0][1] + pts[-1][1]) / 2
            else:
                t = e['lpos'] if e.get('lpos') is not None else 0.3
                tot = sum(math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1]) for i in range(len(pts) - 1))
                tgt = tot * t; acc = 0
                for i in range(len(pts) - 1):
                    seg = math.hypot(pts[i + 1][0] - pts[i][0], pts[i + 1][1] - pts[i][1])
                    if acc + seg >= tgt:
                        r = (tgt - acc) / max(seg, 1e-6); mx, my = pts[i][0] + r * (pts[i + 1][0] - pts[i][0]), pts[i][1] + r * (pts[i + 1][1] - pts[i][1]); break
                    acc += seg
                else:
                    mx, my = pts[0]
            d.text((mx, my), e['label'], fill=col, font=f(11 if e['kind'] != 'causal' else 22), anchor='mm')
    img.save(path)


if __name__ == '__main__':
    out = sys.argv[1]
    emit(out)
    preview(out.replace('.drawio', '_preview.png'), 0.28)
    if len(sys.argv) > 2:
        c = [int(v) for v in sys.argv[2].split(',')]
        preview(out.replace('.drawio', '_crop.png'), float(sys.argv[3]), tuple(c))
    print('nodes', len(nodes), 'edges', len(edges))
